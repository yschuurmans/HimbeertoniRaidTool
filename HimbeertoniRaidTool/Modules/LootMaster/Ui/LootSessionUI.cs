﻿using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

internal class LootSessionUi : HrtWindow
{
    private const string RULES_POPUP_ID = "RulesButtonPopup";
    private readonly IEnumerable<RaidGroup> _possibleGroups;
    private readonly UiSortableList<LootRule> _ruleListUi;
    private readonly LootSession _session;

    internal LootSessionUi(InstanceWithLoot lootSource, RaidGroup group, IEnumerable<RaidGroup> possibleGroups,
                           LootRuling lootRuling,
                           RolePriority defaultRolePriority)
    {
        _session = new LootSession(group, lootRuling, defaultRolePriority, lootSource);
        _ruleListUi = new UiSortableList<LootRule>(LootRuling.PossibleRules, lootRuling.RuleSet);

        MinSize = new Vector2(600, 300);
        //Size = new Vector2(1100, 600);
        SizeCondition = ImGuiCond.Appearing;
        Title = string.Format(LootmasterLoc.LootSessionWindow_Title, lootSource.Name);
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
        OpenCentered = true;
        _possibleGroups = possibleGroups;
    }

    public override void Draw()
    {
        //Header
        if (ImGuiHelper.CloseButton())
            Hide();
        ImGui.SameLine();
        ImGui.Text($"{LootmasterLoc.Lootsession_State}: {_session.CurrentState.FriendlyName()}");
        ImGui.SameLine();

        if (ImGuiHelper.Button(FontAwesomeIcon.Cogs, "##RulesButton",
                               LootmasterLoc.LootSession_RulesButton_Tooltip))
            ImGui.OpenPopup(RULES_POPUP_ID);
        if (ImGui.BeginPopup(RULES_POPUP_ID))
        {
            if (ImGuiHelper.CloseButton())
                ImGui.CloseCurrentPopup();
            DrawRulingOptions();
            ImGui.EndPopup();
        }

        ImGui.BeginDisabled(_session.CurrentState >= LootSession.State.LootChosen);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ScaleFactor * 200f);
        if (ImGui.BeginCombo("##RaidGroup", _session.Group.Name))
        {
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (RaidGroup group in _possibleGroups)
            {
                if (ImGui.Selectable(group.Name) && group != _session.Group)
                    _session.Group = group;
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGuiHelper.Button(LootmasterLoc.LootSession_CalcButton_Text,
                               LootmasterLoc.LootSession_CalcButton_Tooltip))
            _session.Evaluate();
        ImGui.EndDisabled();
        ImGui.NewLine();

        DrawLootSelection();
        ImGui.NewLine();

        if (_session.CurrentState >= LootSession.State.LootChosen)
            DrawResults();
    }

    private void DrawLootSelection()
    {
        const float itemSize = 80f;
        const int itemsPerRow = 7;
        int rows = (int)Math.Ceiling(_session.Loot.Count / (float)itemsPerRow);
        ImGui.BeginDisabled(_session.CurrentState >= LootSession.State.LootChosen);
        for (int row = 0; row < rows; row++)
        {
            ImGui.PushID(row);
            if (ImGui.BeginTable("##LootSelection", itemsPerRow,
                                 ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.SizingFixedFit))
            {
                for (int col = 0; col < itemsPerRow; col++)
                {
                    if (row * itemsPerRow + col >= _session.Loot.Count)
                    {
                        ImGui.TableNextRow();
                        break;
                    }

                    HrtItem item = _session.Loot[row * itemsPerRow + col].item;
                    ImGui.TableNextColumn();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ScaleFactor);
                    ImGui.Image(ServiceManager.IconCache[item.Icon].ImGuiHandle,
                                Vector2.One * ScaleFactor * (itemSize - 30f));
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        item.Draw();
                        ImGui.EndTooltip();
                    }
                }

                for (int col = 0; col < itemsPerRow; col++)
                {
                    if (row * itemsPerRow + col >= _session.Loot.Count)
                    {
                        ImGui.TableNextRow();
                        break;
                    }

                    (HrtItem item, int count) = _session.Loot[row * itemsPerRow + col];
                    ImGui.TableNextColumn();
                    int count2 = count;
                    ImGui.SetNextItemWidth(ScaleFactor * (itemSize - 10f));
                    if (ImGui.InputInt($"##Input{item.Id}", ref count2))
                    {
                        if (count2 < 0)
                            count2 = 0;
                        _session.Loot[row * itemsPerRow + col] = (item, count2);
                    }
                }

                ImGui.EndTable();
            }

