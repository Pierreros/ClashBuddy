﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Robi.Clash.DefaultSelectors.Apollo
{
    public enum FightState
    {
        // Defense
        DPTL1,      // PrincessTower Line 1
        DPTL2,      // PrincessTower Line 2
        DKT,        // KingTower
        // UnderAttack
        UAPTL1,     // PrincessTower Line 1
        UAPTL2,     // PrincessTower Line 2
        UAKTL1,     // KingTower Line 1
        UAKTL2,     // KingTower Line 2

        // Attack
        APTL1,      // PrincessTower Line 1
        APTL2,      // PrincessTower Line 2
        AKT,        // KingTower
        // Others
        START,
        WAIT
    };

    enum CardTypeOld
    {
        Defense,
        All,
        Troop,
        Buildings,
        NONE
    };

    enum DeployDecision
    {
        DamagingSpell,
        AOEAttack,
        AttacksFlying,
        Buildings,
        CycleSpell,
        PowerSpell
    };

    enum FightStyle
    {
        Defensive,
        Balanced,
        Rusher
    };

    enum SpecificCardType
    {
        All,


        // Mobs
        MobsTank,
        MobsNoTank,
        MobsDamageDealer,
        MobsDamageDealerAOE,
        MobsBuildingAttacker,
        MobsRanger,
        MobsAOEGround,
        MobsAOEAll,
        MobsFlyingAttack,
        MobsUnderAttack,

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
    };

    public enum Level
    {
        ZERO,
        LOW,
        MEDIUM,
        HIGH,
        HIGHER,
        HIGHEST
    };
}
