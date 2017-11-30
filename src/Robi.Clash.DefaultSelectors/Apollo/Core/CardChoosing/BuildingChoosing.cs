using System.Linq;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.CardChoosing
{
    internal class BuildingChoosing
    {
        private static Handcard Building(Playfield p)
        {
            var buildingCard = p.ownHandCards.FirstOrDefault(n => n.card.type == boardObjType.BUILDING);
            if (buildingCard != null)
                return new Handcard(buildingCard.name, buildingCard.lvl);

            return null;
        }
    }
}