﻿using Robi.Common;
using Serilog;

namespace Robi.Clash.DefaultSelectors
{
    using System;
    using System.Collections.Generic;


    public class attackDef
    {
        public BoardObj attacker;
        public BoardObj target;
        public int distance = int.MaxValue;
        public int time = int.MaxValue;
        public VectorAI attPos;
        public VectorAI tgtPos;
        public bool empty = true;

        public attackDef()
        {
        }

        public attackDef(BoardObj att, BoardObj tgt)
        {
            if (att.Line != tgt.Line && tgt.Tower < 10) return;
            if (att.Atk == 0) return;
            switch (att.TargetType)
            {
                case targetType.GROUND:
                    if (tgt.Transport == transportType.AIR) return;
                    break;
                case targetType.NONE: return;
            }

            this.attacker = att;
            this.target = tgt;
            this.empty = false;

            //TODO: here we calc attackPos based on range and collision radius and speed both bo - просто добавить учёт радиуса коллизий
            if (att.type == boardObjType.BUILDING || att.frozen) att.Speed = 0;
            if (tgt.type == boardObjType.BUILDING || tgt.frozen) tgt.Speed = 0;

            //TODO: find how to calc it without Math
            int fullDistance = (int)Math.Sqrt((att.Position.X - tgt.Position.X) * (att.Position.X - tgt.Position.X) + (att.Position.Y - tgt.Position.Y) * (att.Position.Y - tgt.Position.Y));
            int attackDistance = fullDistance - att.Range;

            if (attackDistance <= 0) this.time = 0;
            else
            {
                //TODO: get real direction
                //-TODO: переделать с учётом того что цель может двигаться а может и нет и вообще у неё тоже есть радиус атаки и она может остановиться...
                //-тут мы учли обе скорости, но это не правильно
                if (att.Speed + tgt.Speed == 0)
                {
                    this.empty = true;
                    return;
                }
                this.time = attackDistance / (att.Speed + tgt.Speed);
            }


            int distanceAtt = att.Speed * this.time;
            int distanceTgt = tgt.Speed * this.time;
            this.attPos = new VectorAI(att.Position);
            this.tgtPos = new VectorAI(tgt.Position);
            if (this.time > 0)
            {
                if (att.Speed > 0)
                {
                    this.attPos.X = ((fullDistance - distanceAtt) * att.Position.X + distanceAtt * tgt.Position.X) / fullDistance;
                    this.attPos.Y = ((fullDistance - distanceAtt) * att.Position.Y + distanceAtt * tgt.Position.Y) / fullDistance;
                }
                if (tgt.Speed > 0)
                {
                    this.tgtPos.X = ((fullDistance - distanceTgt) * tgt.Position.X + distanceTgt * att.Position.X) / fullDistance;
                    this.tgtPos.Y = ((fullDistance - distanceTgt) * tgt.Position.Y + distanceTgt * att.Position.Y) / fullDistance; //-переделать под реальные радиусы и учесть точку останова для радиусных
                }
            }
        }
    }

    public class opposite
    {
        public CardDB.cardName name = CardDB.cardName.unknown;
        public int value = int.MinValue;
        public BoardObj bo = null;
        public Handcard hc = null;
        public BoardObj target = null; //TODO: leave target or targetPosition
                                       //public VectorAI targetPosition;

        public opposite()
        {
        }

        public opposite(CardDB.cardName name, int value, BoardObj bo, BoardObj target)
        {
            this.name = name;
            this.value = value;
            this.bo = bo;
            this.target = target;
        }

        public opposite(CardDB.cardName name, int value, Handcard hc, BoardObj target)
        {
            this.name = name;
            this.value = value;
            this.hc = hc;
            this.target = target;
        }

    }

    public enum boPriority
    {
        byTotalGroundDPS,
        byTotalAirDPS,
        byTotalGroundAreaDPS,
        byTotalAirAreaDPS,
        byTotalBuildingsDPS,
        byTotalAirTransport,
        byTotalNumber,

        byLowHpNumber,
        byAvgHpNumber,
        byHiHpNumber,
    }

    public class group
    {
        public int SquaredR = 9000000; //R = 3000
        public bool own = true;
        public VectorAI Position = new VectorAI(0, 0);
        private bool sum = false;
        private int LowHPlimit = 50;

        public List<BoardObj> lowHPbo = new List<BoardObj>(); //list of all units with HP <= LowHPlimit 
        public int lowHPboGroundDPS = 0; //total DPS on Ground objects from all units with HP <= LowHPlimit
        public int lowHPboGroundAreaDPS = 0; //total area DPS on Ground troops from units with HP <= LowHPlimit (considers only units with Area Damage)
        public int lowHPboAirDPS = 0; //total DPS on Air troops from all units with HP <= LowHPlimit
        public int lowHPboAirAreaDPS = 0; //total area DPS on Air troops from units with HP <= LowHPlimit (considers only units with Area Damage)
        public int lowHPboBuildingsDPS = 0; //total DPS on Buildings from all units with HP <= LowHPlimit
        public int lowHPboAirTransport = 0; //num units with HP <= LowHPlimit who can fly (transportType.AIR)
        public int lowHPboHP = 0; //average HP per 1 unit from units with HP <= LowHPlimit

