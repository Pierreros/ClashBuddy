using System.Collections.Generic;
using System.Linq;
using Robi.Common;
using Serilog;

namespace Robi.Clash.DefaultSelectors.Apollo
{
    internal class PlayfieldAnalyse
    {
        // ToDo: Use ATK per second btw: Whats with tower damage?
        // ToDo: Involve KT in analyses
        public static readonly ILogger Logger = LogProvider.CreateLogger<PlayfieldAnalyse>();

        public static Line[] lines;

        public static void AnalyseLines(Playfield p)
        {
            lines = new Line[2];
            lines[0] = new Line();
            lines[1] = new Line();

            Tower(p);
            Minions(p);
            Comparision();
            Level(p);
        }

        private static void Tower(Playfield p)
        {
            #region TowerAnalyses

            //double oPtHpL1 = Helper.Quotient(p.ownPrincessTower1.HP, Behaviors.Apollo.playfield.ownKingsTower.MaxHP) * 100;
            //double oPtHpL2 = Helper.Quotient(p.ownPrincessTower2.HP, Behaviors.Apollo.playfield.ownKingsTower.MaxHP) * 100;
            //double ePtHpL1 = Helper.Quotient(p.enemyPrincessTower1.HP, Behaviors.Apollo.playfield.ownKingsTower.MaxHP) * 100;
            //double ePtHpL2 = Helper.Quotient(p.enemyPrincessTower2.HP, Behaviors.Apollo.playfield.ownKingsTower.MaxHP) * 100;

            //if (oPtHpL1 == 0) lines[0].OwnPtHp = Apollo.Level.ZERO;
            //else if (oPtHpL1 <= 30) lines[0].OwnPtHp = Apollo.Level.LOW;
            //else if (oPtHpL1 <= 70) lines[0].OwnPtHp = Apollo.Level.MEDIUM;
            //else lines[0].OwnPtHp = Apollo.Level.HIGH;

            //if (oPtHpL2 == 0) lines[1].OwnPtHp = Apollo.Level.ZERO;
            //else if (oPtHpL2 <= 30) lines[1].OwnPtHp = Apollo.Level.LOW;
            //else if (oPtHpL2 <= 70) lines[1].OwnPtHp = Apollo.Level.MEDIUM;
            //else lines[1].OwnPtHp = Apollo.Level.HIGH;

            #endregion
        }

        private static void Minions(Playfield p)
        {
            #region minion sums (atk and health; Line 1 and 2)

            var enemyMinionsL1 = p.enemyMinions.Where(n => n.Line == 1).ToArray();
            var enemyMinionsL2 = p.enemyMinions.Where(n => n.Line == 2).ToArray();

            lines[0].EnemyMinionAtk = enemyMinionsL1.Sum(n => n.Atk);
            lines[0].EnemyMinionHP = enemyMinionsL1.Sum(n => n.HP);
            lines[1].EnemyMinionAtk = enemyMinionsL2.Sum(n => n.Atk);
            lines[1].EnemyMinionHP = enemyMinionsL2.Sum(n => n.HP);

            IEnumerable<BoardObj> ownMinionsL1 = p.ownMinions.Where(n => n.Line == 1).ToArray();
            IEnumerable<BoardObj> ownMinionsL2 = p.ownMinions.Where(n => n.Line == 2).ToArray();

            var ownSideL1HP = ownMinionsL1.Where(n => n.onMySide(p.home)).Sum(n => n.HP);
            var enemySideL1HP = ownMinionsL1.Where(n => !n.onMySide(p.home)).Sum(n => n.HP);

            var ownSideL2HP = ownMinionsL2.Where(n => n.onMySide(p.home)).Sum(n => n.HP);
            var enemySideL2HP = ownMinionsL2.Where(n => !n.onMySide(p.home)).Sum(n => n.HP);


            lines[0].OwnMinionHP = ownSideL1HP + enemySideL1HP;
            lines[0].OwnMinionAtk = ownMinionsL1.Sum(n => n.Atk);
            lines[1].OwnMinionHP = ownSideL2HP + enemySideL2HP;
            lines[1].OwnMinionAtk = ownMinionsL2.Sum(n => n.Atk);

            lines[0].OwnSide = ownSideL1HP > enemySideL1HP;
            lines[1].OwnSide = ownSideL2HP > enemySideL2HP;

            #endregion

            lines[0].OwnMinionCount = ownMinionsL1.Count();
            lines[1].OwnMinionCount = ownMinionsL2.Count();
            lines[0].EnemyMinionCount = enemyMinionsL1.Count();
            lines[1].EnemyMinionCount = enemyMinionsL2.Count();
        }

