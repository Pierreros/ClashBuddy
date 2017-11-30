using Robi.Clash.DefaultSelectors.Apollo.Core.Decisions;
using Robi.Clash.DefaultSelectors.Apollo.Other;
using Robi.Common;
using Serilog;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Positioning
{
    internal class PositionHandling
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<PositionHandling>();

        public static VectorAI GetNextSpellPosition(FightState gameState, Handcard hc, Playfield p, dynamic dsDestination)
        {
            if (hc?.card == null)
                return null;

            VectorAI choosedPosition = null;


            if (hc.card.type == boardObjType.AOE || hc.card.type == boardObjType.PROJECTILE)
            {
                Logger.Debug("AOE or PROJECTILE");
                return SpellPositioning.GetPositionOfTheBestDamagingSpellDeploy(p, dsDestination);
            }

            // ToDo: Handle Defense Gamestates
            switch (gameState)
            {
                case FightState.UAKTL1:
                    choosedPosition = UAKT(p, hc, 1);
                    break;
                case FightState.UAKTL2:
                    choosedPosition = UAKT(p, hc, 2);
                    break;
                case FightState.UAPTL1:
                    choosedPosition = UAPTL1(p, hc);
                    break;
                case FightState.UAPTL2:
                    choosedPosition = UAPTL2(p, hc);
                    break;
                case FightState.AKT:
                    choosedPosition = AKT(p, hc);
                    break;
                case FightState.APTL1:
                    choosedPosition = APTL1(p, hc);
                    break;
                case FightState.APTL2:
                    choosedPosition = APTL2(p, hc);
                    break;
                case FightState.DKT:
                    choosedPosition = DKT(p, hc, 0);
                    break;
                case FightState.DPTL1:
                    choosedPosition = DPTL1(p, hc);
                    break;
                case FightState.DPTL2:
                    choosedPosition = DPTL2(p, hc);
                    break;
                default:
                    //Logger.Debug("GameState unknown");
                    break;
            }

            //Logger.Debug("GameState: {GameState}", gameState.ToString());
            //Logger.Debug("nextPosition: " + nextPosition);

            return choosedPosition;
        }


        #region UnderAttack

        private static VectorAI UAKT(Playfield p, Handcard hc, int line)
        {
            return DKT(p, hc, line);
        }

        private static VectorAI UAPTL1(Playfield p, Handcard hc)
        {
            return DPTL1(p, hc);
        }

        private static VectorAI UAPTL2(Playfield p, Handcard hc)
        {
            return DPTL2(p, hc);
        }

        #endregion

        #region Defense

        private static VectorAI DKT(Playfield p, Handcard hc, int line)
        {
            // ToDo: Improve
            if (line == 0)
                line = p.enemyPrincessTower1.HP < p.enemyPrincessTower2.HP ? 1 : 2;

            if (hc.card.type == boardObjType.MOB)
            {
                if (hc.card.MaxHP >= Setting.MinHealthAsTank)
                    if (line == 2)
                    {
                        Logger.Debug("KT RightUp");
                        var v = p.getDeployPosition(p.ownKingsTower.Position, deployDirectionRelative.RightUp, 100);
                        return v;
                    }
                    else
                    {
                        Logger.Debug("KT LeftUp");
                        var v = p.getDeployPosition(p.ownKingsTower.Position, deployDirectionRelative.LeftUp, 100);
                        return v;
                    }

                if (hc.card.Transport == transportType.AIR)
                    return p.getDeployPosition(line == 2
                        ? deployDirectionAbsolute.ownPrincessTowerLine2
                        : deployDirectionAbsolute.ownPrincessTowerLine1);
                if (line == 2)
                {
                    Logger.Debug("BehindKT: Line2");
                    var position = p.getDeployPosition(deployDirectionAbsolute.behindKingsTowerLine2);
                    return position;
                }
                else
                {
                    Logger.Debug("BehindKT: Line1");
                    var position = p.getDeployPosition(deployDirectionAbsolute.behindKingsTowerLine1);
                    return position;
                }
            }
            if (hc.card.type == boardObjType.BUILDING)
                return BuildingPositioning.GetPositionOfTheBestBuildingDeploy(p, hc, FightState.DKT);
            Logger.Debug("DKT: Handcard equals NONE!");
            return p.ownKingsTower?.Position;
        }

        private static VectorAI DPTL1(Playfield p, Handcard hc)
        {
            var lPT = p.ownPrincessTower1;

            if (lPT?.Position == null)
                return DKT(p, hc, 1);

            switch (hc.card.type)
            {
                case boardObjType.MOB:
                    return MobPositioning.PrincessTowerCharacterDeploymentCorrection(lPT.Position, p, hc);
                case boardObjType.BUILDING:
                    //switch ((cardToDeploy as CardBuilding).Type)
                    //{
                    //    case BuildingType.BuildingDefense:
                    //    case BuildingType.BuildingSpawning:
                    return BuildingPositioning.GetPositionOfTheBestBuildingDeploy(p, hc, FightState.DPTL1);
            }
            return lPT.Position;
        }

        private static VectorAI DPTL2(Playfield p, Handcard hc)
        {
            var rPT = p.ownPrincessTower2;

            if (rPT == null && rPT.Position == null)
                return DKT(p, hc, 2);

            if (hc.card.type == boardObjType.MOB)
                return MobPositioning.PrincessTowerCharacterDeploymentCorrection(rPT.Position, p, hc);
            if (hc.card.type == boardObjType.BUILDING)
                return BuildingPositioning.GetPositionOfTheBestBuildingDeploy(p, hc, FightState.DPTL2);
            return rPT.Position;
        }

        #endregion

        #region Attack

        private static VectorAI AKT(Playfield p, Handcard hc)
        {
            Logger.Debug("AKT");

            if (p.enemyPrincessTowers.Count == 2)
                if (p.enemyPrincessTower1.HP < p.enemyPrincessTower2.HP)
                    return APTL1(p, hc);
                else
                    return APTL2(p, hc);

            if (p.enemyPrincessTower1.HP == 0 && p.enemyPrincessTower2.HP > 0)
                return APTL1(p, hc);

            if (p.enemyPrincessTower2.HP == 0 && p.enemyPrincessTower1.HP > 0)
                return APTL2(p, hc);

            var position = p.enemyKingsTower?.Position;

            //if (Decision.SupportDeployment(p, 1))
            //    position = p.getDeployPosition(position, deployDirectionRelative.Down, 500);

            return position;
        }


        private static VectorAI APTL1(Playfield p, Handcard hc)
        {
            return APT(p, hc, 1);
        }

        private static VectorAI APTL2(Playfield p, Handcard hc)
        {
            return APT(p, hc, 2);
        }

        private static VectorAI APT(Playfield p, Handcard hc, int line)
        {
            Logger.Debug("ALPT");

            if (hc.card.type == boardObjType.BUILDING)
                return line == 1
                    ? BuildingPositioning.GetPositionOfTheBestBuildingDeploy(p, hc, FightState.APTL1)
                    : BuildingPositioning.GetPositionOfTheBestBuildingDeploy(p, hc, FightState.APTL2);

            if (hc.card.MaxHP >= Setting.MinHealthAsTank)
            {
                var tankInFront = PositionHelper.DeployTankInFront(p, line);

                if (tankInFront != null)
                    return tankInFront;
            }
            else
            {
                var behindTank = PositionHelper.DeployBehindTank(p, line);

                if (behindTank != null)
                    return behindTank;
            }

            VectorAI PT;

            if (PlayfieldAnalyse.lines[line - 1].OwnSide)
            {
                PT = p.getDeployPosition(deployDirectionAbsolute.ownPrincessTowerLine1);

                if (DeploymentDecision.SupportDeployment(p, line, true))
                    PT = p.getDeployPosition(PT, deployDirectionRelative.Down);
            }
            else
            {
                PT = p.getDeployPosition(deployDirectionAbsolute.enemyPrincessTowerLine1);

                if (DeploymentDecision.SupportDeployment(p, line, false))
                    PT = p.getDeployPosition(PT, deployDirectionRelative.Down);
            }

            return PT;
        }

        #endregion
    }
}