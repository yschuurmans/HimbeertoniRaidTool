﻿using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using System;
using Dalamud.Logging;
using Dalamud.Game;
using Dalamud.Data;

namespace HimbeertoniRaidTool
{
#pragma warning disable CS8618
    public class Services
    {
        [PluginService] public static SigScanner SigScanner { get; private set; }
        [PluginService] public static CommandManager CommandManager { get; private set; }
        [PluginService] public static ChatGui ChatGui { get; private set; }
        [PluginService] public static DataManager Data { get; private set; }
        [PluginService] public static GameGui GameGui { get; private set; }
    }
#pragma warning restore CS8618
    public sealed class HRTPlugin : IDalamudPlugin
    {
        
        private static HRTPlugin? _Plugin;
        public static HRTPlugin Plugin => _Plugin ?? throw new NullReferenceException();
        public string Name => "Himbeertoni Raid Tool";

        private readonly List<Tuple<string, string, bool>> Commands = new List<Tuple<string, string, bool>> {
            new Tuple<string, string, bool>("/hrt" , "Does nothing at the moment", true ),
            new Tuple<string, string, bool>("/lootmaster", "Opens LootMaster Window", false ),
            new Tuple<string, string, bool>("/lm" , "Opens LootMaster Window (short version)", true )
        };
        
        internal DalamudPluginInterface PluginInterface { get; init; }
        
        internal Configuration Configuration { get; init; }
        private ConfigUI OptionsUi { get; init; }
        private LootMaster.LootMaster LM { get; init; }
        
        public HRTPlugin([RequiredVersion("1.0")]DalamudPluginInterface pluginInterface)
        {
            _Plugin = this;
            pluginInterface.Create<Services>();
            FFXIVClientStructs.Resolver.Initialize(Services.SigScanner.SearchBase);
            this.PluginInterface = pluginInterface;
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            if(this.Configuration.GroupInfo != null)
            {
                this.LM = new(Configuration.GroupInfo);
            }
            else
            {
                this.LM = new(this);
            }
            this.OptionsUi = new(this);
            InitCommands();
        }

        private void InitCommands()
        {
            foreach(var command in Commands)
            {
                Services.CommandManager.AddHandler(command.Item1, new CommandInfo(OnCommand)
                {
                    HelpMessage = command.Item2,
                    ShowInHelp = command.Item3
                });
            }
        }

        public void Dispose()
        {
            this.OptionsUi.Dispose();
            this.LM.Dispose();
            foreach(var command in Commands)
            {
                Services.CommandManager.RemoveHandler(command.Item1);
            }
        }
        private void OnCommand(string command, string args)
        {
            switch(command)
            {
                case "/hrt":
                    this.OptionsUi.Show();
                    break;
                case "/lm":
                case "/lootmaster":
                    this.LM.OnCommand(args);
                    break;
                default:
                    PluginLog.LogError("Command \"" + command + "\" not found");
                    break;
            }            
        }
    }
}
