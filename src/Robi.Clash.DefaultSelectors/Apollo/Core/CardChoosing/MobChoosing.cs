using System.Linq;
using Robi.Clash.DefaultSelectors.Apollo.Core.Classification;
using Robi.Clash.DefaultSelectors.Apollo.Other;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.CardChoosing
{
    internal class MobChoosing
    {
        public static Handcard AOEDecision(Playfield p)
        {
            Handcard aoeGround = null, aoeAir = null;

            var objGround =
                BoardObjHelper.EnemyCharacterWithTheMostEnemiesAround(p, out var biggestEnemieGroupCount,
                    transportType.GROUND);
            if (biggestEnemieGroupCount > 3)
                aoeGround = ClassificationHandling
                    .GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsAOE, MoreSpecificMobCardType.AOEGround)
                    .FirstOrDefault();

            var objAir =
                BoardObjHelper.EnemyCharacterWithTheMostEnemiesAround(p, out biggestEnemieGroupCount,
                    transportType.AIR);
            if (biggestEnemieGroupCount > 3)
                aoeAir = ClassificationHandling
                    .GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsAOE, MoreSpecificMobCardType.AOEAll)
                    .FirstOrDefault();

            return aoeAir ?? aoeGround;
        }

        public static Handcard BigGroupDecision(Playfield p, FightState fightState)
        {
            var aoe = p.enemyMinions.Where(n => n.card.aoeAir || n.card.aoeGround);
            var tanks = p.ownMinions.Where(n => MobClassification.IsMobsTankCurrentHP(n));

            // ToDo: Improve condition 
            switch (fightState)
            {
                case FightState.UAPTL1:
                case FightState.UAKTL1:
                case FightState.APTL1:
                case FightState.DPTL1:
                    if (aoe.Any(n => n.Line == 1) || !tanks.Any(n => n.Line == 1))
                        return null;
                    break;
                case FightState.UAKTL2:
                case FightState.UAPTL2:
                case FightState.APTL2:
                case FightState.DPTL2:
                    if (aoe.Any(n => n.Line == 2) || !tanks.Any(n => n.Line == 2))
                        return null;
                    break;
                case FightState.AKT:
                case FightState.DKT:
                    if (aoe.Any(n => n.Line == 1) || !tanks.Any(n => n.Line == 1)
                        || aoe.Any(n => n.Line == 2) || !tanks.Any(n => n.Line == 2))
                        return null;
                    break;
                default:
                    break;
            }
            return ClassificationHandling.GetOwnHandCards(p, boardObjType.MOB, SpecificCardType.MobsBigGroup)
                .FirstOrDefault();
        }

        public static Handcard GetMobInPeace(Playfield p, FightState currentSituation)
        {
            if (PlayfieldAnalyse.lines[0].Danger <= Level.Low || PlayfieldAnalyse.lines[1].Danger <= Level.Low)
            {
                var tanks = p.ownMinions.Where(n => MobClassification.IsMobsTankCurrentHP(n))
                    .OrderBy(n => n.HP).ToArray();
                switch (currentSituation)
                {
                    case FightState.DPTL1:
                    case FightState.APTL1:
                        var tankL1 = tanks.Where(n => n.Line == 1).OrderBy(n => n.HP).FirstOrDefault();

                        if (tankL1 != null)
                            return p.getPatnerForMobInPeace(tankL1);
                        else
                            return p.getPatnerForMobInPeace(p.ownMinions.Where(n => n.Line == 1).OrderBy(n => n.Atk)
                                .FirstOrDefault());
                    case FightState.DPTL2:
                    case FightState.APTL2:
                        var tankL2 = tanks.Where(n => n.Line == 2).OrderBy(n => n.HP).FirstOrDefault();

                        if (tankL2 != null)
                            return p.getPatnerForMobInPeace(tankL2);
                        else
                            return p.getPatnerForMobInPeace(p.ownMinions.Where(n => n.Line == 2).OrderBy(n => n.Atk)
                                .FirstOrDefault());
                    case FightState.DKT:
                    case FightState.AKT:
                        if (tanks.FirstOrDefault() != null)
                            return p.getPatnerForMobInPeace(tanks.FirstOrDefault());
                        else
                            return p.getPatnerForMobInPeace(p.ownMinions.OrderBy(n => n.Atk).FirstOrDefault());
                    case FightState.UAPTL1:
                    case FightState.UAPTL2:
                    case FightState.UAKTL1:
                    case FightState.UAKTL2:
                    case FightState.START:
                    case FightState.WAIT:
                    default:
                        break;
                }
            }
            return null;
        }
    }
}