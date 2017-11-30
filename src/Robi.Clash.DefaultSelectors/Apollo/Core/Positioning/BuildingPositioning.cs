namespace Robi.Clash.DefaultSelectors.Apollo.Core.Positioning
{
    internal class BuildingPositioning
    {
        public static VectorAI GetPositionOfTheBestBuildingDeploy(Playfield p, Handcard hc, FightState currentSituation)
        {
            // ToDo: Find the best position
            var betweenBridges = p.getDeployPosition(deployDirectionAbsolute.betweenBridges);

            //switch (currentSituation)
            //{
            //    case FightState.UAPTL1:
            //    case FightState.DPTL1:
            //        return p.getDeployPosition(p.ownPrincessTower1.Position, deployDirectionRelative.RightDown);
            //    case FightState.UAPTL2:
            //    case FightState.DPTL2:
            //        return p.getDeployPosition(p.ownPrincessTower2.Position, deployDirectionRelative.LeftDown);
            //    case FightState.UAKTL1:
            //    case FightState.UAKTL2:
            //        return p.getDeployPosition(p.ownKingsTower.Position, deployDirectionRelative.Down);
            //    case FightState.APTL1:
            //        return p.getDeployPosition(betweenBridges, deployDirectionRelative.Left, 1000);
            //    case FightState.APTL2:
            //        return p.getDeployPosition(betweenBridges, deployDirectionRelative.Right, 1000);
            //    case FightState.AKT:
            //        return p.getDeployPosition(p.enemyKingsTower, deployDirectionRelative.Down, 500);
            //}

            return p.getDeployPosition(betweenBridges, deployDirectionRelative.Down, 4000);
        }
    }
}