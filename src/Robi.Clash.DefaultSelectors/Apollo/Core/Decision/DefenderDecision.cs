using System.Linq;
using Robi.Clash.DefaultSelectors.Apollo.Other;
using Robi.Clash.DefaultSelectors.Apollo.Core.Classification;
using Robi.Common;
using Serilog;

namespace Robi.Clash.DefaultSelectors.Apollo.Core
{
    internal class DefenderDecision
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<DefenderDecision>();

        public static BoardObj GetBestDefender(Playfield p)
        {
            // TODO: Find better condition
            var enemy = BoardObjHelper.EnemyCharacterWithTheMostEnemiesAround(p, out var count, transportType.NONE);

            if (enemy == null)
                return p.ownKingsTower;

            switch (enemy.Line)
            {
                case 2:
                    return p.ownPrincessTower2;
                case 1:
                    return p.ownPrincessTower1;
                default:
                    return p.ownKingsTower;
            }
        }
    }
}