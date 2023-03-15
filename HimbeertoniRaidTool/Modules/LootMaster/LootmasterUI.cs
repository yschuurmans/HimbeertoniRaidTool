﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using HimbeertoniRaidTool.Common.Calculations;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.HrtServices;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal class LootmasterUI : HrtWindow
{
    public Vector4 ILevelColor(GearItem item) => (_lootMaster.Configuration.Data.SelectedRaidTier.ArmorItemLevel - (int)item.ItemLevel) switch
    {
        <= 0 => _lootMaster.Configuration.Data.ItemLevelColors[0],
        <= 10 => _lootMaster.Configuration.Data.ItemLevelColors[1],
        <= 20 => _lootMaster.Configuration.Data.ItemLevelColors[2],
        _ => _lootMaster.Configuration.Data.ItemLevelColors[3],
    };
    private readonly LootMasterModule _lootMaster;
    internal int _CurrenGroupIndex { get; private set; }
    private RaidGroup CurrentGroup => RaidGroups[_CurrenGroupIndex];
    private static GameExpansion CurrentExpansion => ServiceManager.GameInfo.CurrentExpansion;
    private static List<RaidGroup> RaidGroups => Services.HrtDataManager.Groups;
    private readonly Queue<HrtUiMessage> _messageQueue = new();
    private (HrtUiMessage message, DateTime time)? _currentMessage;
    private readonly Vector2 _buttonSize;
    private Vector2 ButtonSize => _buttonSize * ScaleFactor;
    internal LootmasterUI(LootMasterModule lootMaster) : base(false, "LootMaster")
    {
        _lootMaster = lootMaster;
        _CurrenGroupIndex = 0;
        Size = new Vector2(1600, 670);
        _buttonSize = new Vector2(30f, 25f);
        SizeCondition = ImGuiCond.FirstUseEver;
        Title = Localize("LootMasterWindowTitle", "Loot Master");

    }
    private bool AddChild(HrtWindow child)
    {
        if (_lootMaster.WindowSystem.Windows.Any(w => child.Equals(w)))
        {
            child.Hide();
            return false;
        }
        _lootMaster.WindowSystem.AddWindow(child);
        child.Show();
        return true;
    }
    public override void OnOpen()
    {
        _CurrenGroupIndex = _lootMaster.Configuration.Data.LastGroupIndex;
    }
    public override void Update()
    {
        base.Update();
        Queue<HrtWindow> toRemove = new();
        foreach (var w in _lootMaster.WindowSystem.Windows.Where(x => !x.IsOpen).Cast<HrtWindow>())
            toRemove.Enqueue(w);
        foreach (var w in toRemove)
        {
            if (!w.Equals(this))
            {
                PluginLog.Debug($"Cleaning Up Window: {w.WindowName}");
                _lootMaster.WindowSystem.RemoveWindow(w);
            }

        }
    }
    private static TimeSpan MessageTimeByMessgeType(HrtUiMessageType type) => type switch
    {
        HrtUiMessageType.Info
        => TimeSpan.FromSeconds(3),
        HrtUiMessageType.Success
        or HrtUiMessageType.Warning
            => TimeSpan.FromSeconds(5),
        HrtUiMessageType.Failure or HrtUiMessageType.Error
        or HrtUiMessageType.Important
            => TimeSpan.FromSeconds(10),
        _ => TimeSpan.FromSeconds(10),
    };
    private void DrawUiMessages()
    {
        if (_currentMessage.HasValue && _currentMessage.Value.time + MessageTimeByMessgeType(_currentMessage.Value.message.MessageType) < DateTime.Now)
            _currentMessage = null;
        if (!_currentMessage.HasValue && _messageQueue.TryDequeue(out var message))
            _currentMessage = (message, DateTime.Now);
        if (_currentMessage.HasValue)
        {
            switch (_currentMessage.Value.message.MessageType)
            {
                case HrtUiMessageType.Error or HrtUiMessageType.Failure:
                    ImGui.TextColored(new Vector4(0.85f, 0.17f, 0.17f, 1f), _currentMessage.Value.message.Message);
                    break;
                case HrtUiMessageType.Success:
                    ImGui.TextColored(new Vector4(0.17f, 0.85f, 0.17f, 1f), _currentMessage.Value.message.Message);
                    break;
                case HrtUiMessageType.Warning:
                    ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.17f, 1f), _currentMessage.Value.message.Message);
                    break;
                case HrtUiMessageType.Important:
                    ImGui.TextColored(new Vector4(0.85f, 0.27f, 0.27f, 1f), _currentMessage.Value.message.Message);
                    break;
                default:
                    ImGui.Text(_currentMessage.Value.message.Message);
                    break;
            }
        }
    }
    private void DrawDetailedPlayer(Player p)
    {
        var curClass = p.MainChar.MainClass;
        ImGui.BeginChild("SoloView");
        ImGui.Columns(3);
        /**
         * Job Selection
         */
        ImGui.Text($"{p.NickName} : {p.MainChar.Name} @ {p.MainChar.HomeWorld?.Name ?? "n.A"}");
        ImGui.SameLine();

        ImGuiHelper.GearUpdateButtons(p, _lootMaster, true);
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditPlayer{p.NickName}", $"{Localize("Edit player", "Edit player")} {p.NickName}"))
        {
            AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, p, _lootMaster.Configuration.Data.GetDefaultBiS));
        }
        ImGui.BeginChild("JobList");
        foreach (var playableClass in p.MainChar.Classes)
        {
            ImGui.Separator();
            ImGui.Spacing();
            bool isMainJob = p.MainChar.MainJob == playableClass.Job;

            if (isMainJob)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            if (ImGuiHelper.Button(playableClass.Job.ToString(), null, true, new Vector2(38f * ScaleFactor, 0f)))
                p.MainChar.MainJob = playableClass.Job;
            if (isMainJob)
                ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.Text("Level: " + playableClass.Level);
            //Current Gear
            ImGui.SameLine();
            ImGui.Text($"{Localize("Current", "Current")} {Localize("iLvl", "iLvl")}: {playableClass.Gear.ItemLevel:D3}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditGear{playableClass.Job}", $"{Localize("Edit", "Edit")} {playableClass.Job} {Localize("gear", "gear")}"))
                AddChild(new EditGearSetWindow(playableClass.Gear, playableClass.Job, _lootMaster.Configuration.Data.SelectedRaidTier));
            //BiS
            ImGui.SameLine();
            ImGui.Text($"{Localize("BiS", "BiS")} {Localize("iLvl", "iLvl")}: {playableClass.BIS.ItemLevel:D3}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditBIS{playableClass.Job}", $"{Localize("Edit", "Edit")} {playableClass.BIS.Name}"))
                AddChild(new EditGearSetWindow(playableClass.BIS, playableClass.Job, _lootMaster.Configuration.Data.SelectedRaidTier));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Redo, playableClass.BIS.EtroID,
                string.Format(Localize("UpdateBis", "Update \"{0}\" from Etro.gg"), playableClass.BIS.Name), playableClass.BIS.EtroID.Length > 0))
                Services.TaskManager.RegisterTask(_lootMaster, () => Services.ConnectorPool.EtroConnector.GetGearSet(playableClass.BIS)
                    , $"BIS update for Character {p.MainChar.Name} ({playableClass.Job}) succeeded"
                    , $"BIS update for Character {p.MainChar.Name} ({playableClass.Job}) failed");
            ImGui.Spacing();
        }
        ImGui.EndChild();
        /**
         * Stat Table
         */
        ImGui.NextColumn();
        if (curClass is not null)
        {
            var curJob = curClass.Job;
            var curRole = curJob.GetRole();
            var mainStat = curJob.MainStat();
            var weaponStat = curRole == Role.Healer || curRole == Role.Caster ? StatType.MagicalDamage : StatType.PhysicalDamage;
            var potencyStat = curRole == Role.Healer || curRole == Role.Caster ? StatType.AttackMagicPotency : StatType.AttackPower;
            ImGui.TextColored(Colors.Red, Localize("StatsUnfinished",
                "Stats are under development and only work correctly for level 70/80/90 jobs"));
            ImGui.BeginTable("MainStats", 5, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV | ImGuiTableFlags.RowBg);
            ImGui.TableSetupColumn(Localize("MainStats", "Main Stats"));
            ImGui.TableSetupColumn(Localize("Current", "Current"));
            ImGui.TableSetupColumn("");
            ImGui.TableSetupColumn(Localize("BiS", "BiS"));
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            DrawStatRow(weaponStat);
            DrawStatRow(StatType.Vitality);
            DrawStatRow(mainStat);
            DrawStatRow(StatType.Defense);
            DrawStatRow(StatType.MagicDefense);
            ImGui.EndTable();
            ImGui.NewLine();
            ImGui.BeginTable("SecondaryStats", 5, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV | ImGuiTableFlags.RowBg);
            ImGui.TableSetupColumn(Localize("SecondaryStats", "Secondary Stats"));
            ImGui.TableSetupColumn(Localize("Current", "Current"));
            ImGui.TableSetupColumn("");
            ImGui.TableSetupColumn(Localize("BiS", "BiS"));
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            DrawStatRow(StatType.CriticalHit);
            DrawStatRow(StatType.Determination);
            DrawStatRow(StatType.DirectHitRate);
            if (curRole == Role.Healer || curRole == Role.Caster)
            {
                DrawStatRow(StatType.SpellSpeed);
                if (curRole == Role.Healer)
                    DrawStatRow(StatType.Piety);
            }
            else
            {
                DrawStatRow(StatType.SkillSpeed);
                if (curRole == Role.Tank)
                    DrawStatRow(StatType.Tenacity);
            }
            ImGui.EndTable();
            ImGui.NewLine();
            void DrawStatRow(StatType type)
            {
                int numEvals = 1;
                if (type == StatType.CriticalHit || type == StatType.Tenacity || type == StatType.SpellSpeed || type == StatType.SkillSpeed)
                    numEvals++;
                ImGui.TableNextColumn();
                ImGui.Text(type.FriendlyName());
                if (type == StatType.CriticalHit)
                    ImGui.Text(Localize("Critical Damage", "Critical Damage"));
                if (type is StatType.SkillSpeed or StatType.SpellSpeed)
                    ImGui.Text(Localize("SpeedMultiplierName", "AA / DoT multiplier"));
                //Current
                ImGui.TableNextColumn();
                ImGui.Text(curClass.GetCurrentStat(type).ToString());
                ImGui.TableNextColumn();
                for (int i = 0; i < numEvals; i++)
                    ImGui.Text(AllaganLibrary.EvaluateStatToDisplay(type, curClass, false, i));
                if (type == weaponStat)
                    ImGuiHelper.AddTooltip(Localize("Dmgper100Tooltip", "Average Dmg with a 100 potency skill"));
                //BiS
                ImGui.TableNextColumn();
                ImGui.Text(curClass.GetBiSStat(type).ToString());
                ImGui.TableNextColumn();
                for (int i = 0; i < numEvals; i++)
                    ImGui.Text(AllaganLibrary.EvaluateStatToDisplay(type, curClass, true, i));
                if (type == weaponStat)
                    ImGuiHelper.AddTooltip(Localize("Dmgper100Tooltip", "Average Dmg with a 100 potency skill"));
            }
        }
        /**
         * Show Gear
         */
        ImGui.NextColumn();
        ImGui.BeginTable("SoloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
        ImGui.TableSetupColumn(Localize("Gear", "Gear"));
        ImGui.TableSetupColumn("");
        ImGui.TableHeadersRow();
        if (curClass is not null)
        {
            DrawSlot(curClass[GearSetSlot.MainHand], true);
            DrawSlot(curClass[GearSetSlot.OffHand], true);
            DrawSlot(curClass[GearSetSlot.Head], true);
            DrawSlot(curClass[GearSetSlot.Ear], true);
            DrawSlot(curClass[GearSetSlot.Body], true);
            DrawSlot(curClass[GearSetSlot.Neck], true);
            DrawSlot(curClass[GearSetSlot.Hands], true);
            DrawSlot(curClass[GearSetSlot.Wrist], true);
            DrawSlot(curClass[GearSetSlot.Legs], true);
            DrawSlot(curClass[GearSetSlot.Ring1], true);
            DrawSlot(curClass[GearSetSlot.Feet], true);
            DrawSlot(curClass[GearSetSlot.Ring2], true);
        }
        else
        {
            for (int i = 0; i < 12; i++)
            {
                ImGui.TableNextColumn();
            }
        }
        ImGui.EndTable();
        ImGui.EndChild();
    }
    public override void Draw()
    {
        if (_CurrenGroupIndex > RaidGroups.Count - 1 || _CurrenGroupIndex < 0)
            _CurrenGroupIndex = 0;
        DrawUiMessages();
        if (ImGuiHelper.Button(FontAwesomeIcon.Cog, "showconfig", Localize("lootmaster:button:showconfig:tooltip", "Open Configuration")))
            Services.Config.Ui.Show();
        ImGui.SameLine();
        DrawLootHandlerButtons();
        DrawRaidGroupSwitchBar();
        if (CurrentGroup.Type == GroupType.Solo)
        {
            if (CurrentGroup[0].MainChar.Filled)
                DrawDetailedPlayer(CurrentGroup[0]);
            else
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "Solo", Localize("Add Player", "Add Player")))
                    AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, CurrentGroup[0], _lootMaster.Configuration.Data.GetDefaultBiS));
            }
        }
        else
        {
            if (ImGui.BeginTable("RaidGroup", 14,
            ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn(Localize("Player", "Player"));
                ImGui.TableSetupColumn(Localize("itemLevelShort", "iLvl"));
                foreach (var slot in GearSet.Slots)
                {
                    if (slot == GearSetSlot.OffHand)
                        continue;
                    ImGui.TableSetupColumn(slot.FriendlyName());
                }
                ImGui.TableSetupColumn(Localize("Options", "Options"));
                ImGui.TableHeadersRow();
                for (int position = 0; position < CurrentGroup.Count; position++)
                {
                    ImGui.PushID(position.ToString());
                    DrawPlayerRow(CurrentGroup[position], position);
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }
    }

    private void DrawRaidGroupSwitchBar()
    {
        ImGui.BeginTabBar("RaidGroupSwichtBar");

        for (int tabBarIdx = 0; tabBarIdx < RaidGroups.Count; tabBarIdx++)
        {
            bool colorPushed = false;
            var g = RaidGroups[tabBarIdx];
            if (tabBarIdx == _CurrenGroupIndex)
            {
                ImGui.PushStyleColor(ImGuiCol.Tab, Colors.RedWood);
                colorPushed = true;
            }
            if (ImGui.TabItemButton($"{g.Name}##{tabBarIdx}"))
                _CurrenGroupIndex = tabBarIdx;
            ImGuiHelper.AddTooltip(Localize("GroupTabTooltip", "Right click for more options"));
            //0 is reserved for Solo on current Character (non editable)
            if (tabBarIdx > 0)
            {
                if (ImGui.BeginPopupContextItem($"{g.Name}##{tabBarIdx}"))
                {
                    if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditGroup", Localize("Edit group", "Edit group")))
                    {
                        AddChild(new EditGroupWindow(g));
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.TrashAlt, "DeleteGroup", Localize("Delete group", "Delete group")))
                    {
                        AddChild(new ConfimationDialog(
                            () => RaidGroups.Remove(g),
                            $"{Localize("DeleteRaidGroup", "Do you really want to delete following group:")} {g.Name}"));
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();

                }
            }
            if (colorPushed)
                ImGui.PopStyleColor();
        }
        const string newGroupContextMenuID = "NewGroupContextMenu";
        if (ImGui.TabItemButton("+"))
        {
            ImGui.OpenPopup(newGroupContextMenuID);
        }
        if (ImGui.BeginPopupContextItem(newGroupContextMenuID))
        {
            if (ImGuiHelper.Button(Localize("From current Group", "From current Group"), null))
            {
                _lootMaster.AddGroup(new(Localize("AutoCreatedGroupName", "Auto Created")), true);
                ImGui.CloseCurrentPopup();
            }
            if (ImGuiHelper.Button(Localize("From scratch", "From scratch"), Localize("Add empty group", "Add empty group")))
            {
                AddChild(new EditGroupWindow(new RaidGroup(), group => _lootMaster.AddGroup(group, false)));
            }
            ImGui.EndPopup();
        }
        ImGui.EndTabBar();
    }

    private void DrawPlayerRow(Player player, int pos)
    {
        if (player.Filled)
        {
            //Player Column
            ImGui.TableNextColumn();
            ImGui.Text($"{player.CurJob?.Role.FriendlyName()}:   {player.NickName}");
            ImGui.Text($"{player.MainChar.Name} @ {player.MainChar.HomeWorld?.Name ?? Localize("n.A.", "n.A.")}");
            var curJob = player.CurJob;
            if (curJob != null)
            {
                if (player.MainChar.Classes.Count() > 1)
                {
                    ImGui.SetNextItemWidth(110 * ScaleFactor);
                    if (ImGui.BeginCombo($"##Class", curJob.ToString()))
                    {
                        foreach (var job in player.MainChar)
                        {
                            if (ImGui.Selectable(job.ToString()))
                                player.MainChar.MainJob = job.Job;
                        }
                        ImGui.EndCombo();
                    }
                }
                else
                {
                    ImGui.Text(curJob.ToString());
                }
                //Gear Column
                var gear = curJob.Gear;
                var bis = curJob.BIS;
                ImGui.TableNextColumn();
                ImGui.Text($"{gear.ItemLevel}");
                ImGuiHelper.AddTooltip(gear.HrtID);
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditCurGear", $"Edit {gear.Name}"))
                    AddChild(new EditGearSetWindow(gear, curJob.Job)); ;
                //ImGui.Text($"{bis.ItemLevel - gear.ItemLevel} {Localize("to BIS", "to BIS")}");
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeightWithSpacing() / 2f);
                ImGui.Text($"{bis.ItemLevel}");
                if (ImGui.IsItemClicked())
                    Services.TaskManager.RegisterTask(a => { }, () =>
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = EtroConnector.GearsetWebBaseUrl + bis.EtroID,
                            UseShellExecute = true,
                        });
                        return new HrtUiMessage();
                    });
                ImGuiHelper.AddTooltip(EtroConnector.GearsetWebBaseUrl + bis.EtroID);
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditBiSGear", $"Edit {bis.Name}"))
                    AddChild(new EditGearSetWindow(bis, curJob.Job));
                foreach ((var slot, var itemTuple) in curJob.ItemTuples)
                {
                    if (slot == GearSetSlot.OffHand)
                        continue;
                    DrawSlot(itemTuple);
                }
            }
            else
            {
                ImGui.Text("");
                for (int i = 0; i < 12; i++)
                    ImGui.TableNextColumn();
            }
            /**
             * Start of functional button section
             */
            {
                ImGui.TableNextColumn();
                ImGuiHelper.GearUpdateButtons(player, _lootMaster, false, ButtonSize);
                var buttonSize = ImGui.GetItemRectSize();
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.ArrowsAltV, $"Rearrange", Localize("lootmaster:button:swapposition:tooltip", "Swap Position"), true, ButtonSize))
                {
                    AddChild(new SwapPositionWindow(pos, CurrentGroup));
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "Edit",
                    $"{Localize("Edit", "Edit")} {player.NickName}", true, buttonSize))
                {
                    AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, player, _lootMaster.Configuration.Data.GetDefaultBiS));
                }
                if (ImGuiHelper.Button(FontAwesomeIcon.Wallet, "Inventory", Localize("lootmaster:button:inventorytooltip", "Open inventory window"), true, buttonSize))
                    AddChild(new InventoryWindow(player.MainChar.MainInventory, $"{player.MainChar.Name}'s Inventory"));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, "Details",
                    $"{Localize("PlayerDetails", "Show player details for")} {player.NickName}", true, buttonSize))
                {
                    AddChild(new PlayerdetailWindow(this, player));
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.TrashAlt, "Delete",
                    $"{Localize("Delete", "Delete")} {player.NickName}", true, buttonSize))
                {
                    AddChild(new ConfimationDialog(
                        () => player.Reset(),
                        $"{Localize("DeletePlayerConfirmation", "Do you really want to delete following player?")} : {player.NickName}"));
                }
            }
        }
        else
        {
            ImGui.TableNextColumn();
            ImGui.Text(Localize("No Player", "No Player"));
            for (int i = 0; i < 12; i++)
                ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddNew", Localize("Add", "Add")))
                AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, player, _lootMaster.Configuration.Data.GetDefaultBiS));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "AddFromDB", Localize("Add from DB", "Add from DB")))
                AddChild(new GetCharacterFromDBWindow(ref player));
        }
    }
    private void DrawSlot((GearItem, GearItem) itemTuple, bool extended = false)
    {
        (var item, var bis) = itemTuple;
        ImGui.TableNextColumn();
        if (item.Filled && bis.Filled &&
            item.Equals(bis, _lootMaster.Configuration.Data.IgnoreMateriaForBiS ? ItemComparisonMode.IgnoreMateria : ItemComparisonMode.Full))
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeightWithSpacing() / (extended ? 2 : 1));
            ImGui.BeginGroup();
            DrawItem(item, extended, true);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }
        }
        else
        {
            ImGui.BeginGroup();
            DrawItem(item, extended);
            ImGui.NewLine();
            DrawItem(bis, extended);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (item.Filled && bis.Filled)
                    ImGui.Columns(2);
                item.Draw();
                if (item.Filled && bis.Filled)
                    ImGui.NextColumn();
                bis.Draw();
                ImGui.Columns();
                ImGui.EndTooltip();
            }
        }
        void DrawItem(GearItem item, bool extended, bool multiLine = false)
        {
            if (item.Filled)
            {
                string toDraw = string.Format(_lootMaster.Configuration.Data.ItemFormatString,
                    item.ItemLevel,
                    item.Source.FriendlyName(),
                    item.Slots.FirstOrDefault(GearSetSlot.None).FriendlyName());
                if (_lootMaster.Configuration.Data.ColoredItemNames)
                    ImGui.TextColored(ILevelColor(item), toDraw);
                else
                    ImGui.Text(toDraw);
                if (extended)
                {
                    List<string> materia = new();
                    foreach (var mat in item.Materia)
                        materia.Add($"{mat.StatType.Abbrev()} +{mat.GetStat()}");
                    if (!multiLine)
                        ImGui.SameLine();
                    ImGui.Text($"( {string.Join(" | ", materia)} )");
                }
            }
            else
                ImGui.Text(Localize("Empty", "Empty"));

        }
    }
    private void DrawLootHandlerButtons()
    {
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(_lootMaster.Configuration.Data.SelectedRaidTier.Name).X + 32f * ScaleFactor);
        if (ImGui.BeginCombo("##Raid Tier", _lootMaster.Configuration.Data.SelectedRaidTier.Name))
        {
            for (int i = 0; i < CurrentExpansion.SavageRaidTiers.Length; i++)
            {
                var tier = CurrentExpansion.SavageRaidTiers[i];
                if (ImGui.Selectable(tier.Name))
                {
                    if (i == CurrentExpansion.SavageRaidTiers.Length - 1)
                        _lootMaster.Configuration.Data.RaidTierOverride = null;
                    else
                        _lootMaster.Configuration.Data.RaidTierOverride = i;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGui.Text(Localize("Distribute loot for:", "Distribute loot for:"));
        ImGui.SameLine();

        foreach (var lootSource in _lootMaster.Configuration.Data.SelectedRaidTier.Bosses)
        {
            if (ImGuiHelper.Button(lootSource.Name, null))
            {
                AddChild(new LootSessionUI(lootSource, CurrentGroup, _lootMaster.Configuration.Data.LootRuling, _lootMaster.Configuration.Data.RolePriority));
            }
            ImGui.SameLine();
        }
        ImGui.NewLine();
    }

    internal void HandleMessage(HrtUiMessage message)
    {
        _messageQueue.Enqueue(message);
    }

    private class PlayerdetailWindow : HrtWindow
    {
        private readonly Action<Player> DrawPlayer;
        private readonly Player Player;
        public PlayerdetailWindow(LootmasterUI lmui, Player p) : base()
        {
            DrawPlayer = lmui.DrawDetailedPlayer;
            Player = p;
            Show();
            Title = $"{Localize("PlayerDetailsTitle", "Player Details")} {Player.NickName}";
            (Size, SizeCondition) = (new Vector2(1600, 600), ImGuiCond.Appearing);
        }
        public override void Draw() => DrawPlayer(Player);
    }
}
internal class GetCharacterFromDBWindow : HrtWindow
{
    private readonly Player _p;
    private readonly uint[] Worlds;
    private readonly string[] WorldNames;
    private int worldSelectIndex;
    private string[] CharacterNames = Array.Empty<string>();
    private int CharacterNameIndex = 0;
    private string NickName = " ";
    internal GetCharacterFromDBWindow(ref Player p) : base(true, $"GetCharacterFromDBWindow{p.NickName}")
    {
        _p = p;
        Worlds = Services.HrtDataManager.GetWorldsWithCharacters().ToArray();
        WorldNames = Array.ConvertAll(Worlds, x => Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.GetRow(x)?.Name.RawString ?? "");
        Title = Localize("GetCharacterTitle", "Get character from DB");
        Size = new Vector2(350, 420);
        Flags = ImGuiWindowFlags.NoScrollbar;
    }
    public override void Draw()
    {
        ImGui.InputText(Localize("Player Name", "Player Name"), ref NickName, 50);
        if (ImGui.ListBox("World", ref worldSelectIndex, WorldNames, WorldNames.Length))
        {
            var list = Services.HrtDataManager.GetCharacterNames(Worlds[worldSelectIndex]);
            list.Sort();
            CharacterNames = list.ToArray();
        }
        ImGui.ListBox("Name", ref CharacterNameIndex, CharacterNames, CharacterNames.Length);
        if (ImGuiHelper.Button(FontAwesomeIcon.Save, "save", Localize("Save", "Save")))
        {

            _p.NickName = NickName;
            var c = _p.MainChar;
            c.Name = CharacterNames[CharacterNameIndex];
            c.HomeWorldID = Worlds[worldSelectIndex];
            Services.HrtDataManager.GetManagedCharacter(ref c);
            _p.MainChar = c;
            Hide();
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.WindowClose, "cancel", Localize("Cancel", "Cancel")))
            Hide();
    }
}
internal class InventoryWindow : HRTWindowWithModalChild
{
    private readonly Inventory _inv;

