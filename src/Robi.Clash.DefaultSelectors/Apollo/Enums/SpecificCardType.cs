namespace Robi.Clash.DefaultSelectors.Apollo
{
    internal enum SpecificCardType
    {
        All,

        // Mobs
        MobsTank,
        MobsDamageDealer,
        MobsBigGroup,
        MobsAOE,
        MobsFlying,
        MobsRanger,
        MobsBuildingAttacker,
        MobsFlyingAttack,

        // Buildings
        BuildingsDefense,
        BuildingsAttack,
        BuildingsSpawning,
        BuildingsMana,

        // Spells
        SpellsDamaging,
        SpellsNonDamaging,
        SpellsTroopSpawning,
        SpellsBuffs
    }
}