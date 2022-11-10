﻿using System.Collections.Generic;
using Dalamud.Logging;
using Lumina.Excel;
using Lumina.Excel.CustomSheets;
using static HimbeertoniRaidTool.Data.Job;
using static HimbeertoniRaidTool.HrtServices.Localization;

namespace HimbeertoniRaidTool.Data
{
    /// <summary>
    /// This class/file encapsulates all data that needs to be update for new Patches of the game.
    /// It is intended to be servicable by someone not familiar with programming.
    /// First there are entries for each RaidTier. To make a new one make it in the following way:
    /// 'public static RaidTier' DescriptiveName(no spaces) ' => 
    ///     new('Expansion number, Raid number (1-3), EncounterDifficulty, ILvl of Weapons, iLvl of other Gear, "A descriptive name", the current max materia tier);
    /// Then there is a list which links loot  to boss encounter
    ///     Just add entries as a new line of format:
    ///     { ItemID(S), (RaidTier (Named like above), Encounter Number(1-4)) },
    ///AFter that information on which Slot Item Coffers belong to
    ///     Just add entries as a new line of format:
    ///     { ItemID, GearSlot.Name },
    /// </summary>
    internal static class CuratedData
    {
        private static readonly ExcelSheet<CustomSpecialShop> ShopSheet = Services.DataManager.GetExcelSheet<CustomSpecialShop>()!;
        static CuratedData()
        {
            ShopIndex = new();
            UsedToBuy = new();
            foreach (uint shopID in RelevantShops)
            {
                CustomSpecialShop? shop = ShopSheet.GetRow(shopID);
                if (shop == null)
                    continue;
                PluginLog.Debug(shop.Name);
                for (int idx = 0; idx < shop.ShopEntries.Length; idx++)
                {
                    var entry = shop.ShopEntries[idx];
                    //Cannot handle dual output
                    if (entry.ItemReceiveEntries[1].Item.Row != 0)
                        continue;
                    ShopIndex[entry.ItemReceiveEntries[0].Item.Row] = (shopID, idx);
                    foreach (var item in entry.ItemCostEntries)
                    {
                        if (!UsedToBuy.ContainsKey(item.Item.Row))
                            UsedToBuy.Add(item.Item.Row, new());
                        UsedToBuy[item.Item.Row].Add(entry.ItemReceiveEntries[0].Item.Row);
                    }
                }
            }
        }
        public static readonly int MaxLevel = 90;
        public static RaidTier CurrentRaidSavage => AbyssosSavage;
        public static RaidTier AbyssosNormal = new(6, 2, EncounterDifficulty.Normal, 620, 610, "Abyssos", 10);
        public static RaidTier AbyssosSavage = new(6, 2, EncounterDifficulty.Savage, 635, 630, "Abyssos " + Localize("Savage", "Savage"), 10);
        public static RaidTier AsphodelosNormal = new(6, 1, EncounterDifficulty.Normal, 590, 580, "Asphodelos", 10);
        public static RaidTier AsphodelosSavage = new(6, 1, EncounterDifficulty.Savage, 605, 600, "Asphodelos " + Localize("Savage", "Savage"), 10);
        public static RaidTier DragonsongRepriseUltimate = new(6, 1, EncounterDifficulty.Ultimate, 605, 0, $"{Localize("Dragonsong Reprise", "Dragonsong Reprise")} {Localize("Ultimate", "Ultimate")}", 10);

        public static RaidTier[] RaidTiers = new RaidTier[]
        {
            AsphodelosSavage,
            AbyssosSavage
        };
        public static readonly Dictionary<ItemIDRange, LootSource> GuaranteedLootSourceDB = new()
        {
            //Only books here atm
            //6.0
            {35823 , (AsphodelosSavage, 1)},
            {35824 , (AsphodelosSavage, 2)},
            {35825 , (AsphodelosSavage, 3)},
            {35826 , (AsphodelosSavage, 4)},
            //6.2
            {38381 , (AbyssosSavage, 1)},
            {38382 , (AbyssosSavage, 2)},
            {38383 , (AbyssosSavage, 3)},
            {38384 , (AbyssosSavage, 4)},
        };