        public List<BoardObj> avgHPbo = new List<BoardObj>(); //hp: LowHPlimit < averageUnits < 550
        public int avgHPboGroundDPS = 0;
        public int avgHPboGroundAreaDPS = 0;
        public int avgHPboAirDPS = 0;
        public int avgHPboAirAreaDPS = 0;
        public int avgHPboBuildingsDPS = 0;
        public int avgHPboAirTransport = 0;
        public int avgHPboHP = 0;

        public List<BoardObj> hiHPbo = new List<BoardObj>(); //units with hp > 550
        public int hiHPboGroundDPS = 0;
        public int hiHPboGroundAreaDPS = 0;
        public int hiHPboAirDPS = 0;
        public int hiHPboAirAreaDPS = 0;
        public int hiHPboBuildingsDPS = 0;
        public int hiHPboAirTransport = 0;
        public int hiHPboHP = 0;

        public group(bool own, VectorAI position, List<BoardObj> list, int lowHpLimit, bool recalcParams = false, int radius = 3000)
        {
            sum = false;
            this.own = own;
            this.Position = position;
            this.SquaredR = radius * radius;
            this.LowHPlimit = lowHpLimit;
            addToGroup(list, recalcParams);
        }

        public void addToGroup(List<BoardObj> list, bool recalcParams = false)
        {
            sum = false;
            BoardObj bo;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                bo = list[i];
                if (bo.HP < 1) continue;
                if ((Position.X - bo.Position.X) * (Position.X - bo.Position.X) + (Position.Y - bo.Position.Y) * (Position.Y - bo.Position.Y) < SquaredR)
                {
                    if (bo.HP <= LowHPlimit) lowHPbo.Add(bo);
                    else if (bo.HP > 550) hiHPbo.Add(bo);
                    else avgHPbo.Add(bo);
                }
            }
            if (recalcParams) recalc();
        }

        private void clear()
        {
            lowHPboGroundDPS = 0;
            lowHPboGroundAreaDPS = 0;
            lowHPboAirDPS = 0;
            lowHPboAirAreaDPS = 0;
            lowHPboBuildingsDPS = 0;
            lowHPboAirTransport = 0;
            lowHPboHP = 0;

            avgHPboGroundDPS = 0;
            avgHPboGroundAreaDPS = 0;
            avgHPboAirDPS = 0;
            avgHPboAirAreaDPS = 0;
            avgHPboBuildingsDPS = 0;
            avgHPboAirTransport = 0;
            avgHPboHP = 0;

            hiHPboGroundDPS = 0;
            hiHPboGroundAreaDPS = 0;
            hiHPboAirDPS = 0;
            hiHPboAirAreaDPS = 0;
            hiHPboBuildingsDPS = 0;
            hiHPboAirTransport = 0;
            hiHPboHP = 0;
        }

