﻿

using System.IO;
using Microsoft.AspNetCore.Mvc.Internal;
using Serilog.Core;
using Serilog.Events;

namespace Robi.Clash.DefaultSelectors
{
    using Common;
    using Engine;
    using Robi.Engine;
    using Robi.Engine.Settings;
    using Serilog;
    using Settings;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public abstract class BehaviorBase : ActionSelectorBase, IBehavior
    {
        private static ILogger Logger { get; } = LogProvider.CreateLogger<BehaviorBase>();

        public abstract Cast GetBestCast(Playfield p);
        public abstract float GetPlayfieldValue(Playfield p);
        public abstract int GetBoValue(BoardObj bo, Playfield p);
        public abstract int GetPlayCardPenalty(CardDB.Card card, Playfield p);
		private string rName = "Routine";
        private static BehaviorBaseSettings Settings => SettingsManager.GetSetting<BehaviorBaseSettings>("Routine");
        private readonly List<Playfield> battleLogs = new List<Playfield>();
        private List<Handcard> prevHandCards = new List<Handcard>();
        private int statNumSuccessfulEntrances = 0;
        private TimeSpan statTimeOutsideRoutine;
        private TimeSpan statSumTimeOutsideRoutine;
        private TimeSpan statTimeInitPlayfield;
        private TimeSpan statSumTimeInitPlayfield;
        private TimeSpan statTimeInsideBehavior;
        private TimeSpan statSumTimeInsideBehavior;
        private DateTime statTimerRoutine;
        private uint nextGId = 0;
        private CastRequest CastRequest;
        private Dictionary<string, CastRequest> CastRequestDB = new Dictionary<string, CastRequest>();
        private Dictionary<string, int> CastRequestDBtmp = new Dictionary<string, int>();
        private readonly Dictionary<int, double> lvlToCoef = new Dictionary<int, double>() { { 1, 1 }, { 2, 1.1 }, { 3, 1.21 }, { 4, 1.33 }, { 5, 1.46 }, { 6, 1.6 }, { 7, 1.76 }, { 8, 1.93 }, { 9, 2.12 }, { 10, 2.33 }, { 11, 2.56 }, };
	
        public static bool GameBeginning = false;
        private VectorAI ownKingsTowerPos = new VectorAI(-1, -1);
        private int friendlyOwnerIndex = -1;

        private ILogEventSink _battleLogger;
        public override void BattleStart()
        {
            base.BattleStart();
            var battleLogName = Path.Combine(LogProvider.LogPath, "BattleLog", $"battle-{DateTime.Now:yyyyMMddHHmmss}.log");
            Logger.Debug($"BattleLog: {battleLogName}");
            _battleLogger = new LoggerConfiguration()
                .Filter.ByExcluding(e =>
                {
                    if (!e.Properties.ContainsKey("SourceContext")) return true;
                    var ctx = ((Serilog.Events.ScalarValue)e.Properties["SourceContext"]).Value as string;
                    if (string.IsNullOrWhiteSpace(ctx)) return true;
                    return !(ctx.StartsWith("Robi.Clash.DefaultSelectors") || ctx.StartsWith("Robi.Engine.PerformanceTimer"));
                }).WriteTo.File(battleLogName, outputTemplate: "{Message}{NewLine}{Exception}").CreateLogger();
            LogProvider.AttachSink(_battleLogger);

            GameBeginning = true;
            ownKingsTowerPos.X = -1;
            ownKingsTowerPos.Y = -1;
            friendlyOwnerIndex = -1;
            battleLogs.Clear();
            prevHandCards.Clear();
            nextGId = 0;

            statNumSuccessfulEntrances = 0;
            statTimeOutsideRoutine = TimeSpan.Zero;
            statTimeInitPlayfield = TimeSpan.Zero;
            statSumTimeOutsideRoutine = TimeSpan.Zero;
            statSumTimeInitPlayfield = TimeSpan.Zero;
            statSumTimeInsideBehavior = TimeSpan.Zero;
            statTimerRoutine = DateTime.Now;
        }

        public override void BattleEnd()
        {
            string battleres = "";
            bool needCrowns = false;

            var battleModel = ClashEngine.Instance.BattleModel;
            if (battleModel == null || !battleModel.IsValid) battleres = "BattleModel not valid";
            else
            {
                var endHud = battleModel.BattleEndHud;
                if (endHud == null || !endHud.IsValid) battleres = "BattleEndHud not valid";
                else
                {
                    var okButton = endHud.LeaveButton;
                    if (okButton == null || !okButton.IsValid) battleres = "OkButton not valid";
                    else
                    {
                        switch (endHud.BattleResult)
                        {
                            case Robi.Clash.Engine.NativeObjects.Native.LOGIC_BATTLE_RESULT.LOGIC_BATTLE_RESULT_BLUE_WINS:
                                needCrowns = true;
                                battleres = "Win";
                                break;
                            case Robi.Clash.Engine.NativeObjects.Native.LOGIC_BATTLE_RESULT.LOGIC_BATTLE_RESULT_RED_WINS:
                                needCrowns = true;
                                battleres = "Lose";
                                break;
                            case Robi.Clash.Engine.NativeObjects.Native.LOGIC_BATTLE_RESULT.LOGIC_BATTLE_RESULT_DRAW:
                                needCrowns = true;
                                battleres = "Draw";
                                break;
                            default:
                                battleres = "ArgumentOutOfRangeException()";
                                //throw new ArgumentOutOfRangeException();
                                break;
                        }
                    }
                }
            }

            if (needCrowns)
            {
                Logger.Debug("Battle result: {res:l} {ownCrowns:l}:{enemyCrowns:l} {date}", battleres, battleModel.CombatHud.ScorePlayer.Content, battleModel.CombatHud.ScoreEnemy.Content, DateTime.Now.ToString(@"yyyy-MM-dd HH\:mm\:ss"));
            }
            else Logger.Debug("Battle result: {res:l} {date:l}", battleres, DateTime.Now.ToString(@"yyyy-MM-dd HH\:mm\:ss"));

            LogProvider.DetachSink(_battleLogger);
            ((Logger)_battleLogger).Dispose();
            _battleLogger = null;
            base.BattleEnd();
        }

        public override void Initialize()
        {
            SettingsManager.RegisterSettings(rName, new BehaviorBaseSettings());
            CardDB.Initialize();
// #if DEBUG - it for all cases
            AIDebugCommand.Register();
// #endif
        }

        public override void Deinitialize()
        {
            SettingsManager.UnregisterSettings(rName);
        }

        public sealed override CastRequest GetNextCast()
        {
            Logger.Debug("");
            if (statNumSuccessfulEntrances > 0) statTimeOutsideRoutine = DateTime.Now - statTimerRoutine;
            statTimerRoutine = DateTime.Now;

            List<BoardObj> ownMinions = new List<BoardObj>();
            List<BoardObj> enemyMinions = new List<BoardObj>();

            List<BoardObj> ownAreaEffects = new List<BoardObj>();
            List<BoardObj> enemyAreaEffects = new List<BoardObj>();

            List<BoardObj> ownBuildings = new List<BoardObj>();
            List<BoardObj> enemyBuildings = new List<BoardObj>();

            BoardObj ownKingsTower = new BoardObj();
            BoardObj ownPrincessTower1 = new BoardObj();
            BoardObj ownPrincessTower2 = new BoardObj();
            BoardObj enemyKingsTower = new BoardObj();
            BoardObj enemyPrincessTower1 = new BoardObj();
            BoardObj enemyPrincessTower2 = new BoardObj();

            List<Handcard> ownHandCards = new List<Handcard>();
            Handcard prevHandCard = new Handcard();

            Logger.Debug("#####Stats##### Inint BO {0}", (statTimerRoutine - DateTime.Now).TotalSeconds);
            
            var battle = ClashEngine.Instance.Battle;
            if (battle == null || !battle.IsValid) return null;
            var om = ClashEngine.Instance.ObjectManager;
            if (om == null) return null;
            var lp = ClashEngine.Instance.LocalPlayer;
            if (lp == null || !lp.IsValid) return null;
            var spellButtons = ClashEngine.Instance.AvailableSpellButtons;
            if (spellButtons == null) return null;
            var spells = ClashEngine.Instance.AvailableSpells;
            if (spells == null) return null;

            if (ownKingsTowerPos.Y == -1)
            {
                List<Tuple<int, int>> towersIndY = new List<Tuple<int, int>>();
                bool needFriendlyIndex = false;
                var chars = om.OfType<Clash.Engine.NativeObjects.Logic.GameObjects.Character>();
                foreach (var @char in chars)
                {
                    if (!@char.IsValid) continue;
                    var data = @char.LogicGameObjectData;
                    if (data == null || !data.IsValid) continue;
                    var name = data.Name;
                    if ((MemPtr)name == MemPtr.Zero) continue;

                    switch (CardDB.Instance.cardNamestringToEnum(name.Value.ToString(), "0"))
                    {
                        case CardDB.cardName.kingtower:
                            int OwnerIndex = (int)@char.OwnerIndex;
                            int charY = @char.StartPosition.Y;
                            towersIndY.Add(new Tuple<int, int>(OwnerIndex, charY));
                            if (OwnerIndex == lp.OwnerIndex) ownKingsTowerPos.Y = charY;
                            break;
                        case CardDB.cardName.kingtowermiddle:
                            ownKingsTowerPos.X = @char.StartPosition.X;
                            needFriendlyIndex = true;
                            break;
                    }
                }

                if (needFriendlyIndex)
                {
                    foreach (var t in towersIndY)
                    {
                        if (ownKingsTowerPos.Y == t.Item2 && lp.OwnerIndex != t.Item1) friendlyOwnerIndex = t.Item1;
                    }
                }
            }

            Logger.Debug("#####Stats##### Inint BO+engine {0}", (statTimerRoutine - DateTime.Now).TotalSeconds);

            using (new PerformanceTimer("GetNextCast entrance"))
            {
                Handcard Mirror = null;
                Dictionary<string, int> AvailableSpells = new Dictionary<string, int>();
                foreach (var spell in spells)
                {
                    if (spell == null || !spell.IsValid) continue;
                    var name = spell.Name;
                    if ((MemPtr)name == MemPtr.Zero) continue;
                    AvailableSpells.Add(name.Value.ToString(), 0);
                }
                foreach (var spellBtn in spellButtons)
                {
                    if (spellBtn == null || !spellBtn.IsValid) continue;
                    if (spellBtn.SpellDeckSpell == null || !spellBtn.SpellDeckSpell.IsValid) continue;
                    if (spellBtn.SpellDeckSpell.Spell == null || !spellBtn.SpellDeckSpell.Spell.IsValid) continue;

                    var name = spellBtn.SpellDeckSpell.Spell.Name;
                    if ((MemPtr)name == MemPtr.Zero) continue;

                    if (!AvailableSpells.ContainsKey(name.Value.ToString())) continue;

                    int lvl = (int)spellBtn.SpellDeckSpell.LevelIndex; // +1?
                    Handcard hc = new Handcard(name.Value.ToString(), lvl);
                    if (hc.card.name == CardDB.cardName.unknown) CardDB.Instance.collectNewCards(spellBtn);
                    hc.manacost = spellBtn.SpellDeckSpell.Spell.ManaCost;
                    if (hc.card.name == CardDB.cardName.mirror) Mirror = hc;
                    //if (hc.card.needUpdate) CardDB.Instance.cardsAdjustment(spell);

                    ownHandCards.Add(hc);
                }
                if (ownHandCards.Count == 4)
                {
                    if (prevHandCards.Count != 4) prevHandCards = new List<Handcard>(ownHandCards);
                    else
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (ownHandCards[i].card.name == prevHandCards[i].card.name) continue;
                            if (ownHandCards[i].card.name == CardDB.cardName.unknown) continue;
                            prevHandCard = prevHandCards[i];
                            if (Mirror != null)
                            {
                                Mirror.transformTo(prevHandCard);
                                Mirror.mirror = true;
                            }
                            break;
                        }
                    }
                }
                
                var qSpells = ClashEngine.Instance.QueuedSpells;
                if (qSpells != null)
                {
                    if (qSpells.Count() == 0)
                    {
                        if (CastRequestDB.Count != 0) CastRequestDB.Clear();
                    }
                    else
                    {
                        if (CastRequest != null && !CastRequestDB.ContainsKey(CastRequest.SpellName)) CastRequestDB.Add(CastRequest.SpellName, CastRequest);
                        
                        if (CastRequestDBtmp.Count != 0) CastRequestDBtmp.Clear();
                        foreach (var qs in qSpells)
                        {
                            if (qs == null || !qs.IsValid) continue;
                            string name = qs.Name.Value.ToString();
                            if (!CastRequestDBtmp.ContainsKey(name)) CastRequestDBtmp.Add(name, 0);
                            if (CastRequestDB.ContainsKey(name))
                            {
                                //add to pf //TODO: real lvl
                                BoardObj bo = new BoardObj(CardDB.Instance.cardNamestringToEnum(name, "21"), 6);
                                bo.Position = new VectorAI(CastRequestDB[name].Position);
                                bo.ownerIndex = (int)lp.OwnerIndex;
                                bo.frozen = true;
                                bo.GId = getNextGId();
                                switch (bo.type)
                                {
                                    case boardObjType.MOB:
                                        int nums = bo.card.SummonNumber;
                                        if (nums == 0) nums = 1;
                                        else
                                        {
                                            if (bo.card.SpawnCharacter != "")
                                            {
                                                bo = new BoardObj(CardDB.Instance.cardNamestringToEnum(bo.card.SpawnCharacter, "22"), 6);
                                                bo.Position = new VectorAI(CastRequestDB[name].Position);
                                                bo.ownerIndex = (int)lp.OwnerIndex;
                                                bo.frozen = true;
                                                bo.GId = getNextGId();
                                            }
                                        }
                                        for (int i = 0; i < nums; i++)
                                        {
                                            ownMinions.Add(bo);
                                            if (nums > 1)
                                            {
                                                bo = new BoardObj(bo);
                                                bo.GId = getNextGId();
                                            }
                                        }
                                        break;
                                    case boardObjType.BUILDING:
                                        ownBuildings.Add(bo);
                                        break;
                                    case boardObjType.AOE:
                                        break;
                                }
                            }
                        }
                        foreach (var kvp in CastRequestDB.ToArray())
                        {
                            if (!CastRequestDBtmp.ContainsKey(kvp.Key)) CastRequestDB.Remove(kvp.Key);
                        }
                    }
                }
                
                //var projs = om.OfType<Clash.Engine.NativeObjects.Logic.GameObjects.Projectile>();
                //foreach (var proj in projs)
                //{
                //    if (proj != null && proj.IsValid)
                //    {
                //        //TODO: get static data for all objects
                //        //Here we get dynamic data only

                //        CardDB.Instance.collectNewCards(proj);
                //    }
                //}

                var aoes = om.OfType<Clash.Engine.NativeObjects.Logic.GameObjects.AreaEffectObject>();
                foreach (var aoe in aoes)
                {
                    if (!aoe.IsValid) continue;
                    var data = aoe.LogicGameObjectData;
                    if(data == null || !data.IsValid) continue;
                    var name = data.Name;
                    if((MemPtr)name == MemPtr.Zero) continue;

                    BoardObj bo = new BoardObj(CardDB.Instance.cardNamestringToEnum(name.Value.ToString(), "1"));
                    //if (bo.card.needUpdate) CardDB.Instance.cardsAdjustment(aoe);
                    if (bo.card.name == CardDB.cardName.unknown) CardDB.Instance.collectNewCards(aoe);
                    bo.GId = aoe.GlobalId;
                    bo.Position = new VectorAI(aoe.StartPosition);
                    bo.Line = bo.Position.X > 8700 ? 2 : 1;
                    //bo.level = TODO real value
                    //bo.Atk = TODO real value
                    bo.LifeTime = aoe.HealthComponent.RemainingTime;

                    //bo.extraData = data.Field10.ToString();//!!TEST

                    bo.ownerIndex = (int)aoe.OwnerIndex;
                    bool own = bo.ownerIndex == lp.OwnerIndex ? true : (bo.ownerIndex == friendlyOwnerIndex ? true : false);
                    bo.own = own;

                    if (own) ownAreaEffects.Add(bo);
                    else enemyAreaEffects.Add(bo);
                }
                
                var chars = om.OfType<Clash.Engine.NativeObjects.Logic.GameObjects.Character>();
                foreach (var @char in chars)
                {
                    if (!@char.IsValid) continue;
                    var data = @char.LogicGameObjectData;
                    if (data == null || !data.IsValid) continue;
                    var name = data.Name;
                    if ((MemPtr) name == MemPtr.Zero) continue; 

                    BoardObj bo = new BoardObj(CardDB.Instance.cardNamestringToEnum(name.Value.ToString(), "2"));
                    bo.ownerIndex = (int)@char.OwnerIndex;
                    bool own = bo.ownerIndex == lp.OwnerIndex ? true : (bo.ownerIndex == friendlyOwnerIndex ? true : false);
                    bo.own = own;

                    //bo.extraData = data.Field10.ToString();//!!TEST

                    if (bo.card.name == CardDB.cardName.unknown) CardDB.Instance.collectNewCards(@char);
                    else if (bo.ownerIndex == lp.OwnerIndex && bo.card.needUpdate) CardDB.Instance.cardsAdjustment(@char);
                    bo.GId = @char.GlobalId;
                    bo.Position = new VectorAI(@char.StartPosition);
                    bo.Line = bo.Position.X > 8700 ? 2 : 1;
                    bo.level = 1 + (int)@char.TowerLevel;
                    bo.Atk = (int)(bo.card.Atk * lvlToCoef[bo.level]); //TODO: need real value
                    //this.frozen = TODO
                    //this.startFrozen = TODO
                    bo.HP = @char.HealthComponent.CurrentHealth;
                    bo.Shield = @char.HealthComponent.CurrentShieldHealth;
                    bo.LifeTime = @char.HealthComponent.LifeTime - @char.HealthComponent.RemainingTime; //TODO: - find real value for battle stage
                    
                    int tower = 0;
                    switch (bo.Name)
                    {
                        case CardDB.cardName.princesstower:
                            tower = bo.Line;
                            if (bo.own)
                            {
                                if (tower == 1) ownPrincessTower1 = bo;
                                else ownPrincessTower2 = bo;
                            }
                            else
                            {
                                if (tower == 1) enemyPrincessTower1 = bo;
                                else enemyPrincessTower2 = bo;
                            }
                            break;
                        case CardDB.cardName.kingtower:
                            tower = 10 + bo.Line;
                            if (bo.own)
                            {
                                if (ownKingsTower.HP == 0) ownKingsTower = bo;
                                else if (bo.HP < ownKingsTower.HP) ownKingsTower = bo;
                                if (ownKingsTowerPos.X != -1) ownKingsTower.Position.X = ownKingsTowerPos.X;
                                ownKingsTower.ownerIndex = (int)lp.OwnerIndex;
                            }
                            else
                            {
                                if (enemyKingsTower.HP == 0) enemyKingsTower = bo;
                                else if (bo.HP < enemyKingsTower.HP) enemyKingsTower = bo;
                                if (ownKingsTowerPos.X != -1) enemyKingsTower.Position.X = ownKingsTowerPos.X;
                            }
                            break;
                        case CardDB.cardName.kingtowermiddle:
                            tower = 100;
                            break;
                        default:
                            if (own)
                            {
                                switch (bo.type)
                                {
                                    case boardObjType.MOB:
                                        ownMinions.Add(bo);
                                        break;
                                    case boardObjType.BUILDING:
                                        ownBuildings.Add(bo);
                                        break;
                                }
                            }
                            else
                            {
                                switch (bo.type)
                                {
                                    case boardObjType.MOB:
                                        enemyMinions.Add(bo);
                                        break;
                                    case boardObjType.BUILDING:
                                        enemyBuildings.Add(bo);
                                        break;
                                }
                            }
                            break;
                    }
                }
            }