        private static void Comparision()
        {
            lines[0].ComparisionHP = lines[0].OwnMinionHP - lines[0].EnemyMinionHP;
            lines[0].ComparisionAtk = lines[0].OwnMinionAtk - lines[0].EnemyMinionAtk;

            lines[1].ComparisionHP = lines[1].OwnMinionHP - lines[1].EnemyMinionHP;
            lines[1].ComparisionAtk = lines[1].OwnMinionAtk - lines[1].EnemyMinionAtk;
        }

        private static void Level(Playfield p)
        {
            lines[0].Danger = GetDangerLevel(p, 0);
            lines[1].Danger = GetDangerLevel(p, 1);

            lines[0].Chance = GetChanceLevel(0, p);
            lines[1].Chance = GetChanceLevel(1, p);
        }

        private static Level GetDangerLevel(Playfield p, int line)
        {
            int dangerLevel, dangerLvlHP, dangerLvlAtk, dangerLvlBuilding, dangerLvlTower = 0;
            float sensitivity = Setting.DangerSensitivity;

            if (sensitivity == 0)
                sensitivity = 0.5f;

            dangerLvlHP = GetDangerLvLMinionHp(line, sensitivity, p);
            dangerLvlAtk = GetDangerLvLMinionAtk(line, sensitivity, p);
            dangerLvlBuilding = GetDangerLvlBuilding(p, line);


            #region PrincessTower HP

            //switch (lines[line].OwnPtHp)
            //{
            //    case Apollo.Level.LOW:
            //        dangerLvlTower += 1;
            //        break;
            //    case Apollo.Level.MEDIUM:
            //        dangerLvlTower += 2;
            //        break;
            //    case Apollo.Level.HIGH:
            //        dangerLvlTower += 3;
            //        break;
            //    default:
            //        break;
            //}

            #endregion

            Logger.Debug("Danger-Analyses-Level");
            Logger.Debug("Atk       :" + dangerLvlAtk);
            Logger.Debug("HP        :" + dangerLvlHP);
            Logger.Debug("Tower-HP  :" + dangerLvlTower);
            Logger.Debug("Building  :" + dangerLvlBuilding);
            Logger.Debug("Danger-Analyses-End");

            dangerLevel = dangerLvlAtk + dangerLvlHP + dangerLvlTower + dangerLvlBuilding;
            // Maybe round up
            if (dangerLvlBuilding == 0)
                return (Level) (dangerLevel / 2);
            if (dangerLvlHP > 0)
                return (Level) (dangerLevel / 3);
            return (Level) dangerLvlBuilding;
        }

        private static int GetDangerLvLMinionHp(int line, float sensitivity, Playfield p)
        {
            var enemyMinionHp = lines[line].EnemyMinionHP;

            if (enemyMinionHp != 0)
            {
                if (enemyMinionHp > p.ownKingsTower.MaxHP / (0.5 * sensitivity))
                    return 5;       
                if (enemyMinionHp > p.ownKingsTower.MaxHP / sensitivity)
                    return 4;       
                if (enemyMinionHp > p.ownKingsTower.MaxHP / (2 * sensitivity))
                    return 3;       
                if (enemyMinionHp > p.ownKingsTower.MaxHP / (4 * sensitivity))
                    return 2;       
                if (enemyMinionHp > p.ownKingsTower.MaxHP / (6 * sensitivity))
                    return 1;

                // ToDo: Use this, but Atk is zero at the moment
                //if (enemyMinionHP < -(lines[line].OwnPtAtk * sensitivity * 2))
                //    dangerLvlHP += 3;
                //else if (enemyMinionHP < -(lines[line].OwnPtAtk * sensitivity * 1.5))
                //    dangerLvlHP += 2;
                //else if (enemyMinionHP < -(lines[line].OwnPtAtk * sensitivity))
                //    dangerLvlHP += 1;
            }

            return 0;
        }