        public void recalc()
        {
            if (sum) return;
            sum = true;
            clear();

            BoardObj bo;
            int count = lowHPbo.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    bo = lowHPbo[i];
                    //TODO real dps, not first hit
                    lowHPboHP += bo.HP;
                    lowHPboBuildingsDPS += bo.Atk; //TODO mb add check for aoe
                    switch (bo.TargetType)
                    {
                        case targetType.GROUND:
                            lowHPboGroundDPS += bo.Atk;
                            if (bo.card.DamageRadius > 1000) lowHPboGroundAreaDPS += bo.Atk;
                            continue;
                        case targetType.ALL:
                            lowHPboGroundDPS += bo.Atk;
                            lowHPboAirDPS += bo.Atk;
                            if (bo.card.DamageRadius > 1000)
                            {
                                lowHPboGroundAreaDPS += bo.Atk;
                                lowHPboAirAreaDPS += bo.Atk;
                            }
                            continue;
                    }
                    if (bo.Transport == transportType.AIR) lowHPboAirTransport++;
                }
                lowHPboHP = lowHPboHP / lowHPbo.Count;
            }

            count = avgHPbo.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    bo = avgHPbo[i];
                    avgHPboHP += bo.HP;
                    avgHPboBuildingsDPS += bo.Atk; //TODO mb add check for aoe
                    switch (bo.TargetType)
                    {
                        case targetType.GROUND:
                            avgHPboGroundDPS += bo.Atk;
                            if (bo.card.DamageRadius > 1000) avgHPboGroundAreaDPS += bo.Atk;
                            continue;
                        case targetType.ALL:
                            avgHPboGroundDPS += bo.Atk;
                            avgHPboAirDPS += bo.Atk;
                            if (bo.card.DamageRadius > 1000)
                            {
                                avgHPboGroundAreaDPS += bo.Atk;
                                avgHPboAirAreaDPS += bo.Atk;
                            }
                            continue;
                    }
                    if (bo.Transport == transportType.AIR) avgHPboAirTransport++;
                }
                avgHPboHP = avgHPboHP / avgHPbo.Count;
            }

            count = hiHPbo.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    bo = hiHPbo[i];
                    hiHPboHP += bo.HP;
                    hiHPboBuildingsDPS += bo.Atk; //TODO mb add check for aoe
                    switch (bo.TargetType)
                    {
                        case targetType.GROUND:
                            hiHPboGroundDPS += bo.Atk;
                            if (bo.card.DamageRadius > 1000) hiHPboGroundAreaDPS += bo.Atk;
                            continue;
                        case targetType.ALL:
                            hiHPboGroundDPS += bo.Atk;
                            hiHPboAirDPS += bo.Atk;
                            if (bo.card.DamageRadius > 1000)
                            {
                                hiHPboGroundAreaDPS += bo.Atk;
                                hiHPboAirAreaDPS += bo.Atk;
                            }
                            continue;
                    }
                    if (bo.Transport == transportType.AIR) hiHPboAirTransport++;
                }
                hiHPboHP = hiHPboHP / hiHPbo.Count;
            }
        }

        public bool isInGroup(VectorAI pos)
        {
            return (Position.X - pos.X) * (Position.X - pos.X) + (Position.Y - pos.Y) * (Position.Y - pos.Y) < SquaredR;
        }
    }

    public class Playfield
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<Playfield>();

        public List<Handcard> ownHandCards = new List<Handcard>();
        public Handcard nextCard = new Handcard();
        public Handcard prevCard = new Handcard();
        public List<CardDB.cardName> ownDeck = new List<CardDB.cardName>();
        public List<CardDB.cardName> enemyDeck = new List<CardDB.cardName>();

        public List<BoardObj> ownMinions = new List<BoardObj>();
        public List<BoardObj> enemyMinions = new List<BoardObj>();

        public List<BoardObj> ownAreaEffects = new List<BoardObj>();
        public List<BoardObj> enemyAreaEffects = new List<BoardObj>();

        public List<BoardObj> ownBuildings = new List<BoardObj>();
        public List<BoardObj> enemyBuildings = new List<BoardObj>();

        public BoardObj ownKingsTower = new BoardObj();
        public BoardObj ownPrincessTower1 = new BoardObj();
        public BoardObj ownPrincessTower2 = new BoardObj();
        public BoardObj enemyKingsTower = new BoardObj();
        public BoardObj enemyPrincessTower1 = new BoardObj();
        public BoardObj enemyPrincessTower2 = new BoardObj();

        public List<BoardObj> ownTowers = new List<BoardObj>();
        public List<BoardObj> ownPrincessTowers = new List<BoardObj>();
        public List<BoardObj> enemyTowers = new List<BoardObj>();
        public List<BoardObj> enemyPrincessTowers = new List<BoardObj>();


        public group ownGroup = null;
        public group enemyGroup = null;

        public int ownerIndex = -1;
        public int ownMana = 0;
        public int enemyMana = 0;
        public TimeSpan BattleTime;

        public bool logging = false;
        public bool home = true;
        public int nextEntity = 70;
        public int pID = 0;
        public int timeShift = 0;
        public Cast bestCast = null;
        public bool suddenDeath = false;

        //public Hrtprozis prozis = Hrtprozis.Instance;
        public int evaluatePenality = 0;
        public int ruleWeight = 0;
        public string rulesUsed = "";
        public bool needPrint = false;
        //public Int64 hashcode = 0; //TODO
        public float value = Int32.MinValue;
        //public int guessingKingTowerHP = 30;                
        public List<Action> playactions = new List<Action>();
        public List<int> pIdHistory = new List<int>();

        private void addTower(BoardObj tower)
        {
            //maybe add duplicate check (todo) - depending on the performance
            if (tower.own)
            {
                ownTowers.Add(tower);
                if (tower.Tower < 9) ownPrincessTowers.Add(tower);
            }
            else
            {
                enemyTowers.Add(tower);
                if (tower.Tower < 9) enemyPrincessTowers.Add(tower);
            }
        }

        private void copyBoardObj(List<BoardObj> source, List<BoardObj> trgt)
        {
            foreach (BoardObj bo in source)
            {
                trgt.Add(new BoardObj(bo));
            }
        }

        private void copyCards(List<Handcard> source, Handcard nxtCard, Handcard prvCard)
        {
            foreach (Handcard hc in source)
            {
                this.ownHandCards.Add(new Handcard(hc));
            }
            this.nextCard = new Handcard(nxtCard);
            this.prevCard = new Handcard(prvCard);
        }

        public Playfield()
        {
            //this.pID = prozis.getPid();
            if (this.needPrint)
            {
                this.pIdHistory.Add(pID);
            }
            this.nextEntity = 1000;
            this.evaluatePenality = 0;
            this.ruleWeight = 0;
            this.rulesUsed = "";
        }

        public void initTowers()
        {
            int i = 0;
            int kingsLine = 0;
            ownTowers.Add(ownKingsTower);
            if (ownPrincessTower1.HP > 0)
            {
                i += ownPrincessTower1.Line;
                ownTowers.Add(ownPrincessTower1);
                ownPrincessTowers.Add(ownPrincessTower1);
            }
            if (ownPrincessTower2.HP > 0)
            {
                i += ownPrincessTower2.Line;
                ownTowers.Add(ownPrincessTower2);
                ownPrincessTowers.Add(ownPrincessTower2);
            }
            switch (i)
            {
                case 0:
                    kingsLine = 3;
                    break;
                case 1:
                    kingsLine = 2;
                    break;
                case 2:
                    kingsLine = 1;
                    break;
            }
            ownKingsTower.Line = kingsLine;

            i = 0;
            kingsLine = 0;
            enemyTowers.Add(enemyKingsTower);
            if (enemyPrincessTower1.HP > 0)
            {
                i += enemyPrincessTower1.Line;
                enemyTowers.Add(enemyPrincessTower1);
                enemyPrincessTowers.Add(enemyPrincessTower1);
            }
            if (enemyPrincessTower2.HP > 0)
            {
                i += enemyPrincessTower2.Line;
                enemyTowers.Add(enemyPrincessTower2);
                enemyPrincessTowers.Add(enemyPrincessTower2);
            }
            switch (i)
            {
                case 0:
                    kingsLine = 3;
                    break;
                case 1:
                    kingsLine = 2;
                    break;
                case 2:
                    kingsLine = 1;
                    break;
            }
            enemyKingsTower.Line = kingsLine;
        }

        public Playfield(Playfield p, int timeShift = 0)
        {
            //this.pID = prozis.getPid();
            this.ownerIndex = p.ownerIndex;
            this.BattleTime = p.BattleTime;
            this.home = p.home;
            this.ownMana = p.ownMana;
            this.enemyMana = p.enemyMana;

            copyCards(p.ownHandCards, p.nextCard, p.prevCard);

            copyBoardObj(p.ownMinions, this.ownMinions);
            copyBoardObj(p.enemyMinions, this.enemyMinions);

            copyBoardObj(p.ownAreaEffects, this.ownAreaEffects);
            copyBoardObj(p.enemyAreaEffects, this.enemyAreaEffects);

            copyBoardObj(p.ownBuildings, this.ownBuildings);
            copyBoardObj(p.enemyBuildings, this.enemyBuildings);

            copyBoardObj(p.ownTowers, this.ownTowers);
            copyBoardObj(p.enemyTowers, this.enemyTowers);

            this.ownKingsTower = new BoardObj(p.ownKingsTower);
            this.ownPrincessTower1 = new BoardObj(p.ownPrincessTower1);
            this.ownPrincessTower2 = new BoardObj(p.ownPrincessTower2);

            this.enemyKingsTower = new BoardObj(p.enemyKingsTower);
            this.enemyPrincessTower1 = new BoardObj(p.enemyPrincessTower1);
            this.enemyPrincessTower2 = new BoardObj(p.enemyPrincessTower2);

            initTowers();

            this.ownDeck = p.ownDeck;
            this.enemyDeck = p.enemyDeck;

            this.ownGroup = p.ownGroup;
            this.enemyGroup = p.enemyGroup;
            //enemyHandCards = prozis.enemyHandCards;

            if (p.needPrint)
            {
                this.needPrint = true;
                this.pIdHistory.AddRange(p.pIdHistory);
                this.pIdHistory.Add(pID);
            }
            this.nextEntity = p.nextEntity;
            this.suddenDeath = p.suddenDeath;


            this.evaluatePenality = p.evaluatePenality;
            this.ruleWeight = p.ruleWeight;
            this.rulesUsed = p.rulesUsed;

            this.playactions.AddRange(p.playactions);

            if (timeShift != 0) TimeMachine.Instance.setTimeShift(this, timeShift);
        }


        public bool isEqual(Playfield p, bool logg = false)
        {
            //TODO
            return true;
        }

        public Int64 getPHash()
        {
            //TODO
            Int64 retval = 0;
            return retval;
        }

        public group getGroup(bool own, int lowHPlimit = 70, boPriority priority = boPriority.byTotalNumber, int radius = 3000)
        {
            group retval = null;
            group Group;
            BoardObj bo;
            List<BoardObj> listMobs = own ? this.ownMinions : this.enemyMinions;
            List<BoardObj> listBuildings = own ? this.ownBuildings : this.enemyBuildings;
            List<BoardObj> listTowers = own ? this.ownTowers : this.enemyTowers;
            int val = -1;
            int tmpval = 0;
            int count = listMobs.Count;
            for (int i = 0; i < count; i++)
            {
                bo = listMobs[i];
                Group = new group(own, bo.Position, listMobs, lowHPlimit, false, radius);
                Group.addToGroup(listBuildings, false);
                Group.addToGroup(listTowers, true);
                switch (priority)
                {
                    case boPriority.byTotalNumber:
                        tmpval = Group.lowHPbo.Count + Group.avgHPbo.Count + Group.hiHPbo.Count;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                    case boPriority.byLowHpNumber:
                        tmpval = Group.lowHPbo.Count;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                    case boPriority.byAvgHpNumber:
                        tmpval = Group.avgHPbo.Count;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                    case boPriority.byHiHpNumber:
                        tmpval = Group.hiHPbo.Count;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                    case boPriority.byTotalBuildingsDPS:
                        tmpval = Group.lowHPboBuildingsDPS + Group.avgHPboBuildingsDPS + Group.hiHPboBuildingsDPS;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                    case boPriority.byTotalAirDPS:
                        tmpval = Group.lowHPboAirDPS + Group.avgHPboAirDPS + Group.hiHPboAirDPS;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                    case boPriority.byTotalAirAreaDPS:
                        tmpval = Group.lowHPboAirAreaDPS + Group.avgHPboAirAreaDPS + Group.hiHPboAirAreaDPS;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                    case boPriority.byTotalGroundDPS:
                        tmpval = Group.lowHPboGroundDPS + Group.avgHPboGroundDPS + Group.hiHPboGroundDPS;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                    case boPriority.byTotalGroundAreaDPS:
                        tmpval = Group.lowHPboGroundAreaDPS + Group.avgHPboGroundAreaDPS + Group.hiHPboGroundAreaDPS;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;

                    case boPriority.byTotalAirTransport:
                        tmpval = Group.lowHPboAirTransport + Group.avgHPboAirTransport + Group.hiHPboAirTransport;
                        if (tmpval > val) { val = tmpval; retval = Group; }
                        break;
                }
            }

            return retval;
        }

        public void rotateLines()
        {
            //-Here we rotate towers and locking for nearest threat
            int i = ownTowers.Count;
            if (i < 1) return;
            List<attackDef> attackersList = new List<attackDef>();
            List<attackDef> tmp;
            attackDef near = new attackDef();
            foreach (BoardObj t in this.ownTowers)
            {
                tmp = t.getImmediateAttackers(this);
                if (tmp.Count > 0 && (near.empty || near.time > tmp[0].time)) near = tmp[0];
            }
            if (!near.empty)
            {
                Playfield p = new Playfield(this, near.time);
                BoardObj myObj = near.attacker.own ? near.attacker : near.target;
                opposite opp = KnowledgeBase.Instance.getOppositeToAll(p, myObj);

                if (opp != null)
                {
                    if (opp.hc != null)
                    {
                        int callTime = near.time - opp.hc.card.DeployTime;
                        if (callTime < 0) callTime = 0;
                        p = new Playfield(p, callTime);
                        VectorAI position = new VectorAI(0, 0);
                        //VectorAI position = p.getDeployPosition(opp); //TODO: this decision takes a behavior or a time machine

                        bestCast = new Cast(opp.hc.name, position, opp.hc);
                    }
                    else if (opp.bo != null)
                    {
                        if (opp.bo.TargetType == targetType.GROUND)
                        {
                            //TODO 
                        }
                    }
                    else
                    {
                        //TODO automatic creation opposite list in getOppositeToAll or do something else
                    }
                }
            }

        }

        public VectorAI getDeployPosition(deployDirectionAbsolute absoluteDirection = deployDirectionAbsolute.none)
        {
            //Absolute directions
            int sign = this.home ? 1 : -1;
            switch (absoluteDirection)
            {
                case deployDirectionAbsolute.behindKingsTowerCenter: return home ? new VectorAI(9500, 900) : new VectorAI(9500, 30900); //TODO: test !home
                case deployDirectionAbsolute.behindKingsTowerLine1: return home ? new VectorAI(7500, 1000) : new VectorAI(7300, 30950);
                case deployDirectionAbsolute.behindKingsTowerLine2: return home ? new VectorAI(11200, 1100) : new VectorAI(11100, 31000);
                case deployDirectionAbsolute.cornerLine1: return home ? new VectorAI(800, 2200) : new VectorAI(500, 30000);
                case deployDirectionAbsolute.cornerLine2: return home ? new VectorAI(17000, 2500) : new VectorAI(17500, 30100);
                case deployDirectionAbsolute.bridgeLine1: return home ? new VectorAI(3500, 14900) : new VectorAI(3500, 17200);
                case deployDirectionAbsolute.bridgeLine2: return home ? new VectorAI(14100, 15100) : new VectorAI(14100, 17000);
                case deployDirectionAbsolute.betweenBridges: return new VectorAI(9500, 16000); //TODO test
                case deployDirectionAbsolute.borderBridgeLine1: return home ? new VectorAI(600, 14900) : new VectorAI(550, 17200); //TODO: test
                case deployDirectionAbsolute.borderBridgeLine2: return home ? new VectorAI(17000, 16700) : new VectorAI(17100, 17000); //TODO: test
                case deployDirectionAbsolute.ownPrincessTowerLine1: return home ? new VectorAI(3500, 6500) : new VectorAI(3500, 25500);
                case deployDirectionAbsolute.ownPrincessTowerLine2: return home ? new VectorAI(14500, 6500) : new VectorAI(14500, 25500);
                case deployDirectionAbsolute.enemyPrincessTowerLine1: return home ? new VectorAI(3500, 25500) : new VectorAI(3500, 6500);
                case deployDirectionAbsolute.enemyPrincessTowerLine2: return home ? new VectorAI(14500, 25500) : new VectorAI(14500, 6500);
            }
            Logger.Debug("!!![getDeployPosition]Error: Absolute directions unhandled: " + absoluteDirection);
            return new VectorAI(0, 0);
        }

        public VectorAI getDeployPosition(VectorAI targetPosition, deployDirectionRelative relativeDirection = deployDirectionRelative.none, int deployDistance = 0) //for deployDistance you can use hc.card.DamageRadius; we  use only for Absolute directions
        {
            //Relative directions
            int sign = this.home ? 1 : -1;
            int lineSign = targetPosition.X > 8700 ? 1 : -1;

            if (targetPosition == null)
            {
                Logger.Debug("!!![getDeployPosition]Error: Relative targetPosition == NULL");
                return new VectorAI(0, 0);
            }

            switch (relativeDirection)
            {
                case deployDirectionRelative.Up: return new VectorAI(targetPosition.X, targetPosition.Y + sign * (1000 + deployDistance));
                case deployDirectionRelative.Down: return new VectorAI(targetPosition.X, targetPosition.Y - sign * (1000 + deployDistance));

                case deployDirectionRelative.RightUp: return new VectorAI(targetPosition.X - sign * (1000 + deployDistance * 7071 / 10000), targetPosition.Y + sign * (1000 + deployDistance * 7071 / 10000));
                case deployDirectionRelative.Right: return new VectorAI(targetPosition.X - sign * (1000 + deployDistance), targetPosition.Y);
                case deployDirectionRelative.RightDown: return new VectorAI(targetPosition.X - sign * (1000 + deployDistance * 7071 / 10000), targetPosition.Y - sign * (1000 + deployDistance * 7071 / 10000));
                case deployDirectionRelative.LeftDown: return new VectorAI(targetPosition.X + sign * (1000 + deployDistance * 7071 / 10000), targetPosition.Y - sign * (1000 + deployDistance * 7071 / 10000));
                case deployDirectionRelative.Left: return new VectorAI(targetPosition.X + sign * (1000 + deployDistance), targetPosition.Y);
                case deployDirectionRelative.LeftUp: return new VectorAI(targetPosition.X + sign * (1000 + deployDistance * 7071 / 10000), targetPosition.Y + sign * (1000 + deployDistance * 7071 / 10000));

                case deployDirectionRelative.borderSideUp: return new VectorAI(targetPosition.X + lineSign * (1000 + deployDistance * 7071 / 10000), targetPosition.Y + sign * (1000 + deployDistance * 7071 / 10000));
                case deployDirectionRelative.borderSideMiddle: return new VectorAI(targetPosition.X + lineSign * (1000 + deployDistance), targetPosition.Y);
                case deployDirectionRelative.borderSideDown: return new VectorAI(targetPosition.X + lineSign * (1000 + deployDistance * 7071 / 10000), targetPosition.Y - sign * (1000 + deployDistance * 7071 / 10000));
                case deployDirectionRelative.centerSideUp: return new VectorAI(targetPosition.X - lineSign * (1000 + deployDistance * 7071 / 10000), targetPosition.Y + sign * (1000 + deployDistance * 7071 / 10000));
                case deployDirectionRelative.centerSideMiddle: return new VectorAI(targetPosition.X - lineSign * (1000 + deployDistance), targetPosition.Y);
                case deployDirectionRelative.centerSideDown: return new VectorAI(targetPosition.X - lineSign * (1000 + deployDistance * 7071 / 10000), targetPosition.Y - sign * (1000 + deployDistance * 7071 / 10000));

                case deployDirectionRelative.lineCorner: return (lineSign > 0) ? (home ? new VectorAI(17000, 2500) : new VectorAI(17450, 30080)) : (home ? new VectorAI(800, 2200) : new VectorAI(550, 30050));
                default:
                    return new VectorAI(targetPosition);
            }
        }

        public VectorAI getDeployPosition(BoardObj bo, deployDirectionRelative relativeDirection = deployDirectionRelative.none, int deployDistance = 0) //for deployDistance you can use hc.card.DamageRadius
        {
            if (bo == null)
            {
                Logger.Debug("!!![getDeployPosition]Error:BoardObj == NULL");
                return new VectorAI(0, 0);
            }
            else return getDeployPosition(bo.Position, relativeDirection, deployDistance);
        }

        public int getDistanceToPointFromBorder(VectorAI Position) //TODO: get actual size fom game for Battlefield + bool CanDeploy
        {
            int retval = 0;
            if ((Position.Y > 15250) == home)
            {
                int line = Position.X > 8700 ? 2 : 1;
                foreach (BoardObj bo in this.enemyTowers)
                {
                    if (bo.Line == line)
                    {

                    }
                    else if (bo.Line == 3)
                    {

                    }
                }
            }
            return retval;
        }

        public bool noEnemiesOnMySide()
        {
            foreach (BoardObj m in this.enemyMinions)
            {
                if ((m.Position.Y < 15250) == home) return false;
            }
            foreach (BoardObj b in this.enemyBuildings)
            {
                if ((b.Position.Y < 15250) == home) return false;
            }
            return true;
        }

        public Handcard getTankCard()
        {
            Handcard retval = null;
            foreach (Handcard hc in ownHandCards)
            {
                if (hc.card.type == boardObjType.MOB && (retval == null || hc.card.MaxHP > retval.card.MaxHP)) retval = hc;
            }
            if (retval != null && retval.card.MaxHP > 800) return retval;
            return null;
        }

        public Handcard getCheapestCard(boardObjType type, targetType tgtType)
        {
            Handcard retval = null;
            List<Handcard> tmp = new List<Handcard>();
            foreach (Handcard hc in ownHandCards)
            {
                if ((hc.card.type == type || type == boardObjType.NONE) &&
                    (hc.card.TargetType == targetType.NONE || hc.card.TargetType == tgtType)) tmp.Add(hc);
            }
            foreach (Handcard hc in tmp)
            {
                if (retval == null || hc.card.cost < retval.card.cost) retval = hc;
            }
            return retval;
        }

        public List<Handcard> getCardsByType(boardObjType type)
        {
            List<Handcard> retval = new List<Handcard>();
            int count = ownHandCards.Count;
            Handcard hc;
            for (int i = 0; i < count; i++)
            {
                hc = ownHandCards[i];
                if (hc.card.type == type) retval.Add(hc);
            }
            return retval;
        }

        public Handcard getPatnerForMobInPeace(BoardObj bo)
        {
            if (bo == null) return null;

            Handcard retval = null;
            List<Handcard> air = new List<Handcard>();
            List<Handcard> troops = new List<Handcard>();
            bool needDamager = false;
            bool needAir = false;
            if (bo.HP > 600) needDamager = true;
            if (bo.TargetType != targetType.ALL) needAir = true;
            foreach (Handcard hc in ownHandCards)
            {
                if (hc.card.type != boardObjType.MOB) continue;
                troops.Add(hc);
                if (hc.card.Transport == transportType.AIR) air.Add(hc);
            }
            if (needAir) retval = getMobCardByCondition(air, needDamager);
            if ((needAir && retval == null) || !needAir) retval = getMobCardByCondition(troops, needDamager);
            return retval;
        }

        /*
        private Handcard getFinisher(BoardObj bo, bool canWait) //useful for Towers
        {
            Handcard retval = null;
            Handcard hc;

            Handcard projectile = null;
            Handcard aoe = null;
            Handcard mob = null;
            int count = this.ownHandCards.Count;
            for (int i = 1; i < count; i++)
            {
                hc = ownHandCards[i];
                switch (hc.card.type)
                {
                    case boardObjType.PROJECTILE:
                        if (bo.HP <= hc.card.Atk)
                        {
                            if (projectile == null || hc.card.Atk > projectile.card.Atk) projectile = hc;
                        }
                        continue;
                    case boardObjType.AOE:
                        int towerDmg = hc.card.towerDamage;
                        if (hc.card.name == CardDB.cardName.poison) towerDmg *= 8;
                        if (bo.HP <= towerDmg)
                        {
                            if (aoe == null || towerDmg > aoe.extraVal)
                            {
                                aoe = hc;
                                aoe.extraVal = towerDmg;
                            }
                        }
                        continue;
                    case boardObjType.MOB:
                        if (bo.type == boardObjType.BUILDING)
                        {
                            if (KnowledgeBase.Instance.)
                            //TODO: calc online
                            if (hc.card.TargetType == targetType.BUILDINGS)
                            {
                                int val;
                                if (bo.HP <= hc.card.Atk)
                                {

                                }
                                else
                                {
                                    int dmg = hc.card.Atk * hc.card.SummonNumber;
                                    int restHp = bo.HP - dmg;
                                    double timeForDestroy = restHp / (dmg * 1000 / hc.card.HitSpeed);
                                    double timeToDestruction = hc.card.MaxHP / (bo.Atk * 1000 / bo.card.HitSpeed);
                                    double delta = timeToDestruction - timeForDestroy;
                                    if (hc.card.SummonNumber > 1)
                                    {
                                        double hitPerMob = Math.Ceiling((double)hc.card.MaxHP / bo.Atk);
                                        timeToDestruction = hc.card.SummonNumber * hitPerMob * 1000 / bo.card.HitSpeed;

                                    }
                                    if (delta > 0)
                                    {
                                        if (mob == null || hc.card.Atk > mob.card.Atk)
                                        {
                                            mob = hc;
                                            mob.extraVal = delta;
                                        }
                                    }
                                }
                            }


                        }
                }
                if (ownHandCards[i].card.MaxHP > retval.card.MaxHP) retval = ownHandCards[i];
            }
            return retval;
        }*/

        private Handcard getMobCardByCondition(List<Handcard> list, bool needDamager) //!needDamager mean needTank
        {
            Handcard retval = null;
            int mobsCount = list.Count;
            if (mobsCount > 0)
            {
                retval = list[0];
                if (needDamager)
                {
                    for (int i = 1; i < mobsCount; i++)
                    {
                        if (list[i].card.Atk > retval.card.Atk)
                        {
                            if (list[i].card.DamageRadius >= retval.card.DamageRadius) retval = list[i]; //-TODO: DamageRadius - collect it
                        }
                        else
                        {
                            if (list[i].card.SpawnNumber > retval.card.SpawnNumber || list[i].card.SummonNumber > retval.card.SummonNumber ||
                                list[i].card.DamageRadius > retval.card.DamageRadius) retval = list[i]; //TODO: check this val for troops like minionhorde
                        }
                    }
                }
                else
                {
                    for (int i = 1; i < mobsCount; i++)
                    {
                        if (list[i].card.MaxHP > retval.card.MaxHP) retval = list[i];
                    }
                }
            }
            return retval;
        }

        public BoardObj getFrontMob()  //TODO: perhaps add for enemy minions
        {
            BoardObj retval = null;
            int count = ownMinions.Count;
            if (count > 0)
            {
                retval = ownMinions[0];
                for (int i = 1; i < count; i++)
                {
                    if (!retval.aheadOf(ownMinions[i], home)) retval = ownMinions[i];
                }
            }
            return retval;
        }

        public VectorAI getBackPosition(Handcard hc, int line = 1)
        {
            if (hc.card.Transport == transportType.GROUND)
            {
                return getDeployPosition(line == 1 ? deployDirectionAbsolute.behindKingsTowerLine1 : deployDirectionAbsolute.behindKingsTowerLine2);
            }
            else
            {
                return getDeployPosition(line == 1 ? deployDirectionAbsolute.cornerLine1 : deployDirectionAbsolute.cornerLine2);
            }
        }

        public void setKingsLine(bool own)
        {
            BoardObj tower = own ? this.ownKingsTower : this.enemyKingsTower;
            List<BoardObj> list = own ? this.ownTowers : this.enemyTowers;
            int i = 0;
            foreach (BoardObj t in list) if (t.Tower < 10) i += t.Line;
            tower.Line = 0;
            switch (i)
            {
                case 0: tower.Line = 3; break;
                case 1: tower.Line = 2; break;
                case 2: tower.Line = 1; break;
            }
            foreach (BoardObj t in list) if (t.Tower > 9) t.Line = tower.Line;
        }

        /*
		public int getNextEntity()
		{
			int retval = this.nextEntity;
			this.nextEntity++;
			return retval;
		}*/


        public void guessObjDamage() //TODO
        {
            //или как то по другому когда один наносит урон другому - выяснить кто сильнее
            //здесь мы быстро предугадуем дамаг по башне и/или миниону
        }

        //TODO allCharsInAreaGetDamage

        private void LogBoardObject(BoardObj bo)
        {
            string extrainfo = (bo.frozen ? " frozen:" + bo.startFrozen : "") + (bo.LifeTime > 0 ? " LifeTime:" + bo.LifeTime : "") + (bo.extraData != "" ? " ed:" + bo.extraData : "");
            Logger.Debug("{type} {own:l} {ownerIndex} {Name} {GId} {Position:l} {level} {Atk} {HP} {Shield}{extrainfo:l}", bo.type, bo.own ? "o" : "e", bo.ownerIndex, bo.Name, bo.GId, bo.Position, bo.level, bo.Atk, bo.HP, bo.Shield, extrainfo);
        }

        private void LogHandCard(Handcard hc)
        {
            Logger.Debug("Hand {position} {name} {lvl} {manacost}{mirror:l}{extraData:l}", hc.position, hc.card.name, hc.lvl, hc.manacost, (hc.mirror ? " mirror" : ""), (hc.extraData == "" ? "" : " ed:" + hc.extraData));
        }

        public void print()
        {
            Logger.Debug("Data bt:{BattleTime:l} owner:{ownerIndex} mana:{ownMana} nxtc:{nname:l}:{lvl} prvc:{pname:l}:{lvl}", BattleTime.ToString(@"hh\:mm\:ss\.fff"), ownerIndex, ownMana, nextCard.name, nextCard.lvl, prevCard.name, prevCard.lvl);

            //help.logg("ownCards");
            foreach (Handcard hc in ownHandCards) LogHandCard(hc);
            //help.logg("ownTowers");
            foreach (BoardObj bo in ownTowers) LogBoardObject(bo);
            //help.logg("ownAOE");
            foreach (BoardObj bo in ownAreaEffects) LogBoardObject(bo);
            //help.logg("ownMinions");
            foreach (BoardObj bo in ownMinions) LogBoardObject(bo);
            //help.logg("ownBuildings");
            foreach (BoardObj bo in ownBuildings) LogBoardObject(bo);

            //help.logg("enemyTowers");
            foreach (BoardObj bo in enemyTowers) LogBoardObject(bo);
            //help.logg("enemyAOE");
            foreach (BoardObj bo in enemyAreaEffects) LogBoardObject(bo);
            //help.logg("enemyMinions");
            foreach (BoardObj bo in enemyMinions) LogBoardObject(bo);
            //help.logg("enemyBuildings");
            foreach (BoardObj bo in enemyBuildings) LogBoardObject(bo);
        }

        public Action getNextAction()
        {
            if (this.playactions.Count >= 1) return this.playactions[0];
            return null;
        }
    }
}