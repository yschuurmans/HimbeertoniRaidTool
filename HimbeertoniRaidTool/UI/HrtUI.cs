﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Game;
using ImGuiNET;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public abstract class HrtUI : IDisposable
    {
        private bool _disposed = false;
        private bool _volatile;
        private bool _isChild = false;
        protected bool HideInBattle = false;
        protected bool Visible = false;
        private readonly List<HrtUI> Children = new();

        public HrtUI(bool @volatile = true)
        {
            RegisterActions();
            _volatile = @volatile;
        }
        private void RegisterActions()
        {
            Services.PluginInterface.UiBuilder.Draw += InternalDraw;
            Services.Framework.Update += Update;
        }
        private void UnRegisterActions()
        {
            Services.PluginInterface.UiBuilder.Draw -= InternalDraw;
            Services.Framework.Update -= Update;
        }
        public void Show()
        {
            if (_disposed)
                return;
            OnShow();
            Visible = true;
            Children.ForEach(x => x.Show());
        }
        protected virtual void OnShow() { }
        public void Hide()
        {
            Children.ForEach(x => x.Hide());
            Visible = false;
            OnHide();
        }
        protected virtual void OnHide() { }
        private void Update(Framework fw)
        {
            if (!Visible)
                Children.ForEach(x => x.Hide());
            if (!Visible && _volatile)
                Dispose();
            Children.ForEach(x => x.Update(fw));
            Children.RemoveAll(x => x._disposed);
        }
        protected bool AddChild(HrtUI child, bool showOnAdd = false)
        {
            if (!ChildExists(child))
            {
                child.SetUpAsChild();
                Children.Add(child);
                if (showOnAdd)
                    child.Show();
                return true;
            }
            else
            {
                child.Dispose();
                return false;
            }

        }
        protected bool ChildExists<T>([DisallowNull] T c) => Children.Exists(x => c.Equals(x));
        protected void ClearChildren() => Children.ForEach(x => x.Dispose());
        private void SetUpAsChild()
        {
            _volatile = true;
            _isChild = true;
            UnRegisterActions();
            Show();
        }
        protected virtual void BeforeDispose() { }
        public void Dispose()
        {
            if (_disposed)
                return;
            Hide();
            BeforeDispose();
            Children.ForEach(c => c.Dispose());
            Children.Clear();
            if (!_isChild)
                UnRegisterActions();
            _disposed = true;
        }
        private void InternalDraw()
        {
            if (!Visible || _disposed)
                return;
            if (HideInBattle
                && (Services.ClientState.LocalPlayer?.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) ?? false))
                return;
            Children.ForEach(_ => _.InternalDraw());
            Draw();
        }
        protected abstract void Draw();
    }
    public class ConfimationDialog : HrtUI
    {
        private readonly Action _Action;
        private readonly string _Title;
        private readonly string _Text;

        public ConfimationDialog(Action action, string text, string title = "") : base()
        {
            title = title.Equals("") ? Localize("Confirmation", "Confirmation") : title;
            _Text = text;
            _Title = title;
            _Action = action;
            Show();
        }
        protected override void Draw()
        {
            ImGui.OpenPopup(_Title);
            ImGui.SetNextWindowPos(
                new Vector2(ImGui.GetMainViewport().Size.X * 0.5f, ImGui.GetMainViewport().Size.Y * 0.5f),
                ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            if (ImGui.BeginPopupModal(_Title, ref Visible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.Text(_Text);
                if (ImGuiHelper.Button(Localize("OK", "OK"), Localize("Confirm action", "OK")))
                {
                    _Action();
                    Hide();
                }
                ImGui.SameLine();
                if (ImGuiHelper.CancelButton())
                    Hide();
                ImGui.End();
            }
        }
    }
}
