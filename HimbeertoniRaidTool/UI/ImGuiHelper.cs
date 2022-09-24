﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.Modules.LootMaster;
using ImGuiNET;
using Lumina.Excel;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    public static class ImGuiHelper
    {
        public static bool SaveButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.Save, "Save", tooltip ?? Localize("Save", "Save"), enabled, size ?? new Vector2(50f, 25f));
        public static bool CancelButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.WindowClose, "Cancel", tooltip ?? Localize("Cancel", "Cancel"), enabled, size ?? new Vector2(50f, 25f));
        public static bool Button(string label, string? tooltip, bool enabled = true, Vector2 size = default)
        {
            ImGui.BeginDisabled(!enabled);
            bool result = ImGui.Button(label, size);
            ImGui.EndDisabled();
            if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(tooltip);
            return result;
        }
        public static bool Button(FontAwesomeIcon icon, string id, string? tooltip, bool enabled = true, Vector2 size = default)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.BeginDisabled(!enabled);
            bool result = ImGui.Button($"{icon.ToIconString()}##{id}", size);
            ImGui.EndDisabled();
            ImGui.PopFont();
            if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(tooltip);
            return result;
        }
        public static bool GearUpdateButton(Player p)
        {
            PlayerCharacter? playerChar = null;
            string inspectTooltip = Localize("Inspect", "Update Gear");
            bool inspectActive = true;
            if (!GearRefresherOnExamine.CanOpenExamine)
            {
                inspectActive = false;
                inspectTooltip = Localize("InspectUnavail", "Functionality unavailable due to incompatibility with game version");
            }
            else if (!Helper.TryGetChar(out playerChar, p.MainChar.Name, p.MainChar.HomeWorld))
            {
                inspectActive = false;
                inspectTooltip = Localize("CharacterNotInReach", "Character is not in reach to examine");
            }
            bool pressed = Button(FontAwesomeIcon.Search, p.Pos.ToString(), inspectTooltip, inspectActive);
            if (pressed)
            {
                GearRefresherOnExamine.RefreshGearInfos(playerChar);
            }
            return pressed;
        }

        public static bool GearUpdateByLodeStoneButton(Player p)
        {
            bool active = true;
            string inspectTooltip = Localize("Lodestone Button", "Update Gear from Lodestone");
            bool pressed = Button(FontAwesomeIcon.Cloud, p.Pos.ToString(), inspectTooltip, active);
            if (pressed)
            {
                Dalamud.Logging.PluginLog.Log("Clicked Lodestone Button.");
            }
            return pressed;
        }

        public static bool Combo<T>(string label, ref T value) where T : struct
        {
            T? value2 = value;
            bool result = Combo(label, ref value2);
            if (result && value2.HasValue)
                value = value2.Value;
            return result;
        }


        public static bool Combo<T>(string label, ref T? value) where T : struct
        {
            int id = value.HasValue ? Array.IndexOf(Enum.GetValues(typeof(T)), value) : 0;
            bool result = ImGui.Combo(label, ref id, Enum.GetNames(typeof(T)).ToArray(), Enum.GetNames(typeof(T)).Length);
            if (result)
                value = (T?)Enum.GetValues(typeof(T)).GetValue(id);
            return result;
        }
        //Credit to UnknownX
        //Modified to have filtering of Excel sheet and be usable by keayboard only
        public static bool ExcelSheetCombo<T>(string id, out T? selected, Func<ExcelSheet<T>, string> getPreview, ImGuiComboFlags flags, Func<T, string, bool> searchPredicate, Func<T, string> toName) where T : ExcelRow
            => ExcelSheetCombo(id, out selected, getPreview, flags, searchPredicate, toName, (t) => true);
        public static bool ExcelSheetCombo<T>(string id, out T? selected, Func<ExcelSheet<T>, string> getPreview, ImGuiComboFlags flags, Func<T, string, bool> searchPredicate, Func<T, string> toName, Func<T, bool> preFilter) where T : ExcelRow
        {
            ExcelSheet<T>? sheet = Services.DataManager.GetExcelSheet<T>();
            if (sheet is null)
            {
                selected = null;
                return false;
            }
            return SearchableCombo(id, out selected, getPreview(sheet), flags, sheet, searchPredicate, toName, preFilter);
        }

        private static string search = string.Empty;
        private static HashSet<object>? filtered;
        private static int hoveredItem = 0;
        //This is a small hack since to my knowledge there is no way to close and existing combo when not clikced
        private static readonly Dictionary<string, (bool toogle, bool wasEnterClickedLastTime)> comboDic = new();
        public static bool SearchableCombo<T>(string id, out T? selected, string preview, ImGuiComboFlags flags, IEnumerable<T> possibilities, Func<T, string, bool> searchPredicate, Func<T, string> toName, Func<T, bool> preFilter) where T : notnull
        {
            bool hasSelected = false;
            if (!comboDic.ContainsKey(id))
                comboDic.Add(id, (false, false));
            (bool toogle, bool wasEnterClickedLastTime) = comboDic[id];
            if (wasEnterClickedLastTime || ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                toogle = !toogle;
                search = string.Empty;
                filtered = null;
            }
            bool enterClicked = ImGui.IsKeyPressed(ImGuiKey.Enter);
            wasEnterClickedLastTime = enterClicked;
            comboDic[id] = (toogle, wasEnterClickedLastTime);
            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                hoveredItem--;
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                hoveredItem++;
            hoveredItem = Math.Clamp(hoveredItem, 0, Math.Max(filtered?.Count - 1 ?? 0, 0));
            selected = default;
            if (!ImGui.BeginCombo(id + (toogle ? "##x" : ""), preview, flags)) return false;

            if (ImGui.IsWindowAppearing() && ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive())
            {
                search = string.Empty;
                filtered = null;
                ImGui.SetKeyboardFocusHere(0);
            }

            if (ImGui.InputText("##ExcelSheetComboSearch", ref search, 128))
                filtered = null;
            if (filtered == null)
            {
                filtered = possibilities.Where(preFilter).Where(s => searchPredicate(s, search)).Cast<object>().ToHashSet();
                hoveredItem = 0;
            }
            int i = 0;
            foreach (var row in filtered.Cast<T>())
            {
                bool hovered = hoveredItem == i;
                ImGui.PushID(i);

                if (ImGui.Selectable(toName(row), hovered) || (enterClicked && hovered))
                {
                    hasSelected = true;
                    selected = row;
                }
                ImGui.PopID();
                i++;
                if (!hasSelected) continue;
                ImGui.EndCombo();
                return true;
            }

            ImGui.EndCombo();
            return false;
        }
    }
}
