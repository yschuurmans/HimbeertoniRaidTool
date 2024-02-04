﻿using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal sealed class CoreConfig : HrtConfiguration<CoreConfig.ConfigData>
{
    private const int TARGET_VERSION = 1;
    private readonly PeriodicTask _saveTask;
    public CoreConfig(CoreModule module) : base(module.InternalName, CoreLoc.ConfigUi_Title)
    {
        Ui = new ConfigUi(this);
        _saveTask = new PeriodicTask(PeriodicSave, module.HandleMessage, "Automatic Save",
                                     TimeSpan.FromMinutes(Data.SaveIntervalMinutes))
        {
            ShouldRun = false,
        };
    }
    public override ConfigUi Ui { get; }

    public override void AfterLoad()
    {
        if (Data.Version > TARGET_VERSION)
        {
            string msg = GeneralLoc.Config_Error_Downgrade;
            ServiceManager.PluginLog.Fatal(msg);
            ServiceManager.Chat.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
        }
        Upgrade();
        _saveTask.Repeat = TimeSpan.FromMinutes(Data.SaveIntervalMinutes);
        _saveTask.ShouldRun = Data.SavePeriodically;
        _saveTask.LastRun = DateTime.Now;
        ServiceManager.TaskManager.RegisterTask(_saveTask);
    }

    private void Upgrade()
    {
        while (Data.Version < TARGET_VERSION)
        {
            int oldVersion = Data.Version;
            DoUpgradeStep();
            if (Data.Version > oldVersion)
                continue;
            string msg = string.Format(CoreLoc.Chat_configUpgradeError, oldVersion);
            ServiceManager.PluginLog.Fatal(msg);
            ServiceManager.Chat.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new InvalidOperationException(msg);


        }
    }

    private void DoUpgradeStep() { }

    private static HrtUiMessage PeriodicSave()
    {
        if (ServiceManager.HrtDataManager.Save())
            return new HrtUiMessage(CoreLoc.UiMessage_PeriodicSaveSuccessful,
                                    HrtUiMessageType.Success);
        return new HrtUiMessage(CoreLoc.UiMessage_PeriodicSaveFailed,
                                HrtUiMessageType.Failure);
    }

    internal sealed class ConfigData : IHrtConfigData
    {
        [JsonProperty] public ChangelogShowOptions ChangelogNotificationOptions = ChangelogShowOptions.ShowAll;
        [JsonProperty] public int EtroUpdateIntervalDays = 7;
        [JsonProperty] public bool HideInCombat = true;
        /*
         * ChangeLog
         */
        [JsonProperty] public Version LastSeenChangelog = new(0, 0, 0, 0);
        [JsonProperty] public int SaveIntervalMinutes = 30;
        [JsonProperty] public bool SavePeriodically = true;
        [JsonProperty] public bool ShowWelcomeWindow = true;
        [JsonProperty] public bool UpdateCombatJobs = true;
        [JsonProperty] public bool UpdateDoHJobs;
        [JsonProperty] public bool UpdateDoLJobs;
        /**
         * BiS
         */
        [JsonProperty] public bool UpdateEtroBisOnStartup = true;
        [JsonProperty] public bool UpdateGearOnExamine = true;
        /*
         * Data providers
         */
        [JsonProperty] public bool UpdateOwnData = true;
        [JsonProperty] public int Version = 1;


        public void AfterLoad() { }

        public void BeforeSave() { }
    }

    internal class ConfigUi : IHrtConfigUi
    {
        private readonly CoreConfig _parent;
        private ConfigData _dataCopy;

        public ConfigUi(CoreConfig parent)
        {
            _parent = parent;
            _dataCopy = parent.Data.Clone();
        }

        public void Cancel()
        {
        }

        public void Draw()
        {
            ImGui.Text(CoreLoc.ConfigUi_hdg_dataUpdate);
            ImGui.Checkbox(CoreLoc.ConfigUi_cb_ownData, ref _dataCopy.UpdateOwnData);
            ImGuiHelper.AddTooltip(CoreLoc.ConfigUi_cb_tt_ownData);
            ImGui.Checkbox(CoreLoc.ConfigUi_cb_examine, ref _dataCopy.UpdateGearOnExamine);
            ImGui.BeginDisabled(_dataCopy is { UpdateOwnData: false, UpdateGearOnExamine: false });
            ImGui.Text(CoreLoc.ConfigUi_text_dataUpdateJobs);
            ImGui.Indent(25);
            ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateCombatJobs, ref _dataCopy.UpdateCombatJobs);
            ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateDohJobs, ref _dataCopy.UpdateDoHJobs);
            ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateDolJobs, ref _dataCopy.UpdateDoLJobs);
            ImGui.Indent(-25);
            ImGui.EndDisabled();
            ImGui.Separator();
            ImGui.Text(CoreLoc.ConfigUi_hdg_ui);
            ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_hideInCombat, ref _dataCopy.HideInCombat,
                                 CoreLoc.ConfigUi_cb_tt_hideInCombat);
            ImGui.Separator();
            ImGui.Text(CoreLoc.ConfigUi_hdg_AutoSave);
            ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_periodicSave, ref _dataCopy.SavePeriodically,
                                 CoreLoc.ConfigUi_cb_tt_periodicSave);
            ImGui.BeginDisabled(!_dataCopy.SavePeriodically);
            ImGui.TextWrapped($"{CoreLoc.ConfigUi_in_autoSaveInterval}:");
            ImGui.SetNextItemWidth(150 * HrtWindow.ScaleFactor);
            if (ImGui.InputInt("##AutoSave_interval_min", ref _dataCopy.SaveIntervalMinutes))
                if (_dataCopy.SaveIntervalMinutes < 1)
                    _dataCopy.SaveIntervalMinutes = 1;
            ImGui.EndDisabled();
            ImGui.Separator();
            ImGui.Text(CoreLoc.ConfigUi_hdg_changelog);
            ImGuiHelper.Combo("##showChangelog", ref _dataCopy.ChangelogNotificationOptions,
                              t => t.LocalizedDescription());
            ImGui.Separator();
            ImGui.Text(CoreLoc.ConfigUi_hdg_etroUpdates);
            ImGui.Checkbox(CoreLoc.ConfigUi_cb_autoEtroUpdate, ref _dataCopy.UpdateEtroBisOnStartup);
            ImGui.BeginDisabled(!_dataCopy.UpdateEtroBisOnStartup);
            ImGui.SetNextItemWidth(150f * HrtWindow.ScaleFactor);
            if (ImGui.InputInt(CoreLoc.ConfigUi_in_etroUpdateInterval, ref _dataCopy.EtroUpdateIntervalDays))
                if (_dataCopy.EtroUpdateIntervalDays < 1)
                    _dataCopy.EtroUpdateIntervalDays = 1;
            ImGui.EndDisabled();
        }

        public void OnHide()
        {
        }

        public void OnShow() => _dataCopy = _parent.Data.Clone();

        public void Save()
        {
            if (_dataCopy.SaveIntervalMinutes != _parent.Data.SaveIntervalMinutes)
                _parent._saveTask.Repeat = TimeSpan.FromMinutes(_dataCopy.SaveIntervalMinutes);
            if (_dataCopy.SavePeriodically != _parent.Data.SavePeriodically)
                _parent._saveTask.ShouldRun = _dataCopy.SavePeriodically;
            _parent.Data = _dataCopy;
        }
    }
}