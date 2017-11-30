using System;
using Robi.Clash.DefaultSelectors.Apollo.Other;
using Robi.Common;
using Serilog;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Positioning
{
    internal class SpellPositioning
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<PositionHandling>();
        public static BoardObj SpellDestination { get; set; }

        public static VectorAI GetPositionOfTheBestDamagingSpellDeploy(Playfield p, dynamic enemy)
        {
            if (enemy.Typ == boardObjType.BUILDING)
                return enemy.Position;

            if (enemy.Position != null)
            {
                // ToDo: Use a mix of the HP and count of the Units
                // How fast are the enemy units, needed for a better correction
                if (BoardObjHelper.HowManyNFCharactersAroundCharacter(p, enemy.Position) >=
                    Setting.SpellCorrectionConditionCharCount)
                {
                    if (enemy.Position != null)
                        return p.getDeployPosition(enemy.Position, deployDirectionRelative.Down, 500);
                }
                else
                {
                    if (enemy.Position != null)
                        return enemy.Position;
                }
            }

            return null;
        }
    }
}