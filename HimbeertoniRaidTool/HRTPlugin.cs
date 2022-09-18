﻿using System;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using HimbeertoniRaidTool.HrtServices;
using HimbeertoniRaidTool.Modules.LootMaster;
using HimbeertoniRaidTool.Modules.WelcomeWindow;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool
{

#pragma warning disable CS8618
    public class Services
    {
        [PluginService] public static SigScanner SigScanner { get; private set; }
        [PluginService] public static CommandManager CommandManager { get; private set; }
        [PluginService] public static ChatGui ChatGui { get; private set; }
        [PluginService] public static DataManager DataManager { get; private set; }
        [PluginService] public static GameGui GameGui { get; private set; }
        [PluginService] public static TargetManager TargetManager { get; private set; }
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] public static ClientState ClientState { get; private set; }
        [PluginService] public static Framework Framework { get; private set; }
        [PluginService] public static ObjectTable ObjectTable { get; private set; }
        [PluginService] public static PartyList PartyList { get; private set; }
        public static IconCache IconCache { get; private set; }

        internal static void Init()
        {
            IconCache ??= new IconCache(PluginInterface, DataManager);
        }

    }
#pragma warning restore CS8618
    public sealed class HRTPlugin : IDalamudPlugin
    {
        private readonly Dalamud.Localization Loc;

        private readonly Configuration _Configuration;
        public string Name => "Himbeertoni Raid Tool";

        private readonly List<string> RegisteredCommands = new();
        private readonly Dictionary<Type, dynamic> RegisteredModules = new();

        public HRTPlugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            //Init all services
            pluginInterface.Create<Services>();
            Services.Init();
            FFXIVClientStructs.Resolver.Initialize(Services.SigScanner.SearchBase);
            //Init Localization
            Loc = new(Services.PluginInterface.AssemblyLocation.Directory + "\\locale");
            Loc.SetupWithLangCode(Services.PluginInterface.UiLanguage);
            Services.PluginInterface.LanguageChanged += OnLanguageChanged;
            DataManagement.DataManager.Init();
            //Load and update/correct configuration + ConfigUi
            _Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            _Configuration.AfterLoad();
            AddCommand(new HrtCommand()
            {
                Command = "/hrt",
                Description = Localize("/hrt", "Open Welcome Window with explanations"),
                ShowInHelp = true,
                OnCommand = OnCommand
            });
            //TODO: Some more elegant way to load modules
            AddModule<LootMasterModule, LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>(Modules.LootMaster.LootMasterModule.Instance);
            AddModule<WelcomeWindowModule, WelcomeWindowConfig.ConfigData, IHrtConfigUi>(WelcomeWindowModule.Instance);

        }
        public T? GetModule<T, S, Q>() where T : IHrtModule<S, Q> where S : new() where Q : IHrtConfigUi
        {
            RegisteredModules.TryGetValue(typeof(T), out dynamic? value);
            return (T?)value;
        }
        private void AddModule<T, S, Q>(T module) where T : IHrtModule<S, Q> where S : new() where Q : IHrtConfigUi
        {
            RegisteredModules.Add(typeof(T), module);
            foreach (HrtCommand command in module.Commands)
                AddCommand(command);
            if (!_Configuration.RegisterConfig(module.Configuration))
                PluginLog.Error($"Confugiration load error:{module.Name}");
            module.AfterFullyLoaded();
        }
        private void OnLanguageChanged(string langCode)
        {
            Loc.SetupWithLangCode(langCode);
        }
        private void AddCommand(HrtCommand command)
        {
            if (
                Services.CommandManager.AddHandler(command.Command,
                new CommandInfo((x, y) => command.OnCommand(y))
                {
                    HelpMessage = command.Description,
                    ShowInHelp = command.ShowInHelp
                })
                )
                RegisteredCommands.Add(command.Command);
        }
        public void Dispose()
        {
            _Configuration.Ui.Dispose();
            RegisteredCommands.ForEach(command => Services.CommandManager.RemoveHandler(command));
            Services.PluginInterface.LanguageChanged -= OnLanguageChanged;
            foreach (var moduleEntry in RegisteredModules)
            {
                try
                {
                    moduleEntry.Value.Dispose();
                }
                catch (Exception e)
                {
                    PluginLog.Fatal($"Unable to Dispose module \"{moduleEntry.Key}\"");
                }
            }
            DataManagement.DataManager.Save();
        }
        private void OnCommand(string args)
        {
            switch (args)
            {
                case string a when a.Contains("option") || a.Contains("config"): _Configuration.Ui.Show(); break;
                case string b when b.Contains("exportlocale"): Loc.ExportLocalizable(); break;
                case string when args.IsNullOrEmpty(): GetModule<WelcomeWindowModule, WelcomeWindowConfig.ConfigData, IHrtConfigUi>()?.Show(); break;
                default:
                    PluginLog.LogError($"Argument {args} for command \"/hrt\" not recognized");
                    break;
            }
        }
    }
}
