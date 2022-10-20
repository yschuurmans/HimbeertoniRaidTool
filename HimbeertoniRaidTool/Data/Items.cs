﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Extensions;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GearItem : HrtItem
    {
        [JsonProperty]
        public bool IsHq = false;
        [JsonIgnore]
        [Obsolete("Evaluate for all availbale slots")]
        public GearSetSlot Slot => (Item?.EquipSlotCategory.Value).ToSlot();
        [JsonIgnore]
        public IEnumerable<GearSetSlot> Slots => (Item?.EquipSlotCategory.Value).AvailableSlots();
        [JsonIgnore]
        public GearSource Source => CuratedData.GearSourceDB.GetValueOrDefault(ID, GearSource.undefined);
        [JsonProperty("Materia")]
        public List<HrtMateria> Materia = new();
        [JsonIgnore]
        public uint? ILevelCache = null;
        [JsonIgnore]
        public uint ItemLevel => ILevelCache ??= Item?.LevelItem.Row ?? 0;
        public bool Filled => ID > 0;
        public int GetStat(StatType type, bool includeMateria = true)
        {
            if (Item is null) return 0;
            int result = 0;
            switch (type)
            {
                case StatType.PhysicalDamage: result += Item.DamagePhys; break;
                case StatType.MagicalDamage: result += Item.DamageMag; break;
                case StatType.Defense: result += Item.DefensePhys; break;
                case StatType.MagicDefense: result += Item.DefenseMag; break;
                default:
                    if (IsHq)
                        foreach (Item.ItemUnkData73Obj param in Item.UnkData73.Where(x => x.BaseParamSpecial == (byte)type))
                            result += param.BaseParamValueSpecial;

                    foreach (Item.ItemUnkData59Obj param in Item.UnkData59.Where(x => x.BaseParam == (byte)type))
                        result += param.BaseParamValue;
                    break;
            }
            if (includeMateria)
                foreach (HrtMateria materia in Materia.Where(x => x.StatType == type))
                    result += materia.GetStat();
            return result;
        }
        public GearItem(uint ID = 0) : base(ID) { }
        public bool Equals(GearItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ID != other.ID) return false;
            if (Materia.Count != other.Materia.Count) return false;
            var cnt = new Dictionary<HrtMateria, int>();
            foreach (HrtMateria s in Materia)
            {
                if (cnt.ContainsKey(s))
                    cnt[s]++;
                else
                    cnt.Add(s, 1);
            }
            foreach (HrtMateria s in other.Materia)
            {
                if (cnt.ContainsKey(s))
                    cnt[s]--;
                else
                    return false;
            }
            foreach (int s in cnt.Values)
                if (s != 0)
                    return false;
            return true;
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class HrtItem
    {
        [JsonProperty("ID", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        protected readonly uint _ID = 0;
        public virtual uint ID => _ID;
        private Item? ItemCache = null;
        public Item? Item => ItemCache ??= _itemSheet.GetRow(ID);
        public string Name => Item?.Name.RawString ?? "";
        public bool IsGear => (Item?.ClassJobCategory.Row ?? 0) != 0;
        /// <summary>
        /// Is done this way since HrtMateria cannot be created from ItemID alone 
        /// and always will be of type HrtMateria
        /// </summary>
        public bool IsMateria => GetType().IsAssignableTo(typeof(HrtMateria));
        public bool IsExhangableItem => CuratedData.ExchangedFor.ContainsKey(ID);
        public bool IsContainerItem => CuratedData.ItemContainerDB.ContainsKey(ID);
        [JsonIgnore]
        protected static readonly ExcelSheet<Item> _itemSheet = Services.DataManager.Excel.GetSheet<Item>()!;

        public HrtItem(uint ID) => _ID = ID;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not HrtItem other) return false;
            return ID == other.ID;
        }
        public override int GetHashCode() => ID.GetHashCode();
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class HrtMateria : HrtItem
    {
        [JsonProperty("Category")]
        private readonly MateriaCategory Category;
        [JsonProperty("MateriaLevel")]
        public readonly byte MateriaLevel;
        [JsonIgnore]
        private static readonly ExcelSheet<Materia> _materiaSheet = Services.DataManager.Excel.GetSheet<Materia>()!;
        private uint? IDCache = null;
        [JsonIgnore]
        public override uint ID => IDCache ??= Materia?.Item[MateriaLevel].Row ?? 0;
        public Materia? Materia => _materiaSheet.GetRow((ushort)Category);
        public StatType StatType => (StatType)(Materia?.BaseParam.Row ?? 0);
        public HrtMateria() : this(0, 0) { }
        public HrtMateria((MateriaCategory cat, byte lvl) mat) : this(mat.cat, mat.lvl) { }
        [JsonConstructor]
        public HrtMateria(MateriaCategory cat, byte lvl) : base(0) => (Category, MateriaLevel) = (cat, lvl);


        public int GetStat() => Materia?.Value[MateriaLevel] ?? 0;
    }
    /// <summary>
    /// Models an item that can be exchanged for another item
    /// Items with FilterGroup 16??
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ExchangableItem : HrtItem
    {
        public List<GearItem> PossiblePurchases =>
            CuratedData.ExchangedFor.GetValueOrDefault(_ID)?.ToList().ConvertAll(id => new GearItem(id))
            ?? new();

        public ExchangableItem(uint id) : base(id) { }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ContainerItem : HrtItem
    {
        public List<GearItem> PossiblePurchases =>
            CuratedData.ItemContainerDB.GetValueOrDefault(_ID)?.ToList().ConvertAll(id => new GearItem(id))
            ?? new();

        public ContainerItem(uint id) : base(id) { }
    }
    public class ItemIDRange : ItemIDCollection
    {
        public static implicit operator ItemIDRange(uint id) => new(id, id);
        public static implicit operator ItemIDRange((uint, uint) id) => new(id.Item1, id.Item2);
        public ItemIDRange(uint start, uint end) : base(Enumerable.Range((int)start, Math.Max(0, (int)end - (int)start + 1)).ToList().ConvertAll(x => (uint)x)) { }
    }
    public class ItemIDList : ItemIDCollection
    {
        public static implicit operator ItemIDList(uint[] ids) => new(ids);

        public ItemIDList(params uint[] ids) : base(ids) { }
    }
    public abstract class ItemIDCollection : IEnumerable<uint>
    {
        private readonly ReadOnlyCollection<uint> _IDs;
        protected ItemIDCollection(IEnumerable<uint> ids) => _IDs = new(ids.ToList());
        public IEnumerator<uint> GetEnumerator() => _IDs.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _IDs.GetEnumerator();
    }

}
