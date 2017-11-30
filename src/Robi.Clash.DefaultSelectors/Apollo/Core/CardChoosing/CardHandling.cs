using System.Linq;
using Robi.Clash.DefaultSelectors.Apollo.Core.Classification;
using Robi.Clash.DefaultSelectors.Apollo.Core.Decisions;
using Robi.Common;
using Serilog;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.CardChoosing
{
    internal class CardHandling
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<CardHandling>();

        public static Handcard All(Playfield p, FightState currentSituation, out dynamic dsDestination)
        {
            // TODO: Use more current situation
            Logger.Debug("Path: Spell - All");
            Logger.Debug("FightState: " + currentSituation);

            var damagingSpell = SpellChoosing.DamagingSpellDecision(p, out dsDestination);
            if (damagingSpell != null)
                return damagingSpell;

            var aoeCard = MobChoosing.AOEDecision(p);
            if (aoeCard != null)
                return aoeCard;

            var bigGroupCard = MobChoosing.BigGroupDecision(p, currentSituation);
            if (bigGroupCard != null)
                return bigGroupCard;

            if (p.enemyMinions.Any(n => n.Transport == transportType.AIR))
            {
                Logger.Debug("AttackFlying Needed");
                var atkFlying = ClassificationHandling
                    .GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsFlyingAttack)
                    .FirstOrDefault();
                if (atkFlying != null)
                    return atkFlying;
            }

            if (DeployBuildingDecision(p, out var buildingCard, currentSituation))
                if (buildingCard != null)
                    return new Handcard(buildingCard.name, buildingCard.lvl);

            // ToDo: Don´t play a tank, if theres already one on this side
            if ((int) currentSituation < 3 || (int) currentSituation > 6) // Just not at Under Attack
            {
                var tank = ClassificationHandling.GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsTank)
                    .OrderBy(n => n.card.MaxHP);
                var lt = tank.LastOrDefault();
                if (lt != null && lt.manacost <= p.ownMana)
                    return lt;
            }

            // ToDo: Decision for building attacker
            if ((int) currentSituation > 6 && (int) currentSituation < 10)
            {
                var buildingAtkCard = ClassificationHandling
                    .GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsBuildingAttacker).FirstOrDefault();
                if (buildingAtkCard != null && buildingAtkCard.manacost <= p.ownMana)
                    return buildingAtkCard;
            }

            if ((int) currentSituation < 3)
            {
                var highestHP = ClassificationHandling.GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.All)
                    .Where(n => n.manacost - p.ownMana <= 0)
                    .OrderBy(n => n.card.MaxHP).LastOrDefault();

                return highestHP;
            }

            var rangerCard = ClassificationHandling.GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsRanger)
                .FirstOrDefault();
            if (rangerCard != null && rangerCard.manacost <= p.ownMana)
                return rangerCard;

            var damageDealerCard = ClassificationHandling.GetOwnHandCards(p, boardObjType.MOB,
                SpecificCardType.MobsDamageDealer, MoreSpecificMobCardType.NoBigGroup).FirstOrDefault();
            if (damageDealerCard != null && damageDealerCard.manacost <= p.ownMana)
                return damageDealerCard;

            //if((int)currentSituation >= 3 && (int)currentSituation <= 6)
            //    return Classification.GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.Mobs).FirstOrDefault();

            Logger.Debug("Wait - No card selected...");
            return null;
        }

        //private static Handcard DefenseTroop(Playfield p)
        //{
        //    Logger.Debug("Path: Spell - DefenseTroop");

        //    if (IsAOEAttackNeeded(p))
        //    {
        //        var atkAOE = GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsAOEGround).FirstOrDefault(); // Todo: just AOE-Attack

        //        if (atkAOE != null)
        //            return new Handcard(atkAOE.name, atkAOE.lvl);
        //    }

        //    if (p.enemyMinions.Where(n => n.Transport == transportType.AIR).Count() > 0)
        //    {
        //        var atkFlying = GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsFlyingAttack).FirstOrDefault();
        //        if (atkFlying != null)
        //            return new Handcard(atkFlying.name, atkFlying.lvl);
        //    }

        //    var powerSpell = powerCard(p).FirstOrDefault();
        //    if (powerSpell != null)
        //        return new Handcard(powerSpell.name, powerSpell.lvl);

        //    return cycleCard(p).FirstOrDefault();
        //}

        //private static Handcard Defense(Playfield p, FightState currentSituation, out VectorAI choosedPosition)
        //{
        //    Logger.Debug("Path: Spell - Defense");
        //    choosedPosition = null;
        //    IEnumerable<Handcard> damagingSpells = GetOwnHandCards(p, boardObjType.AOE, SpecificCardType.SpellsDamaging);


        //    Handcard damagingSpell = DamagingSpellDecision(p, out choosedPosition);
        //    if (damagingSpell != null)
        //        return new Handcard(damagingSpell.name, damagingSpell.lvl);

        //    Handcard aoeCard = AOEDecision(p, out choosedPosition, currentSituation);
        //    if (aoeCard != null)
        //        return aoeCard;

        //    if (p.enemyMinions.Where(n => n.Transport == transportType.AIR).Count() > 0)
        //    {
        //        Handcard atkFlying = GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsFlyingAttack).FirstOrDefault();
        //        if (atkFlying != null)
        //            return atkFlying;
        //    }

        //    var powerSpell = powerCard(p).FirstOrDefault();
        //    if (powerSpell != null)
        //        return new Handcard(powerSpell.name, powerSpell.lvl);

        //    return p.ownHandCards.FirstOrDefault();
        //}


        // ToDo: Create a Building concept


        // TODO: Check this out

        public static bool DeployBuildingDecision(Playfield p, out Handcard buildingCard, FightState currentSituation)
        {
            buildingCard = null;
            var condition = false;

            var hcMana = ClassificationHandling
                .GetOwnHandCards(p, boardObjType.BUILDING, SpecificCardType.BuildingsMana)
                .FirstOrDefault();
            var hcDefense = ClassificationHandling
                .GetOwnHandCards(p, boardObjType.BUILDING, SpecificCardType.BuildingsDefense)
                .FirstOrDefault();
            var hcAttack = ClassificationHandling
                .GetOwnHandCards(p, boardObjType.BUILDING, SpecificCardType.BuildingsAttack)
                .FirstOrDefault();
            var hcSpawning = ClassificationHandling
                .GetOwnHandCards(p, boardObjType.BUILDING, SpecificCardType.BuildingsSpawning).FirstOrDefault();


            // Just for Defense
            if ((int) currentSituation < 3) return false;

            if (hcMana != null) condition = true;
            if (hcSpawning != null) condition = true;
            if (hcDefense != null) condition = true;

            // ToDo: Attack condition

            // ToDo: Underattack condition

            return condition;
        }

        public static Handcard GetOppositeCard(Playfield p, FightState currentSituation)
        {
            switch (currentSituation)
            {
                case FightState.UAKTL1:
                case FightState.UAKTL2:
                case FightState.UAPTL1:
                case FightState.UAPTL2:
                case FightState.AKT:
                case FightState.APTL1:
                case FightState.APTL2:
                case FightState.DKT:
                case FightState.DPTL1:
                case FightState.DPTL2:
                {
                    var defender = DefenderDecision.GetBestDefender(p);

                    if (defender == null)
                        return null;

                    Logger.Debug("BestDefender: {Defender}", defender.ToString());
                    var spell = KnowledgeBase.Instance.getOppositeToAll(p, defender,
                        DeploymentDecision.CanWaitDecision(p, currentSituation));

                    if (spell != null && spell.hc != null)
                    {
                        Logger.Debug("Spell: {Sp} - MissingMana: {MM}", spell.hc.name, spell.hc.missingMana);
                        if (spell.hc.missingMana == 100) // Oposite-Card is already on the field
                            return null;
                        if (spell.hc.missingMana > 0)
                            return null;
                        return spell.hc;
                    }
                }
                    break;
                case FightState.START:
                case FightState.WAIT:
                default:
                    break;
            }
            return null;
        }
    }
}