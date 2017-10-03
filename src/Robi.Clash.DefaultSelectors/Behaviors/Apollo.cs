﻿namespace Robi.Clash.DefaultSelectors.Behaviors
{
    using Robi.Clash.Engine.NativeObjects.Logic.GameObjects;
    using Common;
    using Engine.NativeObjects.Native;
    using Robi.Clash.DefaultSelectors.Apollo;
    using Robi.Engine.Settings;
    using Serilog;
    using Settings;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Robi.Clash.Engine;
    using System.Reflection;

    public class Apollo : BehaviorBase
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<Apollo>();

        #region
        public override string Name => "Apollo";

        public override string Description => "1vs1; Please lean back and let me Apollo do the work...";

        public override string Author => "Peros_";

        public override Version Version => new Version(1, 7, 0, 0);
        public override Guid Identifier => new Guid("{669f976f-23ce-4b97-9105-a21595a394bf}");

        private static ApolloSettings Settings => SettingsManager.GetSetting<ApolloSettings>("Apollo");

        public override void Initialize()
        {
            base.Initialize();
            SettingsManager.RegisterSettings(Name, new ApolloSettings());
            FillSettings();
        }

        public override void Deinitialize()
        {
            SettingsManager.UnregisterSettings(Name);
            base.Deinitialize();
        }
        #endregion

        private static bool StartLoadedDeploy = false;
        private static FightState currentSituation;
        private static Line[] Lines = new Line[2];

        public override Cast GetBestCast(Playfield p)
        {
            //DebugThings(p);
            Cast bc = null;
            Logger.Debug("Home = {Home}", p.home);

            #region Apollo Magic
            Logger.Debug("Part: Get CurrentSituation");
            PlayfieldAnalyse.AnalyseLines(p);
            currentSituation = GetCurrentFightState(p);
            Logger.Debug("Part: GetOppositeCard");
            Handcard hc = CardChoosing.GetOppositeCard(p, currentSituation);

            if (hc == null)
            {
                Logger.Debug("Part: SpellApolloWay");
                Handcard hcApollo = SpellMagic(p, currentSituation, out  VectorAI choosedPosition);

                if (hcApollo != null)
                {
                    hc = hcApollo;

                    if (choosedPosition != null)
                        return new Cast(hcApollo.name, choosedPosition, hcApollo);
                }

            }

            if (hc == null)
                return null;

            Logger.Debug("Part: GetSpellPosition");
            VectorAI nextPosition = PositionChoosing.GetNextSpellPosition(currentSituation, hc, p);
            bc = new Cast(hc.name, nextPosition, hc);
            #endregion

            if (bc != null) Logger.Debug("BestCast:" + bc.SpellName + " " + bc.Position.ToString());
            else Logger.Debug("BestCast: null");

            return bc;
        }

        

        private static void DebugThings(Playfield p)
        {
            Type t = typeof(Line);
            PropertyInfo[] properties = t.GetProperties();

            PlayfieldAnalyse.AnalyseLines(p);
            Line[] line = PlayfieldAnalyse.lines;

            foreach (PropertyInfo nP in properties)
            {
                Console.WriteLine(nP.GetValue(line).ToString());
            }

            IEnumerable<Handcard> damagingSpells = Classification.GetOwnHandCards(p, boardObjType.AOE, SpecificCardType.SpellsDamaging);
            if (damagingSpells != null)
            {
                IOrderedEnumerable<Handcard> radiusOrderedDS = damagingSpells.OrderBy(n => n.card.DamageRadius);
                group Group = p.getGroup(false, 200, boPriority.byTotalNumber, radiusOrderedDS.FirstOrDefault().card.DamageRadius);
            }

            Logger.Debug("Name: " + p.ownKingsTower.Name);
            Logger.Debug("Name: " + p.ownPrincessTower1.Name);
            Logger.Debug("Name: " + p.ownPrincessTower2.Name);

            int i1 = p.ownKingsTower.HP;
            int i2 = p.ownPrincessTower1.HP;
            int i3 = p.ownPrincessTower2.HP;

            int abc = 10;
            Logger.Debug("test");
        }

        private static Handcard SpellMagic(Playfield p, FightState currentSituation, out VectorAI choosedPosition)
        {
            choosedPosition = null;
            switch (currentSituation)
            {
                case FightState.UAKT:
                case FightState.UALPT:
                case FightState.UARPT:
                case FightState.DKT:
                case FightState.DLPT:
                case FightState.DRPT:
                case FightState.AKT:
                case FightState.ALPT:
                case FightState.ARPT:
                    return CardChoosing.All(p, currentSituation, out choosedPosition);
                case FightState.START:
                    return null;
                case FightState.WAIT:
                    return null;
                default:
                    return CardChoosing.All(p, currentSituation, out choosedPosition);
            }
        }

        public static FightState GetCurrentFightState(Playfield p)
        {
            try
            {
                switch ((FightStyle)Settings.FightStyle)
                {
                    case FightStyle.Defensive:
                        return GetCurrentFightStateDefensive(p);
                    case FightStyle.Balanced:
                        return GetCurrentFightStateBalanced(p);
                    case FightStyle.Rusher:
                        return GetCurrentFightStateRusher(p);
                    default:
                        return FightState.DKT;
                }
            }
            catch (Exception e)
            {
                return GetCurrentFightStateBalanced(p);
            }

        }

        private static FightState GetCurrentFightStateBalanced(Playfield p)
        {

            FightState fightState = FightState.WAIT;

            if (GameBeginning)
            {
                StartLoadedDeploy = false;
                return Decision.GameBeginningDecision(p, GameBeginning);
            }

            //if (!p.noEnemiesOnMySide())
            //{
            //    StartLoadedDeploy = false;
            //    fightState = EnemyIsOnOurSideDecision(p);
            //}


            int dangerOrAttackLine = Decision.DangerOrBestAttackingLine(p);


            if (dangerOrAttackLine < 0)
            {
                Logger.Debug("Danger");
                StartLoadedDeploy = false;
                fightState = Decision.DangerousSituationDecision(p, dangerOrAttackLine * (-1));
            }
            else if (dangerOrAttackLine > 0)
            {
                Logger.Debug("Chance");
                StartLoadedDeploy = false;
                fightState = Decision.GoodAttackChanceDecision(p, dangerOrAttackLine);
            }
            else
            {
                try
                {
                    if (p.ownMana >= Settings.ManaTillDeploy)
                    {
                        StartLoadedDeploy = true;
                        fightState = Decision.DefenseDecision(p);
                    }
                }
                catch (Exception) { StartLoadedDeploy = true; }

                if (StartLoadedDeploy)
                    fightState = Decision.DefenseDecision(p);
            }

            //Logger.Debug("FightSate = {0}", fightState.ToString());
            return fightState;
        }

        private static FightState GetCurrentFightStateDefensive(Playfield p)
        {
            if (GameBeginning)
                return Decision.GameBeginningDecision(p, GameBeginning);

            if (!p.noEnemiesOnMySide())
                return Decision.EnemyIsOnOurSideDecision(p);
            else if (p.enemyMinions.Count > 1)
                return Decision.EnemyHasCharsOnTheFieldDecision(p);
            else
                return Decision.DefenseDecision(p);
        }

        private static FightState GetCurrentFightStateRusher(Playfield p)
        {
            if (!p.noEnemiesOnMySide())
                return Decision.EnemyIsOnOurSideDecision(p);
            else
                return Decision.AttackDecision(p);
        }

        private static void FillSettings()
        {
            Apollo.Settings.FightStyle = Settings.FightStyle; 
            Apollo.Settings.KingTowerSpellDamagingHealth = Settings.KingTowerSpellDamagingHealth;
            Apollo.Settings.ManaTillDeploy = Settings.ManaTillDeploy;
            Apollo.Settings.ManaTillFirstAttack = Settings.ManaTillFirstAttack;
            Apollo.Settings.MinHealthAsTank = Settings.MinHealthAsTank;
            Apollo.Settings.SpellCorrectionConditionCharCount = Settings.SpellCorrectionConditionCharCount;
            Apollo.Settings.SpellDeployConditionCharCount = Settings.SpellDeployConditionCharCount;
        }

        public override float GetPlayfieldValue(Playfield p)
        {
            if (p.value >= -2000000) return p.value;
            int retval = 0;
            return retval;
        }

        public override int GetBoValue(BoardObj bo, Playfield p)
        {
            int retval = 5;
            return retval;
        }

        public override int GetPlayCardPenalty(CardDB.Card card, Playfield p)
        {
            return 0;
        }
    }
}