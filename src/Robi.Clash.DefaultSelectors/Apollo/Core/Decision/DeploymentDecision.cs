using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Robi.Clash.DefaultSelectors.Apollo.Core.Classification;
using Robi.Clash.DefaultSelectors.Apollo.Other;

namespace Robi.Clash.DefaultSelectors.Apollo.Core.Decisions
{
    class DeploymentDecision
    {
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
                            if (p.enemyPrincessTower1.HP < 300 && p.enemyPrincessTower1.HP > 0)
                                return 2;
                            if (PlayfieldAnalyse.lines[0].Chance == Level.High)
                                return 2;
                            break;
                        }
                    case FightState.APTL2:
                        {
                            if (p.BattleTime.TotalSeconds < 10)
                                return 0;
                            if (p.enemyPrincessTower2.HP < 300 && p.enemyPrincessTower2.HP > 0)
                                return 2;
                            if (PlayfieldAnalyse.lines[1].Chance == Level.High)
                                return 2;
                            break;
                        }
                    case FightState.AKT:
                        {
                            if (p.BattleTime.TotalSeconds < 10)
                                return 0;
                            if (p.enemyKingsTower.HP < 300 && p.enemyKingsTower.HP > 0)
                                return 2;
                            if (PlayfieldAnalyse.lines[0].Chance == Level.High ||
                                PlayfieldAnalyse.lines[1].Chance == Level.High)
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
                if (BoardObjHelper.IsAnEnemyObjectInArea(p, p.ownKingsTower.Position, 4000, boardObjType.MOB))
                    return 2; // ToDo: Find better condition, if the handcard can´t attack the minions, we should wait
                if (PlayfieldAnalyse.lines[0].Danger == Level.High || PlayfieldAnalyse.lines[1].Danger == Level.High
                ) // ToDo: Maybe check just the line
                    return 1;
            }

            return 10;
        }



        public static bool SupportDeployment(Playfield p, int line, bool ownSide)
        {
            // If own characters already attacking and you are deploying as support
            // The chars should be deployed behind the own chars

            var attackingChars = ownSide
                ? p.ownMinions.Where(n => n.Line == line && n.onMySide(p.home))
                : p.ownMinions.Where(n => n.Line == line && !n.onMySide(p.home));

            // Maybe check also which card type: Tank deployed in front, Ranger behinde ...

            return attackingChars.Any();
        }


        public static int DangerOrBestAttackingLine(Playfield p) // Good chance for an attack?
        {
            var lines = PlayfieldAnalyse.lines;
            var lvlBorder = 1;
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
                if (lines[1].Danger >= lines[1].Chance)
                {
                    if (lines[0].Danger == lines[1].Danger)
                        return lines[0].ComparisionHP < lines[1].ComparisionHP ? -1 : -2;

                    return lines[0].Danger >= lines[1].Danger ? -1 : -2;
                }
                else
                {
                    return lines[0].Danger >= lines[1].Chance ? -1 : 2;
                }
            if (lines[1].Danger >= lines[1].Chance)
                return lines[0].Chance > lines[1].Danger ? 1 : -2;
            return lines[0].Chance > lines[1].Chance ? 1 : 2;

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

        public static BoardObj IsEnemyKillWithSpellPossible(Playfield p, out Handcard resultHc)
        {
            var hcs = ClassificationHandling.GetOwnHandCards(p, boardObjType.PROJECTILE, SpecificCardType.SpellsDamaging);
            resultHc = null;
            if (!hcs.Any()) return null;

            foreach (var hc in hcs)
            {
                resultHc = hc;
                if (hc.card.towerDamage >= p.enemyKingsTower.HP)
                    return p.enemyKingsTower;

                if (p.suddenDeath)
                {
                    if (hc.card.towerDamage >= p.enemyPrincessTower1.HP)
                        return p.enemyPrincessTower1;

                    if (hc.card.towerDamage >= p.enemyPrincessTower2.HP)
                        return p.enemyPrincessTower2;
                }
            }
            return null;
        }
    }
}
