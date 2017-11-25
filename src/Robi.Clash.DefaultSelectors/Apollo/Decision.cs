﻿using Robi.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Robi.Clash.DefaultSelectors.Apollo
{
    class Decision
    {
        public static readonly ILogger Logger = LogProvider.CreateLogger<Decision>();

        public static int CanWaitDecision(Playfield p, FightState currentSituation)
        {
            if (p.noEnemiesOnMySide())
            {
                switch (currentSituation)
                {
                    //case FightState.DLPT:
                    //case FightState.DRPT:
                    //    break;
                    //case FightState.DKT:
                    //    break;
                    case FightState.APTL1:
                        {
                            if (p.BattleTime.TotalSeconds < 10)
                                return 0;
                            else if (p.enemyPrincessTower1.HP < 300 && p.enemyPrincessTower1.HP > 0)
                                return 2;
                            else if (PlayfieldAnalyse.lines[0].Chance == Level.HIGH)
                                return 2;
                            break;
                        }
                    case FightState.APTL2:
                        {
                            if (p.BattleTime.TotalSeconds < 10)
                                return 0;
                            else if (p.enemyPrincessTower2.HP < 300 && p.enemyPrincessTower2.HP > 0)
                                return 2;
                            else if (PlayfieldAnalyse.lines[1].Chance == Level.HIGH)
                                return 2;
                            break;
                        }
                    case FightState.AKT:
                        {
                            if (p.BattleTime.TotalSeconds < 10)
                                return 0;
                            else if (p.enemyKingsTower.HP < 300 && p.enemyKingsTower.HP > 0)
                                return 2;
                            else if (PlayfieldAnalyse.lines[0].Chance == Level.HIGH || PlayfieldAnalyse.lines[1].Chance == Level.HIGH)
                                return 2;
                            break;
                        }
                }
            }
            else
            {
                if (p.BattleTime.TotalSeconds < 15)
                    return 2;
                if (p.ownKingsTower.HP < 500)
                    return 1;
                if (Helper.IsAnEnemyObjectInArea(p, p.ownKingsTower.Position, 4000, boardObjType.MOB))
                    return 2; // ToDo: Find better condition, if the handcard can´t attack the minions, we should wait
                if (PlayfieldAnalyse.lines[0].Danger == Level.HIGH || PlayfieldAnalyse.lines[1].Danger == Level.HIGH) // ToDo: Maybe check just the line
                    return 1;
            }

            return 10;
        }

        public static FightState DefenseDecision(Playfield p)
        {
            // There a no dangerous or own important minions on the playfield
            // This is why we are deciding independent of the minions on the field

            if (p.ownTowers.Count < 3)
                return FightState.DKT;

            BoardObj princessTower = p.enemyPrincessTowers.OrderBy(n => n.HP).FirstOrDefault(); // Because they are going to attack this tower

            if (princessTower != null && princessTower.Line == 2)
                return FightState.DPTL2;
            else
                return FightState.DPTL1;
        }

        public static FightState AttackDecision(Playfield p)
        {
            if (p.enemyTowers.Count < 3)
            {
                if (p.enemyPrincessTower1.HP > 0 && p.enemyPrincessTower1.HP < p.enemyKingsTower.HP / 2)
                    return FightState.APTL1;
                else if (p.enemyPrincessTower2.HP > 0 && p.enemyPrincessTower2.HP < p.enemyKingsTower.HP / 2)
                    return FightState.APTL2;

                return FightState.AKT;
            }


            BoardObj princessTower = p.enemyPrincessTowers.OrderBy(n => n.HP).FirstOrDefault();

            if (princessTower != null && princessTower.Line == 2)
                return FightState.APTL2;
            else
                return FightState.APTL1;
        }

        public static FightState DangerousSituationDecision(Playfield p, int line)
        {
            if (p.ownTowers.Count > 2)
            {
                return line == 2 ? FightState.UAPTL2 : FightState.UAPTL1;
            }
            else
            {
                return line == 2 ? FightState.UAKTL2 : FightState.UAKTL1;
            }
        }

        public static FightState GoodAttackChanceDecision(Playfield p, int line)
        {
            if (p.enemyTowers.Count < 3)
            {
                if (p.enemyPrincessTower1.HP > 0 && p.enemyPrincessTower1.HP < p.enemyKingsTower.HP / 2)
                    return FightState.APTL1;

                if (p.enemyPrincessTower2.HP > 0 && p.enemyPrincessTower2.HP < p.enemyKingsTower.HP / 2)
                    return FightState.APTL2;

                return FightState.AKT;
            }

            if (line == 2)
                return FightState.APTL2;
            else
                return FightState.APTL1;
        }

        public static FightState GameBeginningDecision(Playfield p, out bool gameBeginning)
        {
            bool StartFirstAttack = true;
            gameBeginning = true;


            StartFirstAttack = (p.ownMana < Setting.ManaTillFirstAttack);

            if (StartFirstAttack)
            {
                if (!p.noEnemiesOnMySide())
                    gameBeginning = false;

                return FightState.START;
            }
            else
            {
                gameBeginning = false;
                BoardObj obj = Helper.GetNearestEnemy(p);

                if (obj?.Line == 2)
                    return FightState.DPTL2;
                else
                    return FightState.DPTL1;
            }
        }

        public static bool SupportDeployment(Playfield p, int line, bool ownSide)
        {
            // If own characters already attacking and you are deploying as support
            // The chars should be deployed behind the own chars

            var attackingChars = ownSide ? 
                p.ownMinions.Where(n => n.Line == line && n.onMySide(p.home)) : 
                p.ownMinions.Where(n => n.Line == line && !n.onMySide(p.home));

            // Maybe check also which card type: Tank deployed in front, Ranger behinde ...

            return attackingChars.Any();
        }


        public static int DangerOrBestAttackingLine(Playfield p) // Good chance for an attack?
        {
            Line[] lines = PlayfieldAnalyse.lines;
            int lvlBorder = 1;
            // comparison
            // ToDo: Use line danger and chance analyses

            //if (lines[0].ComparisionHP == 0 && lines[1].ComparisionHP == 0)
            //    return 0;

            if ((int)lines[0].Danger <= lvlBorder && 
                (int)lines[1].Danger <= lvlBorder && 
                (int)lines[0].Chance <= lvlBorder && 
                (int)lines[1].Chance <= lvlBorder)
                return 0;

            if (lines[0].Danger >= lines[0].Chance)
            {
                if(lines[1].Danger >= lines[1].Chance)
                {
                    if (lines[0].Danger == lines[1].Danger)
                    {
                        return lines[0].ComparisionHP < lines[1].ComparisionHP ? -1 : -2;
                    }

                    return lines[0].Danger >= lines[1].Danger ? -1 : -2;
                }
                else
                {
                    return lines[0].Danger >= lines[1].Chance ? -1 : 2;
                }
            }
            else
            {
                if (lines[1].Danger >= lines[1].Chance)
                {
                    return lines[0].Chance > lines[1].Danger ? 1 : -2;
                }
                else
                {
                    return lines[0].Chance > lines[1].Chance ? 1 : 2;
                }
            }

            #region just as comments (.attacker is not implemented atm)
            // Check if building attacks Tower (.attacker is not implemented atm)
            //if (p.ownKingsTower?.attacker?.type == boardObjType.BUILDING)
            //    return 3;
            //if (p.ownPrincessTower1?.attacker?.type == boardObjType.BUILDING)
            //    return 1;
            //if (p.ownPrincessTower2?.attacker?.type == boardObjType.BUILDING)
            //    return 2;
            #endregion
        }

        public static BoardObj GetBestDefender(Playfield p)
        {
            // TODO: Find better condition
            BoardObj enemy = Helper.EnemyCharacterWithTheMostEnemiesAround(p, out int count, transportType.NONE);

            if (enemy == null)
                return p.ownKingsTower;

            switch (enemy.Line)
            {
                case 2:
                    return p.ownPrincessTower2;
                case 1:
                    return p.ownPrincessTower1;
                default:
                    return p.ownKingsTower;
            }

        }

        public static BoardObj IsEnemyKillWithSpellPossible(Playfield p, out Handcard resultHc)
        {
            IEnumerable<Handcard> hcs = Classification.GetOwnHandCards(p, boardObjType.PROJECTILE, SpecificCardType.SpellsDamaging);
            resultHc = null;
            if (!hcs.Any()) return null;

            foreach(Handcard hc in hcs)
            {
                resultHc = hc;
                if (hc.card.towerDamage >= p.enemyKingsTower.HP)
                    return p.enemyKingsTower;

                if(p.suddenDeath)
                {
                    if (hc.card.towerDamage >= p.enemyPrincessTower1.HP)
                        return p.enemyPrincessTower1;

                    if (hc.card.towerDamage >= p.enemyPrincessTower2.HP)
                        return p.enemyPrincessTower2;
                }
            }
            return null;
        }

        #region Not used in balanced FightMode
        public static FightState EnemyHasCharsOnTheFieldDecision(Playfield p)
        {
            if (p.ownTowers.Count > 2)
            {
                //BoardObj obj = GetNearestEnemy(p);

                // ToDo: Get most dangeroust group
                group mostDangeroustGroup = p.getGroup(false, 200, boPriority.byTotalBuildingsDPS, 3000);

                if (mostDangeroustGroup == null)
                {
                    Logger.Debug("mostDangeroustGroup = null");
                    return FightState.DKT;
                }
                int line = mostDangeroustGroup.Position.X > 8700 ? 2 : 1;
                Logger.Debug("mostDangeroustGroup.Position.X = {0} ; line = {1}", mostDangeroustGroup?.Position?.X, line);


                return line == 2 ? FightState.DPTL2 : FightState.DPTL1;
            }
            else
            {
                return FightState.DKT;
            }
        }
        public static FightState EnemyIsOnOurSideDecision(Playfield p)
        {
            Logger.Debug("Enemy is on our Side!!");
            if (p.ownTowers.Count > 2)
            {
                BoardObj obj = Helper.GetNearestEnemy(p);

                if (obj != null && obj.Line == 2)
                    return FightState.UAPTL2;
                else
                    return FightState.UAPTL1;
            }
            else
            {
                BoardObj obj = Helper.GetNearestEnemy(p);

                if (obj != null && obj.Line == 2)
                    return FightState.UAKTL2;
                else
                    return FightState.UAKTL1;
            }
        }
        #endregion
    }
}
