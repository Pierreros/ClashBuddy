using Robi.Clash.DefaultSelectors.Apollo.Core.Classification;
using Robi.Common;
using Serilog;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Positioning
{
    internal class MobPositioning
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<PositionHandling>();

        public static VectorAI PrincessTowerCharacterDeploymentCorrection(VectorAI position, Playfield p, Handcard hc)
        {
            if (hc?.card == null || position == null)
                return null;

            //Logger.Debug("PT Characer Position Correction: Name und Typ {0} " + cardToDeploy.Name, (cardToDeploy as CardCharacter).Type);
            if (hc.card.type == boardObjType.MOB)
            {
                if (hc.card.MaxHP >= Setting.MinHealthAsTank)
                    return p.getDeployPosition(position, deployDirectionRelative.Up, 100);

                // ToDo: Maybe if there is already a tank, place it behind him

                //if(Classification.GetMoreSpecificCardType(hc, SpecificCardType.MobsAOE) == MoreSpecificMobCardType.AOEGround)
                //{
                //    return p.getDeployPosition(position, deployDirectionRelative.Up, 100);
                //}

                if (ClassificationHandling.GetSpecificCardType(hc) == SpecificCardType.MobsRanger)
                    return p.getDeployPosition(position, deployDirectionRelative.Down, 2000);

                return p.getDeployPosition(position, deployDirectionRelative.Up, 100);
            }
            Logger.Debug("Tower Correction: No Correction!!!");

            return position;
        }
    }
}