            Playfield p;

            Logger.Debug("#####Stats##### before Initialize playfield {0}", (statTimerRoutine - DateTime.Now).TotalSeconds);

            using (new PerformanceTimer("Initialize playfield."))
            {
                Logger.Debug("################################Routine v.0.8.5 Behavior:{Name:l} v.{Version:l}", Name, Version);
                p = new Playfield
                {
                    BattleTime = battle.BattleTime,
                    suddenDeath = battle.BattleTime.TotalSeconds > 180,
                    ownerIndex = (int)lp.OwnerIndex,
                    ownMana = (int)(lp.Mana - lp.ReservedMana),
                    ownHandCards = ownHandCards,
                    ownAreaEffects = ownAreaEffects,
                    ownMinions = ownMinions,
                    ownBuildings = ownBuildings,
                    ownKingsTower = ownKingsTower,
                    ownPrincessTower1 = ownPrincessTower1,
                    ownPrincessTower2 = ownPrincessTower2,
                    enemyAreaEffects = enemyAreaEffects,
                    enemyMinions = enemyMinions,
                    enemyBuildings = enemyBuildings,
                    enemyKingsTower = enemyKingsTower,
                    enemyPrincessTower1 = enemyPrincessTower1,
                    enemyPrincessTower2 = enemyPrincessTower2,

                    prevCard = prevHandCard,
                    //nextCard = //TODO: Add next card
                };

                p.home = p.ownKingsTower.Position.Y < 15250 ? true : false;

                if (p.ownPrincessTower1.Position == null) p.ownPrincessTower1.Position = p.getDeployPosition(deployDirectionAbsolute.ownPrincessTowerLine1);
                if (p.ownPrincessTower2.Position == null) p.ownPrincessTower2.Position = p.getDeployPosition(deployDirectionAbsolute.ownPrincessTowerLine2);
                if (p.enemyPrincessTower1.Position == null) p.enemyPrincessTower1.Position = p.getDeployPosition(deployDirectionAbsolute.enemyPrincessTowerLine1);
                if (p.enemyPrincessTower2.Position == null) p.enemyPrincessTower2.Position = p.getDeployPosition(deployDirectionAbsolute.enemyPrincessTowerLine2);

                p.initTowers();

                p.print();
                battleLogs.Add(p);
            }

