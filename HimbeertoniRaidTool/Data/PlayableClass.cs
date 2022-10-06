﻿using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PlayableClass
    {
        private static readonly ExcelSheet<ClassJob> _classJobSheet = Services.DataManager.GetExcelSheet<ClassJob>()!;
        [JsonProperty("Job")]
        public Job Job;
        [JsonIgnore]
        public ClassJob ClassJob => _classJobSheet.GetRow((uint)Job)!;
        public Character? Parent { get; private set; }
        [JsonProperty("Level")]
        public int Level = 1;
        [JsonProperty("Gear")]
        public GearSet Gear;
        [JsonProperty("BIS")]
        public GearSet BIS;
        [JsonConstructor]
        private PlayableClass()
        {
            Gear = new GearSet();
            BIS = new GearSet();
        }
        public PlayableClass(Job job, Character c)
        {
            Job = job;
            Gear = new(GearSetManager.HRT, c, Job);
            Services.HrtDataManager.GetManagedGearSet(ref Gear);
            BIS = new(GearSetManager.HRT, c, Job, "BIS");
            Services.HrtDataManager.GetManagedGearSet(ref BIS);
        }
        public int GetCurrentStat(StatType type) => GetStat(type, Gear);
        public int GetBiSStat(StatType type) => GetStat(type, BIS);
        private int GetStat(StatType type, GearSet set)
        {
            type = type switch
            {
                StatType.AttackMagicPotency => Job.MainStat(),
                StatType.HealingMagicPotency => StatType.Mind,
                StatType.AttackPower => Job.MainStat(),
                _ => type,
            };
            return AllaganLibrary.GetStatWithModifiers(type, set.GetStat(type), Level, Job, Parent?.Tribe);
        }
        internal void SetParent(Character c)
        {
            string testString = string.Format("{0:X}-{1:X}", c.HomeWorldID, c.Name.ConsistentHash());
            if (Gear.HrtID.StartsWith(testString))
                Parent = c;
        }
        public bool IsEmpty => Level == 0 && Gear.IsEmpty && BIS.IsEmpty;
        public void ManageGear()
        {
            Services.HrtDataManager.GetManagedGearSet(ref Gear);
            Services.HrtDataManager.GetManagedGearSet(ref BIS);
        }
        public bool Equals(PlayableClass? other)
        {
            if (other == null)
                return false;
            return Job == other.Job;
        }
    }
}
