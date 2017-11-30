using System;
using System.Collections.Generic;
using System.Linq;
using Robi.Common;
using Serilog;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Classification
{
    internal class ClassificationHandling
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<ClassificationHandling>();

        public static IEnumerable<Handcard> GetOwnHandCards(Playfield p, boardObjType cardType,
            SpecificCardType sCardType, MoreSpecificMobCardType msCardType = MoreSpecificMobCardType.None)
        {
            var cardsOfType = p.ownHandCards.Where(n => n.card.type == cardType).ToArray();

            if (cardsOfType.Length == 0)
                return cardsOfType;

            switch (cardType)
            {
                case boardObjType.NONE:
                    return null;
                case boardObjType.BUILDING:
                    return BuildingClassification.GetCards(sCardType, cardsOfType);
                case boardObjType.MOB:
                    return MobClassification.GetCards(sCardType, msCardType, cardsOfType);
                case boardObjType.AOE:
                case boardObjType.PROJECTILE:
                    return SpellClassification.GetCards(sCardType, cardsOfType);
            }
            return null;
        }

        public static SpecificCardType GetSpecificCardType(Handcard hc)
        {
            switch (hc.card.type)
            {
                case boardObjType.BUILDING:
                    return BuildingClassification.GetType(hc);
                case boardObjType.MOB:
                    return MobClassification.GetType(hc);
                case boardObjType.AOE:
                case boardObjType.PROJECTILE:
                    return SpellClassification.GetType(hc);
                default:
                    return SpecificCardType.All;
            }
        }
    }
}