            ImGui.PopID();
        }

        ImGui.EndDisabled();
    }

    private void DrawRulingOptions()
    {
        //Begin rule section
        ImRaii.IEndObject rulesChild =
            ImRaii.Child("##Rules", new Vector2(270 * ScaleFactor, 270 * ScaleFactor), false);
        if (!rulesChild.Success)
            return;
        ImGui.TextWrapped($"{LootmasterLoc.LootSession_text_rolePriority}:\n{_session.RolePriority}");
        if (_ruleListUi.Draw())
            _session.RulingOptions.RuleSet = new List<LootRule>(_ruleListUi.List);
        ImGui.NewLine();
        ImGui.TextWrapped(LootmasterLoc.LootSession_text_rolePriorityHint);
    }

    private void DrawResults()
    {
        //Header 
        ImGui.Text(string.Format(LootmasterLoc.LootSession_heading_results, _session.Instance.Name,
                                 _session.Group.Name));
        ImGui.SameLine();
        if (ImGuiHelper.Button(LootmasterLoc.LootResults_AbortButton_Text,
                               LootmasterLoc.LootResults_AbortButton_Tooltip,
                               _session.CurrentState < LootSession.State.DistributionStarted))
            _session.RevertToChooseLoot();
        ImGui.Separator();
        //Guaranteed Item
        ImGui.Text(LootmasterLoc.LootResultWindow_GuaranteedItems);
        if (_session.GuaranteedLoot.Count == 0)
            ImGui.Text(GeneralLoc.None);
        foreach ((HrtItem item, bool awarded) in _session.GuaranteedLoot)
        {
            ImGui.BeginGroup();
            ImGui.Image(ServiceManager.IconCache[item.Icon].ImGuiHandle,
                        Vector2.One * ImGui.GetTextLineHeightWithSpacing());
            ImGui.SameLine();
            ImGui.Text(item.Name);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button($"{LootmasterLoc.LootResultWindow_Button_AwardGuaranteed}##{item.Id}",
                                   LootmasterLoc.LootResultWindow_Button_AwardGuaranteed_Tooltip, !awarded))
                _session.AwardGuaranteedLoot(item);
        }

        //Possible Items
        ImGui.NewLine();
        ImGui.Text(LootmasterLoc.LootResult_PossibleItems);
        ImGui.Separator();
        if (_session.Results.Count == 0)
            ImGui.Text(GeneralLoc.None);
        foreach (((HrtItem item, int nr), LootResultContainer results) in _session.Results)
        {
            ImGui.PushID($"{item.Id}##{nr}");
            if (ImGui.CollapsingHeader($"{item.Name} # {nr + 1}  \n {results.ShortResult}",
                                       ImGuiTreeNodeFlags.DefaultOpen))
                if (ImGui.BeginTable("LootTable", 4 + _session.RulingOptions.ActiveRules.Count(),
                                     ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn(GeneralLoc.Pos);
                    ImGui.TableSetupColumn(GeneralLoc.Player);
                    ImGui.TableSetupColumn(GeneralLoc.Needed_items);
                    ImGui.TableSetupColumn(GeneralLoc.Rule);
                    foreach (LootRule rule in _session.RulingOptions.ActiveRules)
                    {
                        ImGui.TableSetupColumn(rule.Name);
                    }
                    ImGui.TableHeadersRow();

                    int place = 1;
                    LootRule lastRule = LootRuling.Default;
                    for (int i = 0; i < results.Count; i++)
                    {
                        LootResult singleResult = results[i];
                        if (singleResult.ShouldIgnore) continue;
                        LootResult? nextResult = i + 1 < results.Count ? results[i + 1] : null;
                        ImGui.TableNextColumn();
                        ImGui.Text(place.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text($"{singleResult.Player.NickName} ({singleResult.ApplicableJob})");
                        ImGui.TableNextColumn();
                        foreach (GearItem neededItem in singleResult.NeededItems)
                        {
                            ImGui.Text(neededItem.Name);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                neededItem.Draw();
                                ImGui.EndTooltip();
                            }

                            if (!results.IsAwarded ||
                                results.AwardedIdx == i && neededItem.Equals(results[i].AwardedItem))
                            {
                                ImGui.SameLine();
                                if (neededItem.Slots.Count() > 1)
                                {
                                    if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"##Award##{i}##{neededItem.Id}",
                                                           $"{LootmasterLoc.LootResult_AwardButton_Tooltip} ({GeneralLoc.Right_Abbrev})",
                                                           !results.IsAwarded))
                                        _session.AwardItem((item, nr), neededItem, i);
                                    ImGui.SameLine();
                                    if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"##Award##{i}##{neededItem.Id}",
                                                           $"{LootmasterLoc.LootResult_AwardButton_Tooltip} ({GeneralLoc.Left_Abbrev}",
                                                           !results.IsAwarded))
                                        _session.AwardItem((item, nr), neededItem, i, true);
                                }
                                else
                                {
                                    if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"##Award##{i}##{neededItem.Id}",
                                                           $"{LootmasterLoc.LootResult_AwardButton_Tooltip}",
                                                           !results.IsAwarded))
                                        _session.AwardItem((item, nr), neededItem, i);
                                }
                            }
                        }

                        ImGui.TableNextColumn();
                        if (singleResult.Category == LootCategory.Need)
                        {
                            LootRule decidingFactor = singleResult.DecidingFactor(nextResult);
                            ImGui.Text(decidingFactor.Name);
                            foreach (LootRule rule in _session.RulingOptions.ActiveRules)
                            {
                                ImGui.TableNextColumn();
                                string toPrint =
                                    singleResult.EvaluatedRules.TryGetValue(rule, out (float _, string val) a)
                                        ? a.val
                                        : "";
                                if (rule == decidingFactor && rule == lastRule)
                                    ImGui.TextColored(Colors.Yellow, toPrint);
                                else if (rule == decidingFactor)
                                    ImGui.TextColored(Colors.Green, toPrint);
                                else if (rule == lastRule)
                                    ImGui.TextColored(Colors.Red, toPrint);
                                else
                                    ImGui.Text(toPrint);
                            }

                            lastRule = decidingFactor;
                        }
                        else
                        {
                            ImGui.Text(singleResult.Category.FriendlyName());
                            foreach (LootRule _ in _session.RulingOptions.ActiveRules)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text("-");
                            }
                        }

                        place++;
                    }

                    ImGui.EndTable();
                }

            ImGui.PopID();
        }
    }
}