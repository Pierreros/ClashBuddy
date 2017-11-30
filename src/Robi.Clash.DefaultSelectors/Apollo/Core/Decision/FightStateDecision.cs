using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Robi.Clash.DefaultSelectors.Apollo.Other;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Decisions
{
    static class FightStateDecision
    {
        public static Apollo.FightState DefenseDecision(Playfield p)
        {
            // There a no dangerous or own important minions on the playfield
            // This is why we are deciding independent of the minions on the field

            if (p.ownTowers.Count < 3)
                return Apollo.FightState.DKT;

            var princessTower =
                p.enemyPrincessTowers.OrderBy(n => n.HP)
                    .FirstOrDefault(); // Because they are going to attack this tower

            if (princessTower != null && princessTower.Line == 2)
                return Apollo.FightState.DPTL2;
            return Apollo.FightState.DPTL1;
        }

        public static Apollo.FightState AttackDecision(Playfield p)
        {
            if (p.enemyTowers.Count < 3)
            {
                if (p.enemyPrincessTower1.HP > 0 && p.enemyPrincessTower1.HP < p.enemyKingsTower.HP / 2)
                    return Apollo.FightState.APTL1;
                if (p.enemyPrincessTower2.HP > 0 && p.enemyPrincessTower2.HP < p.enemyKingsTower.HP / 2)
                    return Apollo.FightState.APTL2;

                return Apollo.FightState.AKT;
            }


            var princessTower = p.enemyPrincessTowers.OrderBy(n => n.HP).FirstOrDefault();

            if (princessTower != null && princessTower.Line == 2)
                return Apollo.FightState.APTL2;
            return Apollo.FightState.APTL1;
        }

        public static Apollo.FightState DangerousSituationDecision(Playfield p, int line)
        {
            if (p.ownTowers.Count > 2)
                return line == 2 ? Apollo.FightState.UAPTL2 : Apollo.FightState.UAPTL1;
            return line == 2 ? Apollo.FightState.UAKTL2 : Apollo.FightState.UAKTL1;
        }

        public static Apollo.FightState GoodAttackChanceDecision(Playfield p, int line)
        {
            if (p.enemyTowers.Count < 3)
            {
                if (p.enemyPrincessTower1.HP > 0 && p.enemyPrincessTower1.HP < p.enemyKingsTower.HP / 2)
                    return Apollo.FightState.APTL1;

                if (p.enemyPrincessTower2.HP > 0 && p.enemyPrincessTower2.HP < p.enemyKingsTower.HP / 2)
                    return Apollo.FightState.APTL2;

                return Apollo.FightState.AKT;
            }

            if (line == 2)
                return Apollo.FightState.APTL2;
            return Apollo.FightState.APTL1;
        }

        public static Apollo.FightState GameBeginningDecision(Playfield p, out bool gameBeginning)
        {
            var startFirstAttack = true;
            gameBeginning = true;


            startFirstAttack = p.ownMana < Setting.ManaTillFirstAttack;

            if (startFirstAttack)
            {
                if (!p.noEnemiesOnMySide())
                    gameBeginning = false;

                return Apollo.FightState.START;
            }
            gameBeginning = false;
            var obj = BoardObjHelper.GetNearestEnemy(p);

            if (obj?.Line == 2)
                return Apollo.FightState.DPTL2;
            return Apollo.FightState.DPTL1;
        }


        #region Not used in balanced FightMode
        public static FightState EnemyHasCharsOnTheFieldDecision(Playfield p)
        {
            if (p.ownTowers.Count > 2)
            {
                //BoardObj obj = GetNearestEnemy(p);
                // ToDo: Get most dangeroust group
                var mostDangeroustGroup = p.getGroup(false, 200, boPriority.byTotalBuildingsDPS, 3000);

                if (mostDangeroustGroup == null)
                    return FightState.DKT;

                var line = mostDangeroustGroup.Position.X > 8700 ? 2 : 1;
                return line == 2 ? FightState.DPTL2 : FightState.DPTL1;
            }
            return FightState.DKT;
        }

        public static FightState EnemyIsOnOurSideDecision(Playfield p)
        {
            if (p.ownTowers.Count > 2)
            {
                var obj = BoardObjHelper.GetNearestEnemy(p);

                if (obj != null && obj.Line == 2)
                    return FightState.UAPTL2;
                return FightState.UAPTL1;
            }
            else
            {
                var obj = BoardObjHelper.GetNearestEnemy(p);

                if (obj != null && obj.Line == 2)
                    return FightState.UAKTL2;
                return FightState.UAKTL1;
            }
        }
        #endregion
    }
}
