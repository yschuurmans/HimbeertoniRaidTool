﻿using System;
using Lumina.Excel.Extensions;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GearSet
    {
        [JsonProperty("TimeStamp")]
        public DateTime? TimeStamp;
        [JsonProperty("EtroID")]
        public string EtroID = "";
        [JsonProperty("LastEtroFetched")]
        public DateTime EtroFetchDate;
        [JsonProperty("HrtID")]
        public string HrtID = "";
        [JsonProperty("Name")]
        public string Name = "";
        [JsonProperty("ManagedBy")]
        public GearSetManager ManagedBy;
        [JsonIgnore]
        private const int NumSlots = 12;
        [JsonIgnore]
        private readonly GearItem[] Items = new GearItem[NumSlots];
        [JsonProperty]
        public GearItem MainHand { get => Items[0]; set => Items[0] = value; }
        [JsonProperty]
        public GearItem Head { get => Items[1]; set => Items[1] = value; }
        [JsonProperty]
        public GearItem Body { get => Items[2]; set => Items[2] = value; }
        [JsonProperty]
        public GearItem Hands { get => Items[3]; set => Items[3] = value; }
        [JsonProperty]
        public GearItem Legs { get => Items[4]; set => Items[4] = value; }
        [JsonProperty]
        public GearItem Feet { get => Items[5]; set => Items[5] = value; }
        [JsonProperty]
        public GearItem Ear { get => Items[6]; set => Items[6] = value; }
        [JsonProperty]
        public GearItem Neck { get => Items[7]; set => Items[7] = value; }
        [JsonProperty]
        public GearItem Wrist { get => Items[8]; set => Items[8] = value; }
        [JsonProperty]
        public GearItem Ring1 { get => Items[9]; set => Items[9] = value; }
        [JsonProperty]
        public GearItem Ring2 { get => Items[10]; set => Items[10] = value; }
        [JsonProperty]
        public GearItem OffHand { get => Items[11]; set => Items[11] = value; }
        public bool IsEmpty => Array.TrueForAll(Items, x => x.ID == 0);
        public int ItemLevel
        {
            get
            {
                uint itemLevel = 0;
                for (int i = 0; i < NumSlots; i++)
                {
                    if (Items[i] != null && Items[i].ItemLevel > 0)
                    {
                        itemLevel += Items[i].ItemLevel;
                        if (Items[i].Item?.EquipSlotCategory.Value?.Disallows(GearSetSlot.OffHand) ?? false)
                            itemLevel += Items[i].ItemLevel;
                    }
                }
                return (int)((float)itemLevel / NumSlots);

            }
        }
        [JsonConstructor]
        public GearSet()
        {
            ManagedBy = GearSetManager.HRT;
            Clear();
        }
        public GearSet(GearSetManager manager, Character c, Job ac, string name = "HrtCurrent")
        {
            ManagedBy = manager;
            Name = name;
            if (ManagedBy == GearSetManager.HRT)
                HrtID = GenerateID(c, ac, this);
            Clear();
        }
        public void Clear()
        {
            for (int i = 0; i < NumSlots; i++)
            {
                Items[i] = new(0);
            }
        }
        public GearItem this[GearSetSlot slot]
        {
            get => Items[ToIndex(slot)];
            set => Items[ToIndex(slot)] = value;
        }
        public bool Contains(HrtItem item) => Array.Exists(Items, x => x.ID == item.ID);
        public bool ContainsExact(GearItem item) => Array.Exists(Items, x => x.Equals(item));
        public int GetStat(StatType type)
        {
            int result = 0;
            Array.ForEach(Items, x => result += x.GetStat(type));
            return result;
        }
        private static int ToIndex(GearSetSlot slot)
        {
            return slot switch
            {
                GearSetSlot.MainHand => 0,
                GearSetSlot.OffHand => 11,
                GearSetSlot.Head => 1,
                GearSetSlot.Body => 2,
                GearSetSlot.Hands => 3,
                GearSetSlot.Legs => 4,
                GearSetSlot.Feet => 5,
                GearSetSlot.Ear => 6,
                GearSetSlot.Neck => 7,
                GearSetSlot.Wrist => 8,
                GearSetSlot.Ring1 => 9,
                GearSetSlot.Ring2 => 10,
                _ => throw new IndexOutOfRangeException("GearSlot" + slot.ToString() + "does not exist"),
            };
        }

        internal void CopyFrom(GearSet gearSet)
        {
            TimeStamp = gearSet.TimeStamp;
            EtroID = gearSet.EtroID;
            HrtID = gearSet.HrtID;
            Name = gearSet.Name;
            ManagedBy = gearSet.ManagedBy;
            gearSet.Items.CopyTo(Items, 0);
        }
        public void UpdateID(Character c, Job ac) => HrtID = GenerateID(c, ac, this);
        public static string GenerateID(Character c, Job ac, GearSet g)
        {
            string result = "";
            result += string.Format("{0:X}-{1:X}-{2}-{3:X}", c.HomeWorldID, c.Name.ConsistentHash(), ac, g.Name.ConsistentHash());

            return result;

        }
    }
}

