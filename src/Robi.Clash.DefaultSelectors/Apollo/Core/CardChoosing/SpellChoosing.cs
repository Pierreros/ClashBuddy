using System.Collections.Generic;
using System.Linq;
using Robi.Clash.DefaultSelectors.Apollo.Core.Classification;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.CardChoosing
{
    ///-----------------------------------------------------------------
    /// Spell card choosing + basic position
    ///-----------------------------------------------------------------
    internal class SpellChoosing
    {
        public static Handcard DamagingSpellDecision(Playfield p, out dynamic dsDestination)
        {
            dsDestination = null;
            Handcard spellCard = null;

            var damagingSpells =
                ClassificationHandling.GetOwnHandCards(p, boardObjType.PROJECTILE, SpecificCardType.SpellsDamaging);

            var fds = damagingSpells?.FirstOrDefault();
            if (fds == null)
                return null;

            spellCard = TowerDestroyer(p, damagingSpells, out dsDestination);
            return spellCard ?? GroupDestroyer(p, damagingSpells, out dsDestination);
        }

        public static Handcard GroupDestroyer(Playfield p, IEnumerable<Handcard> damagingSpells,
            out dynamic dsDestination)
        {
            dsDestination = null;
            var radiusOrderedDS = damagingSpells.OrderBy(n => n.card.DamageRadius).LastOrDefault();
            var Group = p.getGroup(false, 200, boPriority.byTotalNumber, radiusOrderedDS.card.DamageRadius);

            if (Group == null)
                return null;
            var grpCount = Group.lowHPbo.Count() + Group.avgHPbo.Count() + Group.hiHPbo.Count();
            var hpSum = Group.lowHPboHP + Group.hiHPboHP + Group.avgHPboHP;

            // Big damage radius
            var ds1 = damagingSpells.FirstOrDefault(n => n.card.DamageRadius > 3 && grpCount > 4);
            if (ds1 != null)
            {
                dsDestination = new { Typ = boardObjType.MOB, Position = Group.Position };
                return ds1;
            }

            // Small damage radius
            var ds2 = damagingSpells.FirstOrDefault(n =>
                n.card.DamageRadius <= 3 && grpCount > 1 && hpSum >= n.card.Atk * 2);
            if (ds2 != null)
            {
                dsDestination = new { Typ = boardObjType.MOB, Position = Group.Position };
                return ds2;
            }

            return null;
        }

        public static Handcard TowerDestroyer(Playfield p, IEnumerable<Handcard> damagingSpells,
            out dynamic choosedPosition)
        {
            choosedPosition = null;
            var ds5 = damagingSpells.FirstOrDefault(n => n.card.towerDamage >= p.enemyKingsTower.HP);
            if (ds5 != null)
            {
                choosedPosition = new { Typ = boardObjType.BUILDING, Position = p.enemyKingsTower.Position};
                return ds5;
            }

            if (p.BattleTime.TotalSeconds > 160)
            {
                var ds3 = damagingSpells.FirstOrDefault(n => n.card.towerDamage >= p.enemyPrincessTower1.HP);
                var ds4 = damagingSpells.FirstOrDefault(n => n.card.towerDamage >= p.enemyPrincessTower2.HP);

                if (ds3 != null && p.enemyPrincessTower1.HP > 0)
                {
                    choosedPosition = new { Typ = boardObjType.BUILDING, Position = p.enemyPrincessTower1.Position};
                    return ds3;
                }

                if (ds4 != null && p.enemyPrincessTower2.HP > 0)
                {
                    choosedPosition = new { Typ = boardObjType.BUILDING, Position = p.enemyPrincessTower2.Position};
                    return ds4;
                }
            }
            return null;
        }
    }
}