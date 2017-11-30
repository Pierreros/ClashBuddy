using System;
using System.Linq;
using Robi.Clash.DefaultSelectors.Apollo;
using Robi.Clash.DefaultSelectors.Apollo.Core;
using Robi.Clash.DefaultSelectors.Apollo.Core.CardChoosing;
using Robi.Clash.DefaultSelectors.Apollo.Core.Classification;
using Robi.Clash.DefaultSelectors.Apollo.HotFixes;
using Robi.Clash.DefaultSelectors.Settings;
using Robi.Clash.DefaultSelectors.Apollo.Core.Decisions;
using Robi.Clash.DefaultSelectors.Apollo.Core.Positioning;
using Robi.Common;
using Robi.Engine.Settings;
using Serilog;


namespace Robi.Clash.DefaultSelectors.Behaviors
{
    public class Apollo : BehaviorBase
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<Apollo>();

        private static bool _startLoadedDeploy;
        private static FightState _currentSituation;

        public override Cast GetBestCast(Playfield p)
        {
            Handcard hc = null;
            #region Apollo Magic
            BuildCurrentState(p);

            // Highest priority -> Can we kill the enemy with a spell
            //var finisherTower = DeploymentDecision.IsEnemyKillWithSpellPossible(p, out var hc);
            //if (finisherTower != null && hc?.manacost > p.ownMana) return new Cast(hc.name, finisherTower.Position, hc);
            // ------------------------------------------------------

            // Card Choosing
            //hc = CardHandling.GetOppositeCard(p, _currentSituation) ?? MobChoosing.GetMobInPeace(p, _currentSituation);

            //if (hc == null)
            //{
            var hcApollo = CardHandling.All(p, _currentSituation, out dynamic dsDestination);
            if (hcApollo != null && hcApollo.manacost < p.ownMana)
                hc = hcApollo;
            //}
            // ------------------------------------------------------
            if (hc == null)
                return null;

            // Positioning
            var nextPosition = SpecialPositionHandling.GetPosition(p, hc) ??
                               PositionHandling.GetNextSpellPosition(_currentSituation, hc, p, dsDestination);
            var bc = new Cast(hc.name, nextPosition, hc);
            // ------------------------------------------------------
            #endregion

            Logger.Debug("BestCast:" + bc.SpellName + " " + bc.Position);
            return bc.hc?.manacost > p.ownMana ? null : bc;
        }

        private static void BuildCurrentState(Playfield p)
        {
            PlayfieldAnalyse.AnalyseLines(p); // Danger- and Chancelevel
            _currentSituation = GetCurrentFightState(p); // Attack, Defense or UnderAttack (and where it is)
        }

        //private static Handcard ChooseCard()
        //{
            
        //}


