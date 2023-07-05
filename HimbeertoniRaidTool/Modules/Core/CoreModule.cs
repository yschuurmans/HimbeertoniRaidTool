﻿using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal class CoreModule : IHrtModule<CoreConfig.ConfigData, CoreConfig.ConfigUi>
{
    private readonly CoreConfig _config;
    private readonly WelcomeWindow _wcw;
    private readonly List<HrtCommand> _registeredCommands = new();
    public string Name => "Core Functions";
    public string Description => "Core functionality of Himbeertoni Raid Tool";

    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>()
    {
        new()
        {
            Command = "/hrt",
            Description = Localization.Localize("/hrt", "Show help"),
            ShowInHelp = true,
            OnCommand = OnCommand,
            ShouldExposeToDalamud = true,
        },
    };

    private IEnumerable<HrtCommand> InternalCommands => new List<HrtCommand>()
    {
        new()
        {
            Command = "/options",
            AltCommands = new List<string>
            {
                "/option",
                "/config",
            },
            Description = Localization.Localize("command:hrt:options", "Shows the Configuration window"),
            OnCommand = (_, _) => ServiceManager.Config.Show(),
            ShowInHelp = true,
        },
        new()
        {
            Command = "/exportLocalization",
            AltCommands = Array.Empty<string>(),
            Description = "Exports strings to localize",
            OnCommand = (_, _) => Localization.ExportLocalizable(),
            ShowInHelp = false,
        },
        new()
        {
            Command = "/welcome",
            AltCommands = Array.Empty<string>(),
            Description = Localization.Localize("command:hrt:welcome",
                "Open Welcome Window with explanations on how to use"),
            OnCommand = (_, _) => _wcw.Show(),
            ShowInHelp = true,
        },
        new()
        {
            Command = "/help",
            AltCommands = new List<string>()
            {
                "/usage",
            },
            Description = Localization.Localize("command:hrt:help", "Prints usage information to chat"),
            OnCommand = PrintUsage,
            ShowInHelp = true,
        },
    };

    public string InternalName => "Core";
    public HRTConfiguration<CoreConfig.ConfigData, CoreConfig.ConfigUi> Configuration => _config;
    public WindowSystem WindowSystem { get; }

    public CoreModule()
    {
        WindowSystem = new WindowSystem(InternalName);
        _wcw = new WelcomeWindow(this);
        WindowSystem.AddWindow(_wcw);
        _config = new CoreConfig(this);
        ServiceManager.CoreModule = this;
        foreach (HrtCommand command in InternalCommands)
            AddCommand(command);
    }

    public void HandleMessage(HrtUiMessage message)
    {
        if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
            PluginLog.Warning(message.Message);
        else
            PluginLog.Information(message.Message);
    }

    internal void AddCommand(HrtCommand command)
    {
        _registeredCommands.Add(command);
    }

    private void OnCommand(string command, string args)
    {
        if (!command.Equals("/hrt")) return;
        string subCommand = '/' + (args.IsNullOrEmpty() ? "help" : args.Split(' ')[0]);
        string newArgs = args.IsNullOrEmpty() ? "" : args[(subCommand.Length - 1)..].Trim();
        if (_registeredCommands.Any(x => x.HandlesCommand(subCommand)))
            _registeredCommands.First(x => x.HandlesCommand(subCommand)).OnCommand(subCommand, newArgs);
        else
            PluginLog.Error($"Argument {args} for command \"/hrt\" not recognized");
    }

    private void PrintUsage(string command, string args)
    {
        if (!command.Equals("/help")) return;
        string subCommand = '/' + args.Split(' ')[0];
        //Propagate help call to sub command
        if (_registeredCommands.Any(c => c.HandlesCommand(subCommand)))
        {
            string newArgs = $"help {args[(subCommand.Length - 1)..].Trim()}";

            _registeredCommands.First(x => x.HandlesCommand(subCommand)).OnCommand(subCommand, newArgs);
            return;
        }

        SeStringBuilder stringBuilder = new SeStringBuilder()
            .AddUiForeground("[Himbeertoni Raid Tool]", 45)
            .AddUiForeground("[Help]", 62)
            .AddText(Localization.Localize("hrt:usage:heading", " Commands used for Himbeertoni Raid Tool:"))
            .Add(new NewLinePayload());
        foreach (HrtCommand c in _registeredCommands.Where(com => !com.Command.Equals("/hrt") && com.ShowInHelp))
            stringBuilder
                .AddUiForeground($"/hrt {c.Command[1..]}", 37)
                .AddText($" - {c.Description}")
                .Add(new NewLinePayload());

        ServiceManager.ChatGui.Print(stringBuilder.BuiltString);
    }

    public void AfterFullyLoaded()
    {
        if (_config.Data.ShowWelcomeWindow)
            _wcw.Show();
    }

    public void Update(Framework fw)
    {
    }

    public void Dispose()
    {
    }
}