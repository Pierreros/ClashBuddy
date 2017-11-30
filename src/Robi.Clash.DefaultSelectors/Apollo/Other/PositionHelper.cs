using System.Linq;

namespace Robi.Clash.DefaultSelectors.Apollo.Other
{
    internal class PositionHelper
    {
        public static VectorAI DeployBehindTank(Playfield p, int line)
        {
            var tankChar = p.ownMinions.Where(n => n.Line == line && n.HP >= Setting.MinHealthAsTank).OrderBy(n => n.HP)
                .FirstOrDefault();

            return tankChar != null ? p.getDeployPosition(tankChar, deployDirectionRelative.Down) : null;
        }

        public static VectorAI DeployTankInFront(Playfield p, int line)
        {
            var ownChar = p.ownMinions.Where(n => n.Line == line && n.MaxHP < Setting.MinHealthAsTank)
                .OrderBy(n => n.Position.Y).ToArray();
            var lc = ownChar.LastOrDefault();
            var fc = ownChar.FirstOrDefault();

            if (p.home)
                return lc != null ? p.getDeployPosition(lc, deployDirectionRelative.Up) : null;
            return fc != null ? p.getDeployPosition(fc, deployDirectionRelative.Up) : null;
        }
    }
}