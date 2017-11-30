using System.Collections.Generic;
using System.Linq;

namespace Robi.Clash.DefaultSelectors.Apollo.Other
{
    internal class BoardObjHelper
    {
        public static int? HowManyCharactersAroundCharacter(Playfield p, BoardObj obj)
        {
            var boarderX = 1000;
            var boarderY = 1000;
            IEnumerable<BoardObj> playerCharacter = p.ownMinions;

            var characterAround = playerCharacter.Count(n => n.Position.X > obj.Position.X - boarderX
                                                             && n.Position.X < obj.Position.X + boarderX &&
                                                             n.Position.Y > obj.Position.Y - boarderY &&
                                                             n.Position.Y < obj.Position.Y + boarderY);
            return characterAround;
        }

        // NF = not flying
        public static int? HowManyNFCharactersAroundCharacter(Playfield p, VectorAI position)
        {
            var boarderX = 1000;
            var boarderY = 1000;
            IEnumerable<BoardObj> playerCharacter = p.ownMinions;

            var characterAround = playerCharacter.Count(n => n.Position.X > position.X - boarderX
                                                             && n.Position.X < position.X + boarderX &&
                                                             n.Position.Y > position.Y - boarderY &&
                                                             n.Position.Y < position.Y + boarderY &&
                                                             n.card.Transport == transportType.GROUND);

            return characterAround;
        }

        public static BoardObj EnemyCharacterWithTheMostEnemiesAround(Playfield p, out int count, transportType tP)
        {
            var boarderX = 1000;
            var boarderY = 1000;
            IEnumerable<BoardObj> enemies = p.enemyMinions;
            BoardObj enemy = null;
            count = 0;

            foreach (var item in enemies)
            {
                BoardObj[] enemiesAroundTemp;
                if (tP != transportType.NONE)
                    enemiesAroundTemp = enemies.Where(n => n.Position.X > item.Position.X - boarderX
                                                           && n.Position.X < item.Position.X + boarderX &&
                                                           n.Position.Y > item.Position.Y - boarderY &&
                                                           n.Position.Y < item.Position.Y + boarderY &&
                                                           n.Transport == tP).ToArray();
                else
                    enemiesAroundTemp = enemies.Where(n => n.Position.X > item.Position.X - boarderX
                                                           && n.Position.X < item.Position.X + boarderX &&
                                                           n.Position.Y > item.Position.Y - boarderY &&
                                                           n.Position.Y < item.Position.Y + boarderY).ToArray();

                if (!(enemiesAroundTemp?.Count() > count)) continue;

                count = enemiesAroundTemp.Count();
                enemy = item;
            }

            return enemy;
        }

        public static BoardObj GetNearestEnemy(Playfield p)
        {
            var nearestChar = p.enemyMinions;

            var orderedChar = nearestChar.OrderBy(n => n.Position.Y);

            return p.home ? orderedChar.FirstOrDefault() : orderedChar.LastOrDefault();
        }

        public static bool IsAnEnemyObjectInArea(Playfield p, VectorAI position, int areaSize, boardObjType type)
        {
            bool WhereClause(BoardObj n)
            {
                return n.Position.X >= position.X - areaSize && n.Position.X <= position.X + areaSize &&
                       n.Position.Y >= position.Y - areaSize && n.Position.Y <= position.Y + areaSize;
            }


            if (type == boardObjType.MOB)
                return p.enemyMinions.Where(WhereClause).Any();
            if (type == boardObjType.BUILDING)
                return p.enemyBuildings.Where(WhereClause).Any();
            if (type == boardObjType.AOE)
                return p.enemyAreaEffects.Where(WhereClause).Any();

            return false;
        }
    }
}