        public static FightState GetCurrentFightState(Playfield p)
        {
            switch (Setting.FightStyle)
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

        private static FightState GetCurrentFightStateBalanced(Playfield p)
        {
            var fightState = FightState.WAIT;

            if (GameBeginning)
            {
                _startLoadedDeploy = false;
                return FightStateDecision.GameBeginningDecision(p, out GameBeginning);
            }

            var dangerOrAttackLine = DeploymentDecision.DangerOrBestAttackingLine(p);

            if (dangerOrAttackLine < 0)
            {
                Logger.Debug("Danger");
                _startLoadedDeploy = false;
                fightState = FightStateDecision.DangerousSituationDecision(p, dangerOrAttackLine * -1);
            }
            else if (dangerOrAttackLine > 0)
            {
                Logger.Debug("Chance");
                _startLoadedDeploy = false;
                fightState = FightStateDecision.GoodAttackChanceDecision(p, dangerOrAttackLine);
            }
            else
            {
                if (p.ownMana >= Setting.ManaTillDeploy)
                {
                    _startLoadedDeploy = true;
                    fightState = FightStateDecision.DefenseDecision(p);
                }

                if (_startLoadedDeploy)
                    fightState = FightStateDecision.DefenseDecision(p);
            }

            //Logger.Debug("FightSate = {0}", fightState.ToString());
            return fightState;
        }

        private static FightState GetCurrentFightStateDefensive(Playfield p)
        {
            if (GameBeginning)
                return FightStateDecision.GameBeginningDecision(p, out GameBeginning);

            if (!p.noEnemiesOnMySide())
                return FightStateDecision.EnemyIsOnOurSideDecision(p);
            if (p.enemyMinions.Count > 1)
                return FightStateDecision.EnemyHasCharsOnTheFieldDecision(p);
            return FightStateDecision.DefenseDecision(p);
        }

        private static FightState GetCurrentFightStateRusher(Playfield p)
        {
            return !p.noEnemiesOnMySide() ? FightStateDecision.EnemyIsOnOurSideDecision(p) : FightStateDecision.AttackDecision(p);
        }

        public static void FillSettings()
        {
#if RELEASE
            Setting.FightStyle = Settings.FightStyle;
            Setting.KingTowerSpellDamagingHealth = Settings.KingTowerSpellDamagingHealth;
            Setting.ManaTillDeploy = Settings.ManaTillDeploy;
            Setting.ManaTillFirstAttack = Settings.ManaTillFirstAttack;
            Setting.MinHealthAsTank = Settings.MinHealthAsTank;
            Setting.SpellCorrectionConditionCharCount = Settings.SpellCorrectionConditionCharCount;
            Setting.DangerSensitivity = Settings.DangerSensitivity;
            Setting.ChanceSensitivity = Settings.ChanceSensitivity;
#else
                Setting.FightStyle = FightStyle.Balanced;
                Setting.KingTowerSpellDamagingHealth = 400;
                Setting.ManaTillDeploy = 10;
                Setting.ManaTillFirstAttack = 10;
                Setting.MinHealthAsTank = 1200;
                Setting.SpellCorrectionConditionCharCount = 5;
                Setting.DangerSensitivity = 2;
                Setting.ChanceSensitivity = 2;
            #endif
        }

        public override float GetPlayfieldValue(Playfield p)
        {
            if (p.value >= -2000000) return p.value;
            const int retval = 0;
            return retval;
        }

        public override int GetBoValue(BoardObj bo, Playfield p)
        {
            const int retval = 5;
            return retval;
        }

        public override int GetPlayCardPenalty(CardDB.Card card, Playfield p)
        {
            return 0;
        }

        private static void DebugThings(Playfield p)
        {
            var t = typeof(Line);
            var properties = t.GetProperties();

            PlayfieldAnalyse.AnalyseLines(p);
            var line = PlayfieldAnalyse.lines;

            foreach (var nP in properties)
                Console.WriteLine(nP.GetValue(line).ToString());

            var damagingSpells = ClassificationHandling.GetOwnHandCards(p, boardObjType.AOE, SpecificCardType.SpellsDamaging);
            if (damagingSpells != null)
            {
                var radiusOrderedDs = damagingSpells.OrderBy(n => n.card.DamageRadius).FirstOrDefault();
                if (radiusOrderedDs != null)
                {
                    var group = p.getGroup(false, 200, boPriority.byTotalNumber, radiusOrderedDs.card.DamageRadius);
                }
            }

            Logger.Debug("Name: " + p.ownKingsTower.Name);
            Logger.Debug("Name: " + p.ownPrincessTower1.Name);
            Logger.Debug("Name: " + p.ownPrincessTower2.Name);

            var i1 = p.ownKingsTower.HP;
            var i2 = p.ownPrincessTower1.HP;
            var i3 = p.ownPrincessTower2.HP;

            Logger.Debug("test");
        }

        #region

        public override string Name => "Apollo";

        public override string Description => "1vs1; Please lean back and let me Apollo do the work...";

        public override string Author => "Peros_";

        public override Version Version => new Version(1, 8, 1, 0);
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
    }
}