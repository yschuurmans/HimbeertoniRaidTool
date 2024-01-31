﻿using System.Globalization;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Calculations;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.Services.Localization;
using ServiceManager = HimbeertoniRaidTool.Plugin.Services.ServiceManager;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class LootRuling
{
    public static readonly LootRule Default = new(LootRuleEnum.None);
    public static readonly LootRule NeedOverGreed = new(LootRuleEnum.NeedGreed);
    [JsonProperty("RuleSet")]
    public List<LootRule> RuleSet = new();
    public static IEnumerable<LootRule> PossibleRules
    {
        get
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (LootRuleEnum rule in Enum.GetValues(typeof(LootRuleEnum)))
            {
#pragma warning disable CS0618
                if (rule is LootRuleEnum.None or LootRuleEnum.Dps)
#pragma warning restore CS0618
                    continue;
                //Special Rules only used internally
                if ((int)rule > 900)
                    continue;
                yield return new LootRule(rule);
            }
        }
    }

    [JsonIgnore]
    public IEnumerable<LootRule> ActiveRules => RuleSet.Where(r => r.Active);
}

[JsonObject(MemberSerialization.OptIn)]
public class LootRule : IEquatable<LootRule>, IDrawable
{
    [JsonProperty("Rule")] public readonly LootRuleEnum Rule;

    [JsonProperty("Active")] public bool Active = true;

    [JsonProperty("IgnoreActive")] public bool IgnorePlayers;
    [JsonConstructor]
    public LootRule(LootRuleEnum rule)
    {
        Rule = rule;
    }

    public bool CanIgnore =>
        Rule switch
        {
            LootRuleEnum.BisOverUpgrade => true,
            LootRuleEnum.CanUse         => true,
            LootRuleEnum.CanBuy         => true,
            LootRuleEnum.NeedGreed      => true,
            _                           => false,
        };

    public string Name => GetName();

    private string IgnoreTooltip =>
        Rule switch
        {
            LootRuleEnum.BisOverUpgrade => Localize("loot_rule:ignore:tooltip:bis",
                                                    "Ignore players/jobs not using this in BiS "),
            LootRuleEnum.CanUse => Localize("loot_rule:ignore:tooltip:can_use", "Ignore players/jobs not able to use"),
            LootRuleEnum.CanBuy => Localize("loot_rule:ignore:tooltip:can_buy",
                                            "Ignore players/jobs that could buy these"),
            LootRuleEnum.NeedGreed => Localize("loot_rule:ignore:tooltip:need_greed",
                                               "Ignore players that have no need"),
            _ => "",
        };

    public void Draw()
    {
        ImGui.Checkbox("##active", ref Active);
        ImGuiHelper.AddTooltip(Localize("ui:loot_rule:active:tooltip", "Activated"));
        ImGui.SameLine();
        ImGui.BeginDisabled(!Active);
        ImGui.Text(Name);
        if (CanIgnore)
        {
            ImGui.SameLine();
            ImGui.Checkbox($"{Localize("ui:loot_rule:ignore", "Ignore")}##ignore", ref IgnorePlayers);
            ImGuiHelper.AddTooltip(IgnoreTooltip);
        }
        ImGui.EndDisabled();
    }
    public bool Equals(LootRule? obj) => obj?.Rule == Rule;

    /// <summary>
    ///     Evaluates this LootRule for given player
    /// </summary>
    /// <param name="x">The player to evaluate for</param>
    /// <returns>
    ///     A tuple of int (can be used for Compare like (right - left)) and a string describing the
    ///     value
    /// </returns>
    public (float, string) Eval(LootResult x)
    {
        (float val, string? reason) = InternalEval(x);
        return (val, reason ?? val.ToString(CultureInfo.CurrentCulture));
    }

    public bool ShouldIgnore(LootResult x) => CanIgnore && IgnorePlayers && Rule switch
    {
        LootRuleEnum.BisOverUpgrade => !x.IsBiS(),
        LootRuleEnum.CanUse         => !x.CanUse(),
        LootRuleEnum.CanBuy         => x.CanBuy(),
        LootRuleEnum.Greed          => true,
        _                           => false,
    };

    private (float, string?) InternalEval(LootResult x) => Rule switch
    {
        LootRuleEnum.Random               => (x.Roll(), null),
        LootRuleEnum.LowestItemLevel      => (-x.ItemLevel(), x.ItemLevel().ToString()),
        LootRuleEnum.HighestItemLevelGain => (x.ItemLevelGain(), null),
        LootRuleEnum.BisOverUpgrade       => x.IsBiS() ? (1, "y") : (-1, "n"),
        LootRuleEnum.RolePrio             => (x.RolePriority(), x.ApplicableJob.Role.ToString()),
        LootRuleEnum.DpsGain              => (x.DpsGain(), $"{x.DpsGain() * 100:f1} %%"),
        LootRuleEnum.CanUse               => x.CanUse() ? (1, "y") : (-1, "n"),
        LootRuleEnum.CanBuy               => x.CanBuy() ? (-1, "y") : (1, "n"),
        _                                 => (0, "none"),
    };
    public override string ToString() => Name;
    private string GetName() => Rule switch
    {
        LootRuleEnum.BisOverUpgrade       => Localize("BISOverUpgrade", "BIS > Upgrade"),
        LootRuleEnum.LowestItemLevel      => Localize("LowestItemLevel", "Lowest overall ItemLevel"),
        LootRuleEnum.HighestItemLevelGain => Localize("HighestItemLevelGain", "Highest ItemLevel Gain"),
        LootRuleEnum.RolePrio             => Localize("ByRole", "Prioritize by role"),
        LootRuleEnum.Random               => Localize("Rolling", "Rolling"),
        LootRuleEnum.DpsGain              => Localize("DPSGain", "% DPS gained"),
        LootRuleEnum.CanUse               => Localize("LootRule:CanUse", "Can use now"),
        LootRuleEnum.CanBuy               => Localize("LootRule:CanBuy", "Can buy"),
        LootRuleEnum.None                 => Localize(GeneralLoc.None, GeneralLoc.None),
        LootRuleEnum.Greed                => Localize("Greed", "Greed"),
        LootRuleEnum.NeedGreed            => Localize("Need over Greed", "Need over Greed"),
        _                                 => Localize("Not defined", "Not defined"),
    };

