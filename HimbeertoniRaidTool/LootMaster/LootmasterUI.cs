﻿using ColorHelper;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using static ColorHelper.HRTColorConversions;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.LootMaster
{
    public class LootmasterUI : HrtUI
    {
        [Obsolete]
        private RaidGroup Group => LootMaster.MainGroup;
        private int _CurrenGroupIndex = 0;
        private RaidGroup CurrentGroup => LootMaster.RaidGroups[_CurrenGroupIndex];
        private readonly List<AsyncTaskWithUiResult> Tasks = new();
        private readonly List<HrtUI> Childs = new();
        private string? LocalStringRef = "";
        public LootmasterUI() : base() { }
        public override void Dispose()
        {
            foreach (AsyncTaskWithUiResult t in Tasks)
                t.Dispose();
            Tasks.Clear();
            foreach (HrtUI win in Childs)
                win.Dispose();
            Childs.Clear();
        }
        public override void Draw()
        {
            UpdateChildren();
            if (!Visible)
                return;

            DrawMainWindow();
        }
        public static HSV ILevelColor(GearItem item)
        {
            uint currentMaxILevel = CuratedData.CurrentRaidSavage.ArmorItemLevel;
            if (item.ItemLevel >= currentMaxILevel)
            {
                return ColorName.Green.ToHsv();
            }
            else if (item.ItemLevel >= currentMaxILevel - 10)
            {
                return ColorName.Aquamarine.ToHsv();
            }
            else if (item.ItemLevel >= currentMaxILevel - 20)
            {
                return ColorName.Yellow.ToHsv();
            }
            else
            {
                return ColorName.Red.ToHsv();
            }
        }

        private void UpdateChildren()
        {
            if (!Visible)
                Childs.ForEach(x => x.Hide());
            Childs.ForEach(x => { if (!x.IsVisible) x.Dispose(); });
            Childs.RemoveAll(x => !x.IsVisible);
        }
        private void HandleAsync()
        {
            Tasks.RemoveAll(t => t.FinishedShowing);
            foreach (AsyncTaskWithUiResult t in Tasks)
            {
                t.DrawResult();
            }
        }
        private void DrawMainWindow()
        {

            ImGui.SetNextWindowSize(new Vector2(1600, 600), ImGuiCond.Appearing);
            if (ImGui.Begin(Localize("LootMasterWindowTitle", "Loot Master"), ref Visible,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
            {
                HandleAsync();
                //DrawLootHandlerButtons();

                ImGui.BeginTabBar("RaidGroupSwichtBar");
                ImGui.PushStyleColor(ImGuiCol.TabActive, Vec4(ColorName.Redwood.ToRgb()));
                for (int tabBarIdx = 0; tabBarIdx < LootMaster.RaidGroups.Count; tabBarIdx++)
                {
                    RaidGroup g = LootMaster.RaidGroups[tabBarIdx];
                    ImGuiTabItemFlags flags = ImGuiTabItemFlags.None;
                    if (tabBarIdx == _CurrenGroupIndex)
                        flags = ImGuiTabItemFlags.SetSelected;

                    if (ImGui.TabItemButton(g.Name, flags))
                    {
                        _CurrenGroupIndex = tabBarIdx;
                    }
                    if (ImGui.BeginPopupContextItem($"{g.Name}##{tabBarIdx}"))
                    {

                        if (LocalStringRef is null)
                            LocalStringRef = g.Name;
                        ImGui.InputText(Localize("Name", "Name"), ref LocalStringRef, 100);
                        if (ImGui.Button(Localize("Save", "Save")))
                        {
                            g.Name = LocalStringRef;
                            LocalStringRef = null;
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SameLine();
                        if (ImGui.Button(Localize("Cancel", "Cancel")))
                        {
                            LocalStringRef = null;
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndPopup();
                    }
                    tabBarIdx++;
                }
                ImGui.PopStyleColor();
                ImGui.EndTabBar();
                if (ImGui.BeginTable(Localize("RaidGroup", "RaidGroup"), 14,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.TableSetupColumn(Localize("Player", "Player"));
                    ImGui.TableSetupColumn(Localize("itemLevelShort", "iLvl"));
                    ImGui.TableSetupColumn(Localize("Weapon", "Weapon"));
                    ImGui.TableSetupColumn(Localize("HeadGear", "Head"));
                    ImGui.TableSetupColumn(Localize("ChestGear", "Chest"));
                    ImGui.TableSetupColumn(Localize("Gloves", "Gloves"));
                    ImGui.TableSetupColumn(Localize("LegGear", "Legs"));
                    ImGui.TableSetupColumn(Localize("FeetGear", "Feet"));
                    ImGui.TableSetupColumn(Localize("Earrings", "Earrings"));
                    ImGui.TableSetupColumn(Localize("NeckGear", "Necklace"));
                    ImGui.TableSetupColumn(Localize("WristGear", "Bracelet"));
                    ImGui.TableSetupColumn(Localize("LeftRing", "Ring 1"));
                    ImGui.TableSetupColumn(Localize("RightRing", "Ring 2"));
                    ImGui.TableSetupColumn(Localize("Options", "Options"));
                    ImGui.TableHeadersRow();
                    foreach (Player player in CurrentGroup.Players)
                    {
                        DrawPlayer(player);
                    }
                    ImGui.EndTable();
                }
                if (ImGui.Button(Localize("Close", "Close")))
                {
                    Hide();
                }
            }
            ImGui.End();
        }
        private void DrawLootHandlerButtons()
        {
            if (ImGui.Button(Localize("Loot Boss 1 (Erichthonios)", "Loot Boss 1 (Erichthonios)")))
            {
                LootUi lui = new(CuratedData.AsphodelosSavage, 1, CurrentGroup);
                Childs.Add(lui);
                lui.Show();
            }
            ImGui.SameLine();
            if (ImGui.Button(Localize("Loot Boss 2 (Hippokampos)", "Loot Boss 2 (Hippokampos)")))
            {
                LootUi lui = new(CuratedData.AsphodelosSavage, 2, CurrentGroup);
                Childs.Add(lui);
                lui.Show();
            }
            ImGui.SameLine();
            if (ImGui.Button(Localize("Loot Boss 3 (Phoinix)", "Loot Boss 3 (Phoinix)")))
            {
                LootUi lui = new(CuratedData.AsphodelosSavage, 3, CurrentGroup);
                Childs.Add(lui);
                lui.Show();
            }
            ImGui.SameLine();
            if (ImGui.Button(Localize("Loot Boss 4 (Hesperos)", "Loot Boss 4 (Hesperos)")))
            {
                LootUi lui = new(CuratedData.AsphodelosSavage, 4, CurrentGroup);
                Childs.Add(lui);
                lui.Show();

            }
        }
        private static void DrawItemTooltip(GearItem item)
        {
            ImGui.BeginTooltip();
            if (ImGui.BeginTable("ItemTable", 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn(Localize("ItemTableHeaderHeader", "Header"));
                ImGui.TableSetupColumn(Localize("Value", "Value"));
                DrawRow(Localize("Name", "Name"), item.Item.Name);
                DrawRow(Localize("itemLevelLong", "Item Level"), item.ItemLevel.ToString());
                DrawRow(Localize("itemSource", "Source"), item.Source.ToString());
                ImGui.EndTable();
            }
            ImGui.EndTooltip();

            static void DrawRow(string label, string value)
            {

                ImGui.TableNextColumn();
                ImGui.Text(label);
                ImGui.TableNextColumn();
                ImGui.Text(value);
            }
        }
        private void DrawPlayer(Player player)
        {
            bool PlayerExists = player.Filled && player.MainChar.Filled;
            if (PlayerExists)
            {
                GearSet gear = player.MainChar.MainClass.Gear;
                GearSet bis = player.MainChar.MainClass.BIS;
                ImGui.TableNextColumn();
                ImGui.Text($"{player.Pos}\n{player.NickName}\n{player.MainChar.Name}({player.MainChar.MainClassType})");
                ImGui.TableNextColumn();
                ImGui.Text(gear.ItemLevel.ToString());
                ImGui.Text($"{bis.ItemLevel - gear.ItemLevel} {Localize("to BIS", "to BIS")}");
                ImGui.Text(bis.ItemLevel.ToString());
                DrawItem(gear.MainHand, bis.MainHand);
                DrawItem(gear.Head, bis.Head);
                DrawItem(gear.Body, bis.Body);
                DrawItem(gear.Hands, bis.Hands);
                DrawItem(gear.Legs, bis.Legs);
                DrawItem(gear.Feet, bis.Feet);
                DrawItem(gear.Ear, bis.Ear);
                DrawItem(gear.Neck, bis.Neck);
                DrawItem(gear.Wrist, bis.Wrist);
                DrawItem(gear.Ring1, bis.Ring1);
                DrawItem(gear.Ring2, bis.Ring2);
                ImGui.TableNextColumn();
                ImGui.NewLine();
                EditPlayerButton();
                ImGui.SameLine();
                if (ImGui.Button($"x##{player.Pos}"))
                {
                    player.Reset();
                }
            }
            else
            {

                ImGui.TableNextColumn();
                ImGui.Text(player.Pos.ToString());
                ImGui.Text(Localize("No Player", "No Player"));
                for (int i = 0; i < 12; i++)
                {
                    ImGui.TableNextColumn();
                }
                ImGui.TableNextColumn();
                EditPlayerButton();
            }
            void EditPlayerButton()
            {
                if (ImGui.Button($"{(PlayerExists ? Localize("Edit", "Edit") : Localize("Add", "Add"))}##{player.Pos}"))
                {
                    if (Childs.Exists(x => (x.GetType() == typeof(EditPlayerWindow)) && ((EditPlayerWindow)x).Pos == player.Pos))
                        return;
                    AsyncTaskWithUiResult result;
                    Childs.Add(new EditPlayerWindow(out result, CurrentGroup, player.Pos));
                    Tasks.Add(result);
                }
            }
            void DrawItem(GearItem item, GearItem bis)
            {
                ImGui.TableNextColumn();
                if (item.Valid && bis.Valid && item.Equals(bis))
                {
                    ImGui.NewLine();
                    ImGui.TextColored(
                            Vec4(ILevelColor(item).Saturation(0.8f).Value(0.85f), 1f),
                            $"{item.ItemLevel} {item.Source} {item.Slot.FriendlyName()}");
                    if (ImGui.IsItemHovered())
                        DrawItemTooltip(item);
                    //if (ImGui.IsItemClicked()) Childs.Add(new ShowItemWindow(item));
                }
                else
                {
                    if (item.Valid)
                    {
                        ImGui.TextColored(
                            Vec4(ILevelColor(item).Saturation(0.8f).Value(0.85f), 1f),
                            $"{item.ItemLevel} {item.Source} {item.Slot.FriendlyName()}");
                        if (ImGui.IsItemHovered())
                            DrawItemTooltip(item);
                        //if (ImGui.IsItemClicked()) Childs.Add(new ShowItemWindow(item));
                    }
                    else
                        ImGui.Text(Localize("Empty", "Empty"));
                    ImGui.NewLine();
                    if (bis.Valid)
                    {
                        ImGui.Text($"{bis.ItemLevel} {bis.Source} {bis.Slot.FriendlyName()}");
                        if (ImGui.IsItemHovered())
                            DrawItemTooltip(bis);
                        //if (ImGui.IsItemClicked()) Childs.Add(new ShowItemWindow(bis));
                    }
                    else
                        ImGui.Text(Localize("Empty", "Empty"));
                }
            }
        }

    }

    class LootUi : HrtUI
    {
        private readonly RaidGroup Group;
        private readonly RaidTier RaidTier;
        private readonly int Boss;
        private readonly (GearItem, int)[] Loot;
        private LootRuling LootRuling => HRTPlugin.Configuration.LootRuling;
        private List<HrtUI> Children = new();

        internal LootUi(RaidTier raidTier, int boss, RaidGroup group)
        {
            RaidTier = raidTier;
            Boss = boss;
            Group = group;
            Loot = LootDB.GetPossibleLoot(RaidTier, Boss).ConvertAll(x => (x, 0)).ToArray();
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var child in Children)
            {
                child.Hide();
                child.Dispose();
            }
            Children.Clear();
        }

        public override void Draw()
        {
            if (!Visible)
                return;
            if (ImGui.Begin(String.Format("Loot for {0} boss number {1}", RaidTier.Name, Boss), ref Visible, ImGuiWindowFlags.NoCollapse))
            {
                for (int i = 0; i < Loot.Length; i++)
                {
                    if (Loot[i].Item1.Valid)
                    {
                        ImGui.Text(Loot[i].Item1.Item.Name);
                        ImGui.SameLine();
                        ImGui.InputInt("##" + Loot[i].Item1.Item.Name, ref Loot[i].Item2);

                    }
                }
                if (ImGui.Button(Localize("Distribute", "Distribute")))
                {
                    foreach ((GearItem, int) lootItem in Loot)
                    {
                        List<Player> alreadyLooted = new();
                        for (int j = 0; j < lootItem.Item2; j++)
                        {
                            var evalLoot = LootRuling.Evaluate(Group, lootItem.Item1.Slot, alreadyLooted);
                            alreadyLooted.Add(evalLoot[0].Item1);
                            var lootWindow = new LootResultWindow(lootItem.Item1, evalLoot);
                            Children.Add(lootWindow);
                            lootWindow.Show();
                        }
                    }
                }
                if (ImGui.Button(Localize("Close", "Close")))
                {
                    Hide();
                }
                ImGui.End();
            }
        }
        public override void Hide()
        {
            base.Hide();
            foreach (var win in Children)
                win.Hide();
        }
        class LootResultWindow : HrtUI
        {
            public LootResultWindow(GearItem item1, List<(Player, string)> looters)
            {
                Item = item1;
                Looters = looters;
            }

            public GearItem Item { get; }
            public List<(Player, string)> Looters { get; }

            private LootRuling LootRuling => HRTPlugin.Configuration.LootRuling;
            public override void Draw()
            {
                if (!Visible)
                    return;
                if (ImGui.Begin(String.Format("Loot Results for {0}: ", Item.Name), ref Visible))
                {
                    ImGui.Text(string.Format(Localize("LootRuleHeader", "Loot Results for {0}: "), Item.Name));
                    ImGui.Text(Localize("Following rules were used:", "Following rules were used:"));
                    foreach (LootRule rule in LootRuling.RuleSet)
                        ImGui.BulletText(rule.ToString());
                    int place = 1;
                    foreach ((Player, string) looter in Looters)
                    {
                        ImGui.Text(string.Format(Localize("LootDistributionLine", "Priority {0} for Player {1} won by rule {2} "),
                            place, looter.Item1.NickName, looter.Item2));
                        place++;
                    }
                    ImGui.End();
                }


            }
        }
    }

}