            Logger.Debug("#####Stats##### after Initialize playfield {0}", (statTimerRoutine - DateTime.Now).TotalSeconds);
            statTimeInitPlayfield = DateTime.Now - statTimerRoutine;
            statTimerRoutine = DateTime.Now;

            Cast bc;
            using (new PerformanceTimer("GetBestCast"))
            {
                //DateTime statBehaviorCalcStart = DateTime.Now;
                bc = this.GetBestCast(p);

                CastRequest = null;
                if (bc != null && bc.Position != null)
                {
                    if (p.ownMana + 1 >= bc.hc.manacost) CastRequest = new CastRequest(bc.SpellName, bc.Position.ToVector2f());
                    Logger.Debug("CastRequest {SpellName:l} {Position:l}", bc.SpellName, CastRequest == null ? bc.Position?.ToString() : CastRequest.Position.ToString());
                }
                else Logger.Debug("Waiting for cast, maybe next tick...");
                statTimeInsideBehavior = DateTime.Now - statTimerRoutine;

                //stat info

                statSumTimeOutsideRoutine += statTimeOutsideRoutine;
                statSumTimeInitPlayfield += statTimeInitPlayfield;
                statSumTimeInsideBehavior += statTimeInsideBehavior;
                statNumSuccessfulEntrances++;
                int objsCount = ownAreaEffects.Count + enemyAreaEffects.Count + ownMinions.Count + enemyMinions.Count + ownBuildings.Count + enemyBuildings.Count; //without HandCards
                if (objsCount == 0) objsCount = 2;

                Logger.Debug("Hint: ne:NumberEntrances  CT:CalculationTime  aCT:AverageCalculationTimePer1Game  tpo:TimePer1Object  ToR:TimeOutsideRoutine");
                Logger.Debug("#####Stats### ne:{NumberEntrances} Behavior(CT/aCT/tpo):{BehaviorCalcTime}/{averageBCT}/{timePer1Object} Playfield(CT/aCT/tpo):{PlayfieldCreationTime}/{averagePCT}/{timePer1Object} outsideRoutine(ToR/aToR/tpo):{timeOutsideRoutine}/{averageToR}/{timePer1Object}",
                    statNumSuccessfulEntrances, statTimeInsideBehavior.TotalSeconds, (statSumTimeInsideBehavior / statNumSuccessfulEntrances).TotalSeconds, (statTimeInsideBehavior / objsCount).TotalSeconds,
                     statTimeInitPlayfield.TotalSeconds, (statSumTimeInitPlayfield / statNumSuccessfulEntrances).TotalSeconds, (statTimeInitPlayfield / objsCount).TotalSeconds,
                     statTimeOutsideRoutine.TotalSeconds, (statSumTimeOutsideRoutine / (statNumSuccessfulEntrances > 1 ? statNumSuccessfulEntrances - 1 : 1)).TotalSeconds, (statTimeOutsideRoutine / objsCount).TotalSeconds);
                
                statTimerRoutine = DateTime.Now;
                
                return CastRequest;
            }
        }

        private uint getNextGId()
        {
            return nextGId++;
        }
    }
}