﻿using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules.Core;

#pragma warning disable CS8618
namespace HimbeertoniRaidTool.Plugin.Services;


internal static class ServiceManager
{
    private static bool _initialized;
    public static IChatProvider Chat { get; private set; }
    public static IDataManager DataManager => DalamudServices.DataManager;
    public static ITargetManager TargetManager => DalamudServices.TargetManager;
    public static DalamudPluginInterface PluginInterface => DalamudServices.PluginInterface;
    public static IClientState ClientState => DalamudServices.ClientState;
    public static IObjectTable ObjectTable => DalamudServices.ObjectTable;
    public static IPartyList PartyList => DalamudServices.PartyList;
    public static ICondition Condition => DalamudServices.Condition;
    public static IPluginLog PluginLog { get; private set; }
    public static IconCache IconCache { get; private set; }
    public static HrtDataManager HrtDataManager { get; private set; }
    internal static TaskManager TaskManager { get; private set; }
    internal static ConnectorPool ConnectorPool { get; private set; }
    internal static Configuration Config { get; set; }
    internal static CoreModule CoreModule { get; set; }
    internal static CharacterInfoService CharacterInfoService { get; private set; }
    internal static ItemInfo ItemInfo => Common.Services.ServiceManager.ItemInfo;
    internal static ExamineGearDataProvider ExamineGearDataProvider { get; private set; }
    internal static OwnCharacterDataProvider OwnCharacterDataProvider { get; private set; }

    private static DalamudServiceWrapper DalamudServices { get; set; }

    internal static bool Init(DalamudPluginInterface pluginInterface)
    {
        if (_initialized) return false;
        _initialized = true;
        DalamudServices = pluginInterface.Create<DalamudServiceWrapper>()
                        ?? throw new FailedToLoadException("Could not initialize Service Manager");
        PluginLog = new LoggingProxy(DalamudServices.PluginLog);
        Chat = new DalamudChatProxy(DalamudServices.ChatGui);
        Common.Services.ServiceManager.Init(DataManager.Excel, pluginInterface.UiLanguage);
        IconCache = new IconCache(PluginInterface, DataManager, DalamudServices.TextureProvider);
        HrtDataManager = new HrtDataManager(PluginInterface);
        TaskManager = new TaskManager();
        ConnectorPool = new ConnectorPool(TaskManager, PluginLog);
        CharacterInfoService = new CharacterInfoService(ObjectTable, PartyList);
        ExamineGearDataProvider = new ExamineGearDataProvider(DalamudServices.GameInteropProvider);
        OwnCharacterDataProvider = new OwnCharacterDataProvider(ClientState, DalamudServices.Framework);
        return HrtDataManager.Initialized;
    }

    internal static void Dispose()
    {
        ExamineGearDataProvider.Dispose();
        OwnCharacterDataProvider.Dispose();
        TaskManager.Dispose();
        IconCache.Dispose();
    }
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    private class DalamudServiceWrapper
    {
        [PluginService] public IChatGui ChatGui { get; set; }
        [PluginService] public IDataManager DataManager { get; set; }
        [PluginService] public ITargetManager TargetManager { get; set; }
        [PluginService] public DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public IClientState ClientState { get; set; }
        [PluginService] public IObjectTable ObjectTable { get; set; }
        [PluginService] public IPartyList PartyList { get; set; }
        [PluginService] public ICondition Condition { get; set; }
        [PluginService] public IPluginLog PluginLog { get; set; }
        [PluginService] public IGameInteropProvider GameInteropProvider { get; set; }
        [PluginService] public ITextureProvider TextureProvider { get; set; }
        [PluginService] public IFramework Framework { get; set; }
    }
}

public class FailedToLoadException : Exception
{
    public FailedToLoadException(string? message) : base(message)
    {
    }
}
#pragma warning restore CS8618