        private static int GetDangerLvLMinionAtk(int line, float sensitivity, Playfield p)
        {
            var enemyMinionAtk = lines[line].EnemyMinionAtk;

            if (enemyMinionAtk != 0)
            {
                if (enemyMinionAtk > p.ownKingsTower.MaxHP / sensitivity)
                    return 5;        
                if (enemyMinionAtk > p.ownKingsTower.MaxHP / (4 * sensitivity))
                    return 4;        
                if (enemyMinionAtk > p.ownKingsTower.MaxHP / (5 * sensitivity))
                    return 3;        
                if (enemyMinionAtk > p.ownKingsTower.MaxHP / (10 * sensitivity))
                    return 2;        
                if (enemyMinionAtk > p.ownKingsTower.MaxHP / (15 * sensitivity))
                    return 1;
            }

            return 0;
        }

        private static int GetDangerLvlBuilding(Playfield p, int line)
        {
            var enemyBuildings = p.enemyBuildings;

            if (enemyBuildings?.Count() > 0)
            {
                var bKt = enemyBuildings.FirstOrDefault(n =>
                    n.Line == line && n.IsPositionInArea(p, p.ownKingsTower.Position));

                BoardObj bPt;

                if (line == 0)
                    bPt = enemyBuildings.FirstOrDefault(n => n.IsPositionInArea(p, p.ownPrincessTower1.Position));
                else
                    bPt = enemyBuildings.FirstOrDefault(n => n.IsPositionInArea(p, p.ownPrincessTower2.Position));

                if (bKt != null || bPt != null)
                    return 3;
            }

            return 0;
        }

        private static Level GetChanceLevel(int line, Playfield p)
        {
            int chanceLevel = 0, chanceLvlHP = 0, chanceLvlAtk = 0, chanceLvlTower = 0;
            float sensitivity = Setting.ChanceSensitivity;

            if (sensitivity == 0)
                sensitivity = 0.5f;

            var ownMinionHP = lines[line].OwnMinionHP;
            var ownMinionAtk = lines[line].OwnMinionAtk;

            #region Minion HP

            if (ownMinionHP != 0)
                if (ownMinionHP > p.ownKingsTower.MaxHP / (0.5 * sensitivity))
                    chanceLvlHP += 5;
                else if (ownMinionHP > p.ownKingsTower.MaxHP / sensitivity)
                    chanceLvlHP += 4;
                else if (ownMinionHP > p.ownKingsTower.MaxHP / (2 * sensitivity))
                    chanceLvlHP += 3;
                else if (ownMinionHP > p.ownKingsTower.MaxHP / (4 * sensitivity))
                    chanceLvlHP += 2;
                else if (ownMinionHP > p.ownKingsTower.MaxHP / (6 * sensitivity))
                    chanceLvlHP += 1;

            #endregion

            #region Minion Atk

            if (ownMinionAtk != 0)
                if (ownMinionAtk > p.ownKingsTower.MaxHP / sensitivity)
                    chanceLvlAtk += 5;
                else if (ownMinionAtk > p.ownKingsTower.MaxHP / (3 * sensitivity))
                    chanceLvlAtk += 4;
                else if (ownMinionAtk > p.ownKingsTower.MaxHP / (5 * sensitivity))
                    chanceLvlAtk += 3;
                else if (ownMinionAtk > p.ownKingsTower.MaxHP / (10 * sensitivity))
                    chanceLvlAtk += 2;
                else if (ownMinionAtk > p.ownKingsTower.MaxHP / (15 * sensitivity))
                    chanceLvlAtk += 1;

            #endregion

            #region PrincessTower HP

            //switch (lines[line].EnemyPtHp)
            //{
            //    case Apollo.Level.LOW:
            //        chanceLvlTower += 1;
            //        break;
            //    case Apollo.Level.MEDIUM:
            //        chanceLvlTower += 2;
            //        break;
            //    case Apollo.Level.HIGH:
            //        chanceLvlTower += 3;
            //        break;
            //    default:
            //        break;
            //}

            #endregion

            Logger.Debug("Chance-Analyses-Level");
            Logger.Debug("Atk       :" + chanceLvlAtk);
            Logger.Debug("HP        :" + chanceLvlHP);
            Logger.Debug("Tower-HP  :" + chanceLvlTower);
            Logger.Debug("Chance-Analyses-End");

            chanceLevel = chanceLvlAtk + chanceLvlHP + chanceLvlTower;

            return (Level) (chanceLevel / 2);
        }
    }
}