    internal InventoryWindow(Inventory inv, string title)
    {
        Size = new(400f, 550f);
        SizeCondition = ImGuiCond.Appearing;
        Title = title;
        _inv = inv;
        foreach (var boss in ServiceManager.GameInfo.CurrentExpansion.CurrentSavage.Bosses)
            foreach (var item in boss.GuaranteedItems)
            {
                if (!_inv.Contains(item.ID))
                {
                    _inv[_inv.FirstFreeSlot()] = new(item)
                    {
                        quantity = 0
                    };
                }
            }

    }
    public override void Draw()
    {
        if (ImGuiHelper.CloseButton())
            Hide();
        foreach (var boss in ServiceManager.GameInfo.CurrentExpansion.CurrentSavage.Bosses)
            foreach (var item in boss.GuaranteedItems)
            {
                var icon = Services.IconCache[item.Item!.Icon];
                ImGui.Image(icon.ImGuiHandle, icon.Size());
                ImGui.SameLine();
                ImGui.Text(item.Name);
                ImGui.SameLine();
                var entry = _inv[_inv.IndexOf(item.ID)];
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                ImGui.InputInt($"##{item.Name}", ref entry.quantity);
                _inv[_inv.IndexOf(item.ID)] = entry;
            }
        ImGui.Separator();
        ImGui.Text(Localize("Inventory:AdditionalGear", "Additional Gear"));
        var iconSize = new Vector2(37, 37) * ScaleFactor;
        foreach ((int idx, var entry) in _inv.Where(e => e.Value.IsGear))
        {
            ImGui.PushID(idx);
            if (entry.Item is not GearItem item || item.Item is null)
                continue;
            var icon = Services.IconCache[item.Item.Icon];
            if (ImGuiHelper.Button(FontAwesomeIcon.Trash, "Delete", null, true, iconSize))
                _inv.Remove(idx);
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Image(icon.ImGuiHandle, iconSize);
            ImGui.SameLine();
            ImGui.Text(item.Name);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }
            ImGui.PopID();
        }
        ImGui.BeginDisabled(ChildIsOpen);
        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, $"Add", null, true, iconSize))
            ModalChild = new SelectGearItemWindow(i => _inv.Add(_inv.FirstFreeSlot(), i), i => { }, null, null, null, ServiceManager.GameInfo.CurrentExpansion.CurrentSavage.ArmorItemLevel);
        ImGui.EndDisabled();
    }
}
internal class SwapPositionWindow : HrtWindow
{
    private readonly RaidGroup _group;
    private readonly int _oldPos;
    private int _newPos;
    private readonly int[] possiblePositions;
    private readonly string[] possiblePositionNames;
    internal SwapPositionWindow(int pos, RaidGroup g) : base(true, $"SwapPositionWindow{g.GetHashCode()}{pos}")
    {
        _group = g;
        _oldPos = pos;
        _newPos = 0;
        List<int> positions = Enumerable.Range(0, g.Count).ToList();
        positions.Remove(_oldPos);
        possiblePositions = positions.ToArray();
        possiblePositionNames = positions.ConvertAll(position => $"{_group[position].NickName} (#{position + 1})").ToArray();
        Size = new Vector2(170f, _group.Type == GroupType.Raid ? 230f : 150f);
        Title = $"{Localize("Swap Position of", "Swap Position of")} {_group[_oldPos].NickName}";
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;
    }
    public override void Draw()
    {
        if (ImGuiHelper.SaveButton(Localize("Swap players positions", "Swap players positions")))
        {
            int newPos = possiblePositions[_newPos];
            if (newPos != _oldPos)
            {
                (_group[_oldPos], _group[newPos]) = (_group[newPos], _group[_oldPos]);
            }
            Hide();
        }
        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
            Hide();
        ImGui.ListBox("", ref _newPos, possiblePositionNames, possiblePositions.Length);


    }
}
