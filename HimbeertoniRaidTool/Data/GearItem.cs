﻿using HimbeertoniRaidTool.Connectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class GearItem
    {
        private readonly int ID;
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public int itemLevel { get; set; }
        public GearSource Source { get; set; }

        public GearItem(int idArg)
        {
            this.ID = idArg;
        }

        private void RetrieveItemData()
        {
            GearConnector con = new EtroConnector();
            con.GetGearStats(this);
        }

        public int GetID()
        {
            return this.ID;
        }
    }
    public enum GearSource
    {
        Raid,
        Tome,
        Crafted
    }
}
