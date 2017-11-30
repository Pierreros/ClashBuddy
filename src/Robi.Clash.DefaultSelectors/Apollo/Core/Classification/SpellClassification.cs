using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Classification
{
    class SpellClassification
    {
        public static Func<Handcard, bool> IsSpellBuff = hc => hc.card.affectType == affectType.ONLY_OWN;
        public static Func<Handcard, bool> IsSpellsTroopSpawning = hc => hc.card.SpawnNumber > 0;
        public static Func<Handcard, bool> IsSpellsNonDamaging = hc => hc.card.DamageRadius == 0;
        public static Func<Handcard, bool> IsSpellsDamaging = hc => hc.card.DamageRadius > 0;

        public static SpecificCardType GetType(Handcard hc)
        {
            if (IsSpellBuff(hc)) return SpecificCardType.SpellsBuffs;
            if (IsSpellsDamaging(hc)) return SpecificCardType.SpellsDamaging;
            if (IsSpellsNonDamaging(hc)) return SpecificCardType.SpellsNonDamaging;
            if (IsSpellsTroopSpawning(hc)) return SpecificCardType.SpellsTroopSpawning;
            return SpecificCardType.All;
        }

        public static IEnumerable<Handcard> GetCards(SpecificCardType sCardType, IEnumerable<Handcard> cards)
        {
            Func<Handcard, bool> @delegate = n => true;

            switch (sCardType)
            {
                case SpecificCardType.SpellsDamaging:
                    @delegate = IsSpellsDamaging;
                    break;
                case SpecificCardType.SpellsNonDamaging:
                    @delegate = IsSpellsNonDamaging;
                    break;
                case SpecificCardType.SpellsTroopSpawning:
                    @delegate = IsSpellsTroopSpawning; // TODO: Check
                    break;
                case SpecificCardType.SpellsBuffs:
                    @delegate = IsSpellBuff; // TODO: Check
                    break;
                default:
                    @delegate = n => false;
                    break;
            }
            return cards.Where(@delegate).ToArray();
        }
    }
}
