﻿using System.Collections.Generic;
using static Dalamud.Localization;
using static HimbeertoniRaidTool.Data.Job;

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

        public static readonly Dictionary<ItemIDRange, LootSource> LootSourceDB = new()
        {
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
        public static readonly Dictionary<ItemIDRange, ItemIDCollection> ItemContainerDB = new()
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
        public static readonly Dictionary<uint, ItemIDRange> ExchangedFor = new()
        {
            //6.0
            { 35828, (35170, 35188) },//Radiant Robortant
            { 35829, (35190, 35224) },//Radiant Twine
            { 35830, (35225, 35244) },//Radiatn Coating
            { 35831, (35095, 35113) },//Discal TomeStone
            //6.1
            { 36820, (35828, 35830) },//Aglaia Coin
            //6.2
            { 38386, (38006, 38024) },//Moonshine Brine
            { 38387, (38026, 38060) },//Moonshine Twine
            { 38388, (38061, 38080) },//Moonshine Shine
            { 38389, (37856, 37874) },//Ultralight TomeStone
        };
        public static readonly KeyContainsDictionary<GearSource> GearSourceDictionary = new()
        {
            //6.0x
            { "Asphodelos", GearSource.Raid },
            { "Radiant", GearSource.Tome },
            { "Classical", GearSource.Crafted },
            { "Limbo", GearSource.Raid },
            { "Last", GearSource.Dungeon },
            { "Eternal Dark", GearSource.Trial },
            { "Moonward", GearSource.Tome },
            { "Divine Light", GearSource.Trial },
            //6.1
            { "Panthean", GearSource.AllianceRaid },
            { "Bluefeather", GearSource.Trial },
            //6.2
            { "Purgatory", GearSource.Raid },
            { "Abyssos", GearSource.Raid },
            { "Rinascita", GearSource.Crafted },
            { "Lunar Envoy", GearSource.Tome },
            { "Windswept", GearSource.Trial },
            { "Troian", GearSource.Dungeon },

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
    }
}