        public static readonly Dictionary<ItemIDRange, LootSource> PossibleLootSourceDB = new()
        {
            //6.0
            { (35245, 35264), (AsphodelosSavage, 4) },//All Asphodelos Weapons
            { 35734, (AsphodelosSavage, 4) },//Asphodelos weapon coffer
            { 35735, new((AsphodelosSavage, 2), (AsphodelosSavage, 3)) },//Asphodelos head gear coffer
            { 35736, (AsphodelosSavage, 4) },//Asphodelos chest gear coffer
            { 35737, new((AsphodelosSavage, 2), (AsphodelosSavage, 3)) },//Asphodelos hand gear coffer
            { 35738, (AsphodelosSavage, 3) },//Asphodelos leg gear coffer
            { 35739, new((AsphodelosSavage, 2), (AsphodelosSavage, 3)) },//Asphodelos foot gear coffer
            { 35740, (AsphodelosSavage, 1) },//Asphodelos earring coffer
            { 35741, (AsphodelosSavage, 1) },//Asphodelos necklace coffer
            { 35742, (AsphodelosSavage, 1) },//Asphodelos bracelet coffer
            { 35743, (AsphodelosSavage, 1) },//Asphodelos ring coffers
            { 35828, (AsphodelosSavage, 3) },//Radiant Roborant
            { 35829, (AsphodelosSavage, 3) },//Radiant Twine
            { 35830, (AsphodelosSavage, 2) },//Radiant Coating
            { 35831, (AsphodelosSavage, 2) }, //Discal Tomestone
            //6.2
            { (38081, 38099), (AbyssosSavage, 4) },//All Abyssos Weapons
            { 38390, (AbyssosSavage, 4) },//Abyssos weapon coffer
            { 38391, new((AbyssosSavage, 2), (AbyssosSavage, 3)) },//Abyssos head gear coffer
            { 38392, (AbyssosSavage, 4) },//Abyssos chest gear coffer
            { 38393, new((AbyssosSavage, 2), (AbyssosSavage, 3)) },//Abyssos hand gear coffer
            { 38394, (AbyssosSavage, 3) },//Abyssos leg gear coffer
            { 38395, new((AbyssosSavage, 2), (AbyssosSavage, 3)) },//Abyssos foot gear coffer
            { 38396, (AbyssosSavage, 1) },//Abyssos earring coffer
            { 38397, (AbyssosSavage, 1) },//Abyssos necklace coffer
            { 38398, (AbyssosSavage, 1) },//Abyssos bracelet coffer
            { 38399, (AbyssosSavage, 1) },//Abyssos ring coffers
            { 38386, (AbyssosSavage, 3) },//Moonshine Brine
            { 38387, (AbyssosSavage, 3) },//Moonshine Twine
            { 38388, (AbyssosSavage, 2) },//Moonshine Shine
            { 38389, (AbyssosSavage, 2) }, //Ultralight Tomestone


        };
        public static readonly Dictionary<uint, ItemIDCollection> ItemContainerDB = new()
        {
            //6.0
            { 35734, new ItemIDRange(35245, 35264) },//Asphodelos weapon coffer
            { 35735, new ItemIDList(35265, 35270, 35275, 35280, 35285, 35290, 35295) },//Asphodelos head gear coffer
            { 35736, new ItemIDList(35266, 35271, 35276, 35281, 35286, 35291, 35296) },//Asphodelos chest gear coffer
            { 35737, new ItemIDList(35267, 35272, 35277, 35282, 35287, 35292, 35297) },//Asphodelos hand gear coffer
            { 35738, new ItemIDList(35268, 35273, 35278, 35283, 35288, 35293, 35298) },//Asphodelos leg gear coffer
            { 35739, new ItemIDList(35269, 35274, 35279, 35284, 35289, 35294, 35299) },//Asphodelos foot gear coffer
            { 35740, new ItemIDRange(35300, 35304) },//Asphodelos earring coffer
            { 35741, new ItemIDRange(35305, 35309) },//Asphodelos necklace coffer
            { 35742, new ItemIDRange(35310, 35314) },//Asphodelos bracelet coffer
            { 35743, new ItemIDRange(35315, 35319) },//Asphodelos ring coffers
            //6.2
            { 38390, new ItemIDRange(38081, 38099) },//Abyssos weapon coffer
            { 38391, new ItemIDList(38101, 38106, 38111, 38116, 38121, 38126, 38131) },//Abyssos head gear coffer
            { 38392, new ItemIDList(38102, 38107, 38112, 38117, 38122, 38127, 38132) },//Abyssos chest gear coffer
            { 38393, new ItemIDList(38103, 38108, 38113, 38118, 38123, 38128, 38133) },//Abyssos hand gear coffer
            { 38394, new ItemIDList(38104, 38109, 38114, 38119, 38124, 38129, 38134) },//Abyssos leg gear coffer
            { 38395, new ItemIDList(38105, 38110, 38115, 38120, 38125, 38130, 38135) },//Abyssos foot gear coffer
            { 38396, new ItemIDRange(38136, 38140) },//Abyssos earring coffer
            { 38397, new ItemIDRange(38141, 38145) },//Abyssos necklace coffer
            { 38398, new ItemIDRange(38146, 38150) },//Abyssos bracelet coffer
            { 38399, new ItemIDRange(38151, 38155) },//Abyssos ring coffers
        };
        public static readonly Dictionary<uint, GearSource> GearSourceDB = new Dictionary<ItemIDCollection, GearSource>()
        {
            { new ItemIDRange(34810,34829), GearSource.Dungeon }, //Etheirys  (The Aitiascope)
            { new ItemIDRange(34830,34849), GearSource.Dungeon }, //The last (Dead Ends)
            { new ItemIDRange(34850,34924), GearSource.Tome }, //Moonward Tomestone
            { new ItemIDRange(34925,34944), GearSource.Trial }, //Divine Light
            { new ItemIDRange(34945,34964), GearSource.Trial }, //Eternal Dark
            { new ItemIDRange(34965,35019), GearSource.Raid }, //Asphodelos
            { new ItemIDRange(35020,35094), GearSource.Crafted }, //Classical
            { new ItemIDRange(35095,35169), GearSource.Tome }, //Radiant Tomestone
            { new ItemIDRange(35170,35244), GearSource.Tome }, //Aug Radiant Tomestone
            { new ItemIDRange(35245,35319), GearSource.Raid }, //Asphodelos Savage
            { new ItemIDRange(35320,35340), GearSource.undefined }, //High Durium
            { new ItemIDRange(35341,35361), GearSource.undefined }, //Bismuth
            { new ItemIDRange(35362,35382), GearSource.undefined }, //Mangaganese
            { new ItemIDRange(35383,35403), GearSource.undefined }, //Chondrite
            //missing items
            { new ItemIDRange(36718,36792), GearSource.Crafted }, //Augm Classical
            //missing items
            { new ItemIDRange(36923,36942), GearSource.Trial }, //Bluefeather
            //missing items
            { new ItemIDRange(37131,37165), GearSource.AllianceRaid }, //Panthean
            { new ItemIDRange(37166,37227), GearSource.Dungeon }, //Darbar (alzadaals Legacy)
            //missing items
            { new ItemIDRange(37742,37816), GearSource.Crafted }, //Rinascita
            //missing items
            { new ItemIDRange(37856,37875), GearSource.Trial }, //Windswept
            { new ItemIDRange(37876,37930), GearSource.Raid }, //Abyssos
            { new ItemIDRange(37931,38005), GearSource.Tome }, //Lunar Envoy Tomestone
            { new ItemIDRange(38006,38080), GearSource.Tome }, //Aug Lunar Envoy Tomestone
            { new ItemIDRange(38081,38155), GearSource.Raid }, //Abyssos savage
            { new ItemIDRange(38156,38210), GearSource.Dungeon }, //Troian
            //missing items
            { new ItemIDRange(38400,38419), GearSource.Relic }, //Manderville

        }.ExplodeIDCollection();
        public static readonly Dictionary<uint, (uint shopID, int idx)> ShopIndex;
        public static readonly Dictionary<uint, List<uint>> UsedToBuy;
        public static CustomSpecialShop.ShopEntry? GetShopEntry(uint id) => ShopSheet.GetRow(ShopIndex[id].shopID)?.ShopEntries[ShopIndex[id].idx];
        public static readonly HashSet<uint> RelevantShops = new()
        {
            //6.0
            1770437, //Allagan Tomestones of Astronomy (DoW)
            1770438, //Allagan Tomestones of Astronomy (DoM)
            1770443, //Asphodelos Mythos Exchange (DoW) I
            1770444, //Asphodelos Mythos Exchange (DoW) II
            1770445, //Asphodelos Mythos Exchange (DoM)
            1770446, //Allagan Tomestones of Astronomy Exchange (Weapons)
            1770447, //Radiant's Gear Augmentation (DoW) I
            1770448, //Radiant's Gear Augmentation (DoW) II
            1770449, //Radiant's Gear Augmentation (DoM)
            //6.1
            1770456, //Out-of-this-world Oddities
            //6.2
            1770606, //Allagan Tomestones of Causality (DoW)
            1770607, //Allagan Tomestones of Causality (DoM)
            1770608, //Unsung Relic of Abyssos Exchange (DoW) I
            1770609, //Unsung Relic of Abyssos Exchange (DoW) II
            1770610, //Unsung Relic of Abyssos Exchange (DoM)
            1770611, //Abyssos Mythos Exchange (DoW) I
            1770612, //Abyssos Mythos Exchange (DoW) II
            1770613, //Abyssos Mythos Exchange (DoM)
            1770614, //Allagan Tomestones of Causality Exchange (Weapons)
            1770615, //Aug Lunar Envoy DoW I
            1770616, //Aug Lunar Envoy DoW II
            1770617, //Aug Lunar Envoy DoM
            1770618, //Totem Gear (Barbariccia)
        };
        /// <summary>
        /// Holds a list of Etro IDs to use as BiS sets if users did not enter a preferred BiS
        /// </summary>
        public static Dictionary<Job, string> DefaultBIS { get; set; } = new Dictionary<Job, string>
        {
            { AST, "a2201358-04ad-4b07-81e4-003a514f0694" },
            { BLM, "bd1b7a52-5893-4928-9d7c-d47aea22d8d2" },
            { BRD, "2a242f9b-8a41-4d09-9e14-3c8fb08e97e4" },
            { DNC, "fb5976d5-a94c-4052-9092-3c3990fefa76" },
            { DRG, "de153cb0-05e7-4f23-a924-1fc28c7ae8db" },
            { DRK, "9467c373-ba77-4f20-aa76-06c8e6f926b8" },
            { GNB, "1cdcf24b-af97-4d6b-ab88-dcfee79f791c" },
            { MCH, "8a0bdf80-80f5-42e8-b10a-160b0fc2d151" },
            { MNK, "12aff29c-8420-4c28-a3c4-68d03ac5afa3" },
            { NIN, "c0c2ba50-b93a-4d18-8cba-a0ebb0705fed" },
            { PLD, "86b4625f-d8ef-4bb1-92b1-cef8bcce7390" },
            { RDM, "5f972eb8-c3cd-44da-aa73-0fa769957e5b" },
            { RPR, "c293f73b-5c58-4855-b43d-aae55b212611" },
            { SAM, "4356046d-2f05-432a-a98c-632f11098ade" },
            { SCH, "41c65b56-fa08-4c6a-b86b-627fd14d04ff" },
            { SGE, "80bec2f5-8e9e-43fb-adcf-0cd7f7018c02" },
            { SMN, "b3567b2d-5c92-4ba1-a18a-eb91b614e944" },
            { WAR, "f3f765a3-56a5-446e-b1e1-1c7cdd23f24b" },
            { WHM, "da9ef350-7568-4c98-8ecc-959040d9ba3a" },
        };
    }
    public static class CuratedDataExtension
    {
        public static Dictionary<uint, T> ExplodeIDCollection<T>(this Dictionary<ItemIDCollection, T> source)
        {
            Dictionary<uint, T> result = new();
            foreach ((ItemIDCollection ids, T val) in source)
                foreach (uint id in ids)
                    result.Add(id, val);
            return result;
        }
    }
}
