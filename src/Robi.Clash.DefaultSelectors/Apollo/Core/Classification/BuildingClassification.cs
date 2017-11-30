using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Classification
{
    class BuildingClassification
    {
        public static Func<Handcard, bool> IsBuildingsDefense = hc => hc.card.Atk > 0;
        public static Func<Handcard, bool> IsBuildingsAttack = hc => hc.card.Atk > 0;
        public static Func<Handcard, bool> IsBuildingsSpawning = hc => hc.card.SpawnNumber > 0;
        public static Func<Handcard, bool> IsBuildingsMana = hc => false; // ToDo: Implement mana production

        public static SpecificCardType GetType(Handcard hc)
        {
            if (IsBuildingsDefense(hc)) return SpecificCardType.BuildingsDefense;
            if (IsBuildingsAttack(hc)) return SpecificCardType.BuildingsAttack;
            if (IsBuildingsMana(hc)) return SpecificCardType.BuildingsMana;
            if (IsBuildingsSpawning(hc)) return SpecificCardType.BuildingsSpawning;
            return SpecificCardType.All;
        }

        public static IEnumerable<Handcard> GetCards(SpecificCardType sCardType, IEnumerable<Handcard> cards)
        {
            Func<Handcard, bool> @delegate = n => true;

            switch (sCardType)
            {
                case SpecificCardType.BuildingsDefense:
                    @delegate = IsBuildingsDefense; // TODO: Define
                    break;
                case SpecificCardType.BuildingsAttack:
                    @delegate = IsBuildingsAttack; // TODO: Define
                    break;
                case SpecificCardType.BuildingsSpawning:
                    @delegate = IsBuildingsSpawning;
                    break;
                case SpecificCardType.BuildingsMana:
                    @delegate = n => false;
                    break; // TODO: ManaProduction
            }
            return cards.Where(@delegate).ToArray();
        }

    }
}
