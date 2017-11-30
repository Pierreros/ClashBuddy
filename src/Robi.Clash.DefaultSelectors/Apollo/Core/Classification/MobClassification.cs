using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Classification
{
    class MobClassification
    {
        //public static Func<Handcard, bool> IsMobsNoTank = (Handcard hc) => (hc.card.TargetType != targetType.BUILDINGS && hc.card.MaxHP < Setting.MinHealthAsTank);
        public static Func<Handcard, bool> IsMobsFlyingAttack = hc => hc.card.TargetType == targetType.ALL;
        public static Func<Handcard, bool> IsMobsAOE = hc => hc.card.aoeGround;
        public static Func<Handcard, bool> IsMobsRanger = hc => hc.card.MaxRange >= 1000;
        public static Func<Handcard, bool> IsMobsBuildingAttacker = hc => hc.card.TargetType == targetType.BUILDINGS;
        public static Func<Handcard, bool> IsMobsDamageDealer = hc => hc.card.Atk * hc.card.SummonNumber > 100;
        public static Func<Handcard, bool> IsMobsBigGroup = hc => hc.card.SummonNumber >= 8;
        public static Func<Handcard, bool> IsMobsMinTank = hc => hc.card.MaxHP >= Setting.MinHealthAsTank / 3;
        public static Func<Handcard, bool> IsMobsTank = hc => hc.card.MaxHP >= Setting.MinHealthAsTank;

        public static Func<Handcard, bool> IsCycleCard = hc => hc.manacost <= 2;
        public static Func<Handcard, bool> IsPowerCard = hc => hc.manacost >= 5;

        // More Specific
        public static Func<Handcard, bool> IsShortDistance = hc => hc.card.MaxRange <= 4500;
        public static Func<Handcard, bool> IsLongDistance = hc => hc.card.MaxRange > 4500;
        public static Func<Handcard, bool> IsAOEAll = hc => hc.card.aoeAir;
        public static Func<Handcard, bool> IsAOEGround = hc => hc.card.aoeGround;
        public static Func<Handcard, bool> IsFlying = hc => hc.card.Transport == transportType.AIR;
        public static Func<Handcard, bool> IsNotFlying = hc => hc.card.Transport == transportType.GROUND;
        public static Func<Handcard, bool> IsNoBigGroup = hc => hc.card.SummonNumber < 8;

        // With BoardObj, no Handcard
        public static Func<BoardObj, bool> IsMobsTankCurrentHP = bo => bo.HP >= Setting.MinHealthAsTank;

        public static SpecificCardType GetType(Handcard hc)
        {
            if (IsMobsRanger(hc)) return SpecificCardType.MobsRanger;
            if (IsMobsAOE(hc)) return SpecificCardType.MobsAOE;
            if (IsMobsBuildingAttacker(hc)) return SpecificCardType.MobsBuildingAttacker;
            if (IsMobsDamageDealer(hc)) return SpecificCardType.MobsDamageDealer;
            if (IsMobsBigGroup(hc)) return SpecificCardType.MobsBigGroup;
            if (IsMobsFlyingAttack(hc)) return SpecificCardType.MobsFlyingAttack;
            if (IsMobsTank(hc)) return SpecificCardType.MobsTank;
            return SpecificCardType.All;
        }

        public static MoreSpecificMobCardType GetSpecificType(Handcard hc, SpecificCardType specificCardType)
        {
            switch (specificCardType)
            {
                case SpecificCardType.MobsRanger:
                    return IsShortDistance(hc)
                        ? MoreSpecificMobCardType.ShortDistance
                        : MoreSpecificMobCardType.LongDistance;
                case SpecificCardType.MobsTank:
                    return IsMobsBuildingAttacker(hc)
                        ? MoreSpecificMobCardType.BuildingAttacker
                        : IsFlying(hc)
                            ? MoreSpecificMobCardType.Flying
                            : MoreSpecificMobCardType.NotFlying;
                case SpecificCardType.MobsFlying:
                    return IsMobsDamageDealer(hc)
                        ? MoreSpecificMobCardType.DamageDealer
                        : MoreSpecificMobCardType.Flying;
                case SpecificCardType.MobsDamageDealer:
                    return IsMobsAOE(hc)
                        ? MoreSpecificMobCardType.AOEGround
                        : IsMobsFlyingAttack(hc)
                            ? MoreSpecificMobCardType.FlyingAttack
                            : IsFlying(hc)
                                ? MoreSpecificMobCardType.Flying
                                : MoreSpecificMobCardType.NotFlying;
                case SpecificCardType.MobsBigGroup:

                    break;
                case SpecificCardType.MobsAOE:
                    return IsAOEAll(hc) ? MoreSpecificMobCardType.AOEAll : MoreSpecificMobCardType.AOEGround;
                case SpecificCardType.MobsBuildingAttacker:

                    break;
                case SpecificCardType.MobsFlyingAttack:

                    break;
            }
            return MoreSpecificMobCardType.None;
        }

        public static IEnumerable<Handcard> GetCards(SpecificCardType sCardType, MoreSpecificMobCardType msCardType, IEnumerable<Handcard> cards)
        {
            Func<Handcard, bool> @delegate = n => true;
            Func<Handcard, bool> msDelegate = n => true;

            switch (sCardType)
            {
                case SpecificCardType.All:
                    break;

                // Mobs
                case SpecificCardType.MobsTank:
                    @delegate = IsMobsTank;
                    break;
                case SpecificCardType.MobsRanger:
                    @delegate = IsMobsRanger;
                    break;
                case SpecificCardType.MobsBigGroup:
                    @delegate = IsMobsBigGroup;
                    break;
                case SpecificCardType.MobsDamageDealer:
                    @delegate = IsMobsDamageDealer;
                    break;
                case SpecificCardType.MobsBuildingAttacker:
                    @delegate = IsMobsBuildingAttacker;
                    break;
                case SpecificCardType.MobsFlyingAttack:
                    @delegate = IsMobsFlyingAttack;
                    break;
            }

            switch (msCardType)
            {
                case MoreSpecificMobCardType.None:
                    break;
                case MoreSpecificMobCardType.ShortDistance:
                    msDelegate = IsShortDistance;
                    break;
                case MoreSpecificMobCardType.LongDistance:
                    msDelegate = IsLongDistance;
                    break;
                case MoreSpecificMobCardType.BuildingAttacker:
                    msDelegate = IsMobsBuildingAttacker;
                    break;
                case MoreSpecificMobCardType.AOEGround:
                    msDelegate = IsAOEGround;
                    break;
                case MoreSpecificMobCardType.AOEAll:
                    msDelegate = IsAOEAll;
                    break;
                case MoreSpecificMobCardType.FlyingAttack:
                    msDelegate = IsMobsFlyingAttack;
                    break;
                case MoreSpecificMobCardType.Flying:
                    msDelegate = IsFlying;
                    break;
                case MoreSpecificMobCardType.NotFlying:
                    msDelegate = IsNotFlying;
                    break;
                case MoreSpecificMobCardType.DamageDealer:
                    msDelegate = IsMobsDamageDealer;
                    break;
                case MoreSpecificMobCardType.NoBigGroup:
                    msDelegate = IsNoBigGroup;
                    break;
            }

            if (msDelegate == null)
                return cards.Where(@delegate).ToArray();
            return cards.Where(@delegate).Where(msDelegate).ToArray();
        }
    }
}
