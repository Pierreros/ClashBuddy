namespace Robi.Clash.DefaultSelectors.Apollo
{
    public enum FightState
    {
        // Defense
        DPTL1, // PrincessTower Line 1
        DPTL2, // PrincessTower Line 2
        DKT, // KingTower

        // UnderAttack
        UAPTL1, // PrincessTower Line 1
        UAPTL2, // PrincessTower Line 2
        UAKTL1, // KingTower Line 1
        UAKTL2, // KingTower Line 2

        // Attack
        APTL1, // PrincessTower Line 1
        APTL2, // PrincessTower Line 2
        AKT, // KingTower

        // Others
        START,
        WAIT
    }
}