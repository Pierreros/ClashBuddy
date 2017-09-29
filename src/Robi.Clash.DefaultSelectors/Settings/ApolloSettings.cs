﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Robi.Common;
using Robi.Engine.Settings.Attributes;

namespace Robi.Clash.DefaultSelectors.Settings
{
    internal class ApolloSettings : JsonSettings
    {
        internal ApolloSettings() : base(SettingsPath, "Settings", "Apollo.json")
        {

        }

        [Category("Default")]
        [DefaultValue(Player.FightStyle.Balanced)]
        [DisplayName("Fight Style")]
        [Description("Choose Apollos fight style. Smart balanced, concentrated on the Defense or as an angry rusher?")]
        public Player.FightStyle FightStyle { get; set; }

        [Category("Default")]
        [DisplayName("Enemys KingTower spell damaging mode")]
        [Description("Starts to attack the enemys KingTower with all damaging spells at the Health...")]
        [Range(0, 10000)]
        [DefaultValue(400)]
        public int KingTowerSpellDamagingHealth { get; set; }

        [Category("Default")]
        [DisplayName("Spell position correction (friendly chars around)")]
        [Description("Don´t correct spell deployment position if at least x friendly characters around the enemy")]
        [Range(0, 50)]
        [DefaultValue(2)]
        public int SpellCorrectionConditionCharCount { get; set; }

        [Category("Default")]
        [DisplayName("Spell deployment decision (minimum anzahl Characters)")]
        [Description("How many enemy characters should be at least in the area for an deploy")]
        [Range(0, 50)]
        [DefaultValue(5)]
        public int SpellDeployConditionCharCount { get; set; }

        [Category("Game Start")]
        [DisplayName("Mana-Load")]
        [Description("How much Mana till first attack")]
        [Range(0, 10)]
        [DefaultValue(9)]
        public int ManaTillFirstAttack { get; set; }

        [Category("Attack")]
        [DisplayName("Mana-Load")]
        [Description("How much Mana till deploying characters if no enemy on our side")]
        [Range(0, 10)]
        [DefaultValue(10)]
        public int ManaTillDeploy { get; set; }

        [Category("Default")]
        [DisplayName("Tank health")]
        [Description("How much health-points to classify an character as a tank")]
        [Range(0, 10000)]
        [DefaultValue(1200)]
        public int MinHealthAsTank { get; set; }

        //[SettingsGroup(" Deployment Faktor")]
        //[FloatRangeSettings(100, 0, 10000)]
        //[DefaultValue(200)]
        //public int DeploymentValue { get; set; }
    }
}
