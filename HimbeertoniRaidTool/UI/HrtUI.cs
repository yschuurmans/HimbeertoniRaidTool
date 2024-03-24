﻿using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.UI;

public abstract class HrtWindowWithModalChild : HrtWindow
{
    private HrtWindow? _modalChild;
    protected HrtWindow? ModalChild
    {
        get => _modalChild;
        set
        {
            _modalChild = value;
            _modalChild?.Show();
        }
    }
    public bool ChildIsOpen => _modalChild is { IsOpen: true };
    public override void Update()
    {
        if (ModalChild is { IsOpen: false })
        {
            ModalChild = null;
        }
        base.Update();
    }
    public override void PostDraw()
    {
        if (ModalChild == null)
            return;
        bool open = ModalChild.IsOpen;
        if (ImGui.Begin(ModalChild.WindowName, ref open, ModalChild.Flags))
        {
            ModalChild.Draw();
            ImGui.End();
        }
        if (!open)
            ModalChild.IsOpen = open;
    }
    public bool AddChild(HrtWindow? child)
    {
        if (_modalChild != null)
            return false;
        ModalChild = child;
        return true;
    }
}

public abstract class HrtWindow : Window, IEquatable<HrtWindow>
{
    private readonly string _id;
    private bool _hasResizedLastFrame;
    private Vector2 _newSize;
    private ImGuiCond _savedSizingCond = ImGuiCond.None;
    private bool _shouldResize;
    private static UiConfig _config = UiConfig.Default;
    protected Vector2 MaxSize = ImGui.GetIO().DisplaySize * 0.9f;
    protected Vector2 MinSize = default;
    protected bool OpenCentered;
    protected string Title;

    protected HrtWindow(string? id = null, ImGuiWindowFlags flags = ImGuiWindowFlags.None) : base(
        id ?? $"##{Guid.NewGuid()}", flags)
    {
        _id = WindowName;
        Title = "";
    }
    public static void SetConfig(UiConfig config) => _config = config;
    public static float ScaleFactor => ImGui.GetIO().FontGlobalScale;
    public bool Equals(HrtWindow? other) => _id.Equals(other?._id);
    public void Show() => IsOpen = true;
    public void Hide() => IsOpen = false;
    public override void Update()
    {
        WindowName = $"{Title}##{_id}";
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinSize,
            MaximumSize = MaxSize,
        };
    }
    public override bool DrawConditions() =>
        !(_config.HideInCombat && ServiceManager.Condition[ConditionFlag.InCombat])
     && !ServiceManager.Condition[ConditionFlag.BetweenAreas]
     && base.DrawConditions();
    public override void PreDraw()
    {
        if (OpenCentered)
        {
            Position = (ImGui.GetIO().DisplaySize - Size) / 2;
            PositionCondition = ImGuiCond.Appearing;
            OpenCentered = false;

        }
        if (_hasResizedLastFrame)
        {
            SizeCondition = _savedSizingCond;
            _hasResizedLastFrame = false;
        }
        if (_shouldResize)
        {
            Size = _newSize;
            _savedSizingCond = SizeCondition;
            SizeCondition = ImGuiCond.Always;
            _hasResizedLastFrame = true;
            _shouldResize = false;
            ServiceManager.Logger.Debug($"Tried Resizing to: {Size.Value.X}x{Size.Value.Y}");
        }

    }
    protected void Resize(Vector2 newSize)
    {
        _newSize = newSize;
        _shouldResize = true;
    }
    public override bool Equals(object? obj) => Equals(obj as HrtWindow);

    public override int GetHashCode() => _id.GetHashCode();
}

public readonly struct UiConfig(bool hideInCombat)
{
    public static UiConfig Default => new(false);
    public readonly bool HideInCombat = hideInCombat;

}

public class HrtUiMessage(string msg, HrtUiMessageType msgType = HrtUiMessageType.Info)
{
    public string Message = msg;
    public HrtUiMessageType MessageType = msgType;
    public static HrtUiMessage Empty => new("", HrtUiMessageType.Discard);
}

public enum HrtUiMessageType
{
    Discard,
    Info,
    Success,
    Failure,
    Error,
    Important,
    Warning,
}