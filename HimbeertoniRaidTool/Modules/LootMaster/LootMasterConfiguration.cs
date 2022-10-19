﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Utility;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    internal class LootMasterConfiguration : HRTConfiguration<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
    {
        public override ConfigUi? Ui => _ui;
        private readonly ConfigUi _ui;
        private bool FullyLoaded = false;
        private const int TargetVersion = 1;
        public LootMasterConfiguration(LootMasterModule hrtModule) : base(hrtModule.InternalName, hrtModule.Name)
        {
            _ui = new(this);
        }
        public override void AfterLoad()
        {
            if (FullyLoaded)
                return;
            if (Data.Version > TargetVersion)
            {
                string msg = "Tried loading a configuration from a newer version of the plugin." +
                    "\nTo prevent data loss operation has been stopped.\nYou need to update to use this plugin!";
                PluginLog.LogFatal(msg);
                Services.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
                throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
            }
            if (Data.Version != TargetVersion)
                Upgrade();
            //Make sure this is the same object as the one in CuratedData
            try
            {
                if (Data.RaidTierOverride is not null)
                    Data.RaidTierOverride = CuratedData.RaidTiers[Array.IndexOf(CuratedData.RaidTiers, Data.RaidTierOverride)];
            }
            catch (Exception) { };
            FullyLoaded = true;
        }
        //Still first version no upgrade possible
        private void Upgrade() { }
        internal sealed class ConfigUi : IHrtConfigUi
        {
            private readonly LootMasterConfiguration _config;
            private ConfigData _dataCopy;
            private UiSortableList<LootRule> LootList;
            private static float ScaleFactor => Window.ScaleFactor;

            internal ConfigUi(LootMasterConfiguration config)
            {
                _config = config;
                _dataCopy = config.Data.Clone();
                LootList = new(LootRuling.PossibleRules, _dataCopy.LootRuling.RuleSet.Cast<LootRule>());
            }

            public void Cancel()
            {

            }

            public void Draw()
            {
                ImGui.BeginTabBar("LootMaster");
                if (ImGui.BeginTabItem(Localize("Appearance", "Appearance")))
                {
                    ImGui.Checkbox(Localize("Lootmaster:OpenOnLogin", "Open group overview on login"),
                        ref _dataCopy.OpenOnStartup);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("BiS"))
                {
                    ImGui.Checkbox(Localize("UpdateBisONStartUp", "Update sets from etro.gg periodically"), ref _dataCopy.UpdateEtroBisOnStartup);
                    ImGui.SetNextItemWidth(150f * ScaleFactor);
                    if (ImGui.InputInt(Localize("BisUpdateInterval", "Update interval (days)"), ref _dataCopy.EtroUpdateIntervalDays))
                        if (_dataCopy.EtroUpdateIntervalDays < 1)
                            _dataCopy.EtroUpdateIntervalDays = 1;
                    ImGui.Text(Localize("DefaultBiSHeading", "Default BiS sets (as etro.gg ID)"));
                    ImGui.TextWrapped(Localize("DefaultBiSDisclaimer",
                        "These sets are used when creating a new characer or adding a new job. These do not affect already created characters and jobs."));
                    if (ImGui.BeginChildFrame(1, new Vector2(400 * ScaleFactor, 400 * ScaleFactor), ImGuiWindowFlags.NoResize))
                    {
                        var jobs = Enum.GetValues<Job>();
                        Array.Sort(jobs, (a, b) =>
                        {
                            bool aFilled = !_dataCopy.GetDefaultBiS(a).IsNullOrEmpty();
                            bool bFilled = !_dataCopy.GetDefaultBiS(b).IsNullOrEmpty();
                            if (aFilled && !bFilled)
                                return -1;
                            if (!aFilled && bFilled)
                                return 1;
                            return a.ToString().CompareTo(b.ToString());

                        });
                        foreach (Job c in jobs)
                        {
                            bool isOverriden = _dataCopy.BISUserOverride.ContainsKey(c);
                            string value = _dataCopy.GetDefaultBiS(c);
                            if (ImGui.InputText(c.ToString(), ref value, 100))
                            {
                                if (value != CuratedData.DefaultBIS[c])
                                {
                                    if (isOverriden)
                                        _dataCopy.BISUserOverride[c] = value;
                                    else
                                        _dataCopy.BISUserOverride.Add(c, value);
                                }
                                else
                                {
                                    if (isOverriden)
                                        _dataCopy.BISUserOverride.Remove(c);
                                }

                            }
                            if (isOverriden)
                            {
                                ImGui.SameLine();
                                if (ImGuiHelper.Button(Dalamud.Interface.FontAwesomeIcon.Undo,
                                    $"Reset{c}", Localize("Reset to default", "Reset to default")))
                                    _dataCopy.BISUserOverride.Remove(c);
                            }
                            else
                            {
                                ImGui.SameLine();
                                ImGui.TextDisabled($"({Localize("default", "default")})");
                            }
                        }
                        ImGui.EndChildFrame();
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Loot"))
                {
                    ImGui.Text(Localize("LootRuleOrder", "Order in which loot rules should be applied"));
                    LootList.Draw();
                    ImGui.Separator();
                    ImGui.Text(Localize("ConfigRolePriority", "Priority to loot for each role (smaller is higher priority)"));
                    ImGui.Text($"{Localize("Current priority", "Current priority")}: {_dataCopy.RolePriority}");
                    _dataCopy.RolePriority.DrawEdit();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            public void OnHide() { }

            public void OnShow()
            {
                _dataCopy = _config.Data.Clone();
                LootList = new(LootRuling.PossibleRules, _dataCopy.LootRuling.RuleSet);
            }

            public void Save()
            {
                _dataCopy.LootRuling.RuleSet = LootList.List;
                _config.Data = _dataCopy;
            }
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
        internal sealed class ConfigData
        {
            public int Version { get; set; } = 1;
            [JsonProperty("UserBiS")]
            public Dictionary<Job, string> BISUserOverride = new();
            [JsonProperty]
            public bool UpdateEtroBisOnStartup = false;
            [JsonProperty]
            public int EtroUpdateIntervalDays = 14;
            [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public LootRuling LootRuling = new()
            {
                RuleSet = new List<LootRule>()
                        {
                            new(LootRuleEnum.BISOverUpgrade),
                            new(LootRuleEnum.RolePrio),
                            new(LootRuleEnum.HighesItemLevelGain),
                            new(LootRuleEnum.LowestItemLevel),
                            new(LootRuleEnum.Random)
                        }
            };
            [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RolePriority RolePriority = new()
            {
                { Role.Melee,   0 },
                { Role.Caster,  1 },
                { Role.Ranged,  1 },
                { Role.Tank,    3 },
                { Role.Healer,  4 },
            };
            [JsonProperty]
            public bool OpenOnStartup = false;
            [JsonProperty]
            public int LastGroupIndex = 0;
            [JsonProperty("RaidTier")]
            public RaidTier? RaidTierOverride = null;
            [JsonIgnore]
            public RaidTier SelectedRaidTier => RaidTierOverride ?? CuratedData.CurrentRaidSavage;
            public string GetDefaultBiS(Job c) => BISUserOverride.ContainsKey(c) ? BISUserOverride[c] : CuratedData.DefaultBIS.ContainsKey(c) ? CuratedData.DefaultBIS[c] : "";
        }
    }

}
