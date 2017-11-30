using System;

namespace Robi.Clash.DefaultSelectors.Apollo
{
    internal class Helper
    {
        public static double LevelMultiplicator(int value, int level, int type)
        {
            // 1.1 = mobs
            // 1.07 = KT
            // ToDo: Calculate the value without round errors
            return type == 1 ? value * Math.Pow(1.1d, level) : value * Math.Pow(1.07d, level);
        }

        public static double Quotient(int a, double b)
        {
            if (a == 0 || b == 0)
                return 0;

            return a / b;
        }
    }
}