namespace Robi.Clash.DefaultSelectors.Apollo
{
    internal class Line
    {
        public Level Danger { get; set; }

        public Level Chance { get; set; }

        public bool OwnSide { get; set; }

        public int EnemyMinionHP { get; set; }

        public int EnemyMinionCount { get; set; }

        public int EnemyMinionAtk { get; set; }

        public int OwnMinionHP { get; set; }

        public int OwnMinionCount { get; set; }

        public int OwnMinionAtk { get; set; }

        public Level OwnPtHp { get; set; }

        public Level EnemyPtHp { get; set; }

        public int ComparisionHP { get; set; }

        public int ComparisionAtk { get; set; }
    }
}