    public override int GetHashCode() => Rule.GetHashCode();
    public override bool Equals(object? obj) => Equals(obj as LootRule);
    public static bool operator ==(LootRule l, LootRule r) => l.Equals(r);
    public static bool operator !=(LootRule l, LootRule r) => !l.Equals(r);
}

public static class LootRulesExtension
{
    public static int RolePriority(this LootResult result) => -result.RolePriority;
    public static int Roll(this LootResult result) => result.Roll;
    public static int ItemLevel(this LootResult result) => result.ApplicableJob.CurGear.ItemLevel;
    public static int ItemLevelGain(this LootResult result) => result.NeededItems.Select(
        item => (int)item.ItemLevel - result.ApplicableJob.CurGear
                                            .Where(i => i.Slots.Intersect(item.Slots).Any())
                                            .Aggregate((int)item.ItemLevel,
                                                       (min, i) => Math.Min((int)i.ItemLevel, min))).Prepend(0).Max();
    public static float DpsGain(this LootResult result)
    {
        PlayableClass curClass = result.ApplicableJob;
        double baseDps = AllaganLibrary.EvaluateStat(StatType.PhysicalDamage, curClass, curClass.CurGear,
                                                     result.Player.MainChar.Tribe);
        double newDps = double.NegativeInfinity;
        foreach (GearItem? i in result.ApplicableItems)
        {
            GearItem? item = null;
            foreach (GearItem? bisItem in curClass.CurBis)
            {
                if (bisItem.Equals(i, ItemComparisonMode.IdOnly))
                    item = bisItem.Clone();
            }
            if (item is null)
            {
                item ??= i.Clone();
                foreach (HrtMateria? mat in curClass.CurGear[i.Slots.First()].Materia)
                {
                    item.AddMateria(mat);
                }
            }
            double cur = AllaganLibrary.EvaluateStat(StatType.PhysicalDamage, curClass, curClass.CurGear.With(item),
                                                     result.Player.MainChar.Tribe);
            if (cur > newDps)
                newDps = cur;
        }
        return (float)((newDps - baseDps) / baseDps);
    }
    public static bool IsBiS(this LootResult result) =>
        result.NeededItems.Any(i => result.ApplicableJob.CurBis.Count(x => x.Equals(i, ItemComparisonMode.IdOnly))
                                 != result.ApplicableJob.CurGear.Count(x => x.Equals(i, ItemComparisonMode.IdOnly)));
    public static bool CanUse(this LootResult result) =>
        //Direct gear or coffer drops are always usable
        !result.DroppedItem.IsExchangableItem
     || result.NeededItems.Any(
            item => ServiceManager.ItemInfo.GetShopEntriesForItem(item.Id).Any(shopEntry =>
            {
                for (int i = 0; i < SpecialShop.NUM_COST; i++)
                {
                    SpecialShop.ItemCostEntry cost = shopEntry.entry.ItemCostEntries[i];
                    if (cost.Count == 0) continue;
                    if (cost.Item.Row == result.DroppedItem.Id) continue;
                    if (ItemInfo.IsCurrency(cost.Item.Row)) continue;
                    if (ItemInfo.IsTomeStone(cost.Item.Row)) continue;
                    if (result.ApplicableJob.CurGear.Contains(new HrtItem(cost.Item.Row))) continue;
                    if (result.Player.MainChar.MainInventory.ItemCount(cost.Item.Row) >= cost.Count) continue;
                    return false;
                }
                return true;
            })
        );

    public static bool CanBuy(this LootResult result) => ServiceManager.ItemInfo
                                                                       .GetShopEntriesForItem(result.DroppedItem.Id)
                                                                       .Any(
                                                                           shopEntry =>
                                                                           {
                                                                               for (int i = 0;
                                                                                i < SpecialShop.NUM_COST;
                                                                                i++)
                                                                               {
                                                                                   SpecialShop.ItemCostEntry cost =
                                                                                       shopEntry.entry.ItemCostEntries[
                                                                                           i];
                                                                                   if (cost.Count == 0) continue;
                                                                                   if (ItemInfo.IsCurrency(
                                                                                       cost.Item.Row)) continue;
                                                                                   if (ItemInfo.IsTomeStone(
                                                                                       cost.Item.Row)) continue;
                                                                                   if (result.ApplicableJob.CurGear
                                                                                    .Contains(
                                                                                        new HrtItem(cost.Item.Row)))
                                                                                       continue;
                                                                                   if (result.Player.MainChar
                                                                                        .MainInventory
                                                                                        .ItemCount(cost.Item.Row)
                                                                                  + (result.GuaranteedLoot.Any(
                                                                                        loot => loot.Id
                                                                                         == cost.Item.Row) ? 1 : 0)
                                                                                 >= cost.Count) continue;
                                                                                   return false;
                                                                               }
                                                                               return true;
                                                                           }
                                                                       );
}