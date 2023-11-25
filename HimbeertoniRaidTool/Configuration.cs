﻿using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Interface.Windowing;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin;

public class Configuration : IPluginConfiguration, IDisposable
{
    private bool _fullyLoaded = false;
    private readonly int _targetVersion = 5;
    public int Version { get; set; } = 5;
    private readonly Dictionary<Type, IHrtConfiguration> _configurations = new();
    private readonly ConfigUi _ui;

    public Configuration()
    {
        _ui = new ConfigUi(this);
    }

    internal void AfterLoad()
    {
        if (_fullyLoaded)
            return;
        if (Version < 5)
            Version = 5;
        _fullyLoaded = true;
    }

    internal void Show()
    {
        _ui.Show();
    }

    internal bool RegisterConfig(IHrtConfiguration config)
    {
        if (_configurations.ContainsKey(config.GetType()))
            return false;
        _configurations.Add(config.GetType(), config);
        return config.Load(ServiceManager.HrtDataManager.ModuleConfigurationManager);
    }

    internal void Save(bool saveAll = true)
    {
        if (Version == _targetVersion)
        {
            ServiceManager.PluginInterface.SavePluginConfig(this);
            if (!saveAll)
                return;
            foreach (IHrtConfiguration? config in _configurations.Values)
                config.Save(ServiceManager.HrtDataManager.ModuleConfigurationManager);
        }
        else
        {
            ServiceManager.PluginLog.Error("Configuration Version mismatch. Did not Save!");
        }
    }

    public void Dispose()
    {
        _ui.Dispose();
    }

    public class ConfigUi : HrtWindow, IDisposable
    {
        private readonly WindowSystem _windowSystem;
        private readonly Configuration _configuration;

        public ConfigUi(Configuration configuration) : base("HimbeerToniRaidToolConfiguration")
        {
            _windowSystem = new WindowSystem("HRTConfig");
            _windowSystem.AddWindow(this);
            _configuration = configuration;
            ServiceManager.PluginInterface.UiBuilder.OpenConfigUi += Show;
            ServiceManager.PluginInterface.UiBuilder.Draw += _windowSystem.Draw;

            (Size, SizeCondition) = (new Vector2(450, 500), ImGuiCond.Appearing);
            Flags = ImGuiWindowFlags.NoCollapse;
            Title = Localize("ConfigWindowTitle", "HimbeerToni Raid Tool Configuration");
            IsOpen = false;
        }

        public void Dispose()
        {
            ServiceManager.PluginInterface.UiBuilder.OpenConfigUi -= Show;
            ServiceManager.PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        }

        public override void OnOpen()
        {
            foreach (IHrtConfiguration config in _configuration._configurations.Values)
                config.Ui?.OnShow();
        }

        public override void OnClose()
        {
            foreach (IHrtConfiguration config in _configuration._configurations.Values)
                config.Ui?.OnHide();
        }

        public override void Draw()
        {
            if (ImGuiHelper.SaveButton())
                Save();
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
                Cancel();
            ImGui.BeginTabBar("Modules");
            foreach (IHrtConfiguration c in _configuration._configurations.Values)
                try
                {
                    if (c.Ui == null)
                        continue;
                    if (ImGui.BeginTabItem(c.ParentName))
                    {
                        c.Ui.Draw();
                        ImGui.EndTabItem();
                    }
                }
                catch (Exception)
                {
                }

            ImGui.EndTabBar();
        }

        private void Save()
        {
            foreach (IHrtConfiguration c in _configuration._configurations.Values)
                c.Ui?.Save();
            _configuration.Save();
            Hide();
        }

        private void Cancel()
        {
            foreach (IHrtConfiguration c in _configuration._configurations.Values)
                c.Ui?.Cancel();
            Hide();
        }
    }
}

public interface IHrtConfiguration
{
    public string ParentInternalName { get; }
    public string ParentName { get; }
    public IHrtConfigUi? Ui { get; }
    internal bool Load(IModuleConfigurationManager configManager);
    internal bool Save(IModuleConfigurationManager configManager);
    public void AfterLoad();
}

internal abstract class HrtConfiguration<T> : IHrtConfiguration where T : IHrtConfigData, new()
{
    public string ParentInternalName { get; }
    public string ParentName { get; }
    public T Data = new();
    public abstract IHrtConfigUi? Ui { get; }

    protected HrtConfiguration(string parentInternalName, string parentName)
    {
        ParentInternalName = parentInternalName;
        ParentName = parentName;
    }

    public bool Load(IModuleConfigurationManager configManager) => configManager.LoadConfiguration(ParentInternalName, ref Data);

    public bool Save(IModuleConfigurationManager configManager) => configManager.SaveConfiguration(ParentInternalName, Data);

    public abstract void AfterLoad();
}

public interface IHrtConfigUi
{
    public void OnShow();
    public void Draw();
    public void OnHide();
    public void Save();
    public void Cancel();
}

public interface IHrtConfigData
{
    public void AfterLoad();
    public void BeforeSave();
}