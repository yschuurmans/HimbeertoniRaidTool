﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Plugin.Services;

//Inspired by aka Copied from
//https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Tweaks/UiAdjustment/ExamineItemLevel.cs
internal static unsafe class GearRefresher
{
    private static readonly bool HookLoadSuccessful;
    private static readonly Hook<CharacterInspectOnRefresh>? Hook;
    private static readonly IntPtr HookAddress;
    private static readonly ExcelSheet<World>? WorldSheet;

    private delegate byte CharacterInspectOnRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* a3);

    static GearRefresher()
    {
        try
        {
            HookAddress = ServiceManager.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 48 8B F9 4D 85 C0 0F 84 ?? ?? ?? ?? 85 D2");
            Hook = Hook<CharacterInspectOnRefresh>.FromAddress(HookAddress, OnExamineRefresh);
            HookLoadSuccessful = true;
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, "Failed to hook into examine window");
            HookLoadSuccessful = false;
        }
        WorldSheet = ServiceManager.DataManager.GetExcelSheet<World>();
    }
    internal static void Enable()
    {
        if (HookLoadSuccessful && Hook is not null) Hook.Enable();
    }
    internal static void RefreshGearInfos(PlayerCharacter? @object)
    {
        if (@object is null)
            return;
        try
        {
            AgentInspect.Instance()->ExamineCharacter(@object.ObjectId);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, $"Could not inspect character {@object.Name}");
        }
    }


    private static byte OnExamineRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* loadingStage)
    {
        byte result = Hook!.Original(atkUnitBase, a2, loadingStage);
        if (loadingStage != null && a2 > 0)
        {
            if (loadingStage->UInt == 4)
            {
                GetItemInfos(atkUnitBase);
            }
        }
        return result;

    }

    private static void GetItemInfos(AtkUnitBase* examineWindow)
    {
        if (!HookLoadSuccessful)
            return;
        //Get Chracter Information from examine window
        //There are two possible fields for name/title depending on their order
        string charNameFromExamine = "";
        string charNameFromExamine2 = "";
        World? worldFromExamine;
        try
        {
            charNameFromExamine = examineWindow->UldManager.NodeList[60]->GetAsAtkTextNode()->NodeText.ToString();
            charNameFromExamine2 = examineWindow->UldManager.NodeList[59]->GetAsAtkTextNode()->NodeText.ToString();
            string worldString = examineWindow->UldManager.NodeList[57]->GetAsAtkTextNode()->NodeText.ToString();
            worldFromExamine = WorldSheet?.FirstOrDefault(x => x?.Name.RawString == worldString, null);
        }
        catch (Exception e)
        {
            PluginLog.Debug(e, "Exception while reading name / world from examine window");
            return;
        }
        if (worldFromExamine is null)
            return;
        //Make sure examine window correspods to intended character and character info is fetchable
        if (!ServiceManager.CharacterInfoService.TryGetChar(out var target, charNameFromExamine, worldFromExamine)
            && !ServiceManager.CharacterInfoService.TryGetChar(out target, charNameFromExamine2, worldFromExamine))
        {
            PluginLog.Debug($"Name + World from examine window didn't match any character in the area: " +
                $"Name1 {charNameFromExamine}, Name2 {charNameFromExamine2}, World {worldFromExamine?.Name}");
            return;
        }
        if (!ServiceManager.HrtDataManager.Ready)
        {
            PluginLog.Error($"Database is busy. Did not update gear for:{target.Name}@{target.HomeWorld.GameData?.Name}");
            return;
        }
        //Do not execute on characters not already known
        if (!ServiceManager.HrtDataManager.CharDB.SearchCharacter(target.HomeWorld.Id, target.Name.TextValue, out Character? targetChar))
        {
            PluginLog.Debug($"Did not find character in db:{target.Name}@{target.HomeWorld.GameData?.Name}");
            return;
        }
        //Save characters ContentID if not already known
        if (targetChar.CharID == 0)
        {
            PartyMember? p = ServiceManager.PartyList.FirstOrDefault(p => p?.ObjectId == target.ObjectId, null);
            if (p != null)
            {
                targetChar.CharID = Character.CalcCharID(p.ContentId);
            }
        }
        var targetJob = target.GetJob();
        if (!targetJob.IsCombatJob())
            return;
        var targetClass = targetChar[targetJob];
        if (targetClass == null)
        {
            if (!ServiceManager.HrtDataManager.Ready)
            {
                PluginLog.Error($"Database is busy. Did not update gear for:{targetChar.Name}@{targetChar.HomeWorld?.Name}");
                return;
            }
            targetClass = targetChar.AddClass(targetJob);
            ServiceManager.HrtDataManager.GearDB.AddSet(targetClass.Gear);
            ServiceManager.HrtDataManager.GearDB.AddSet(targetClass.BIS);
        }
        //Getting level does not work in level synced content
        if (target.Level > targetClass.Level)
            targetClass.Level = target.Level;
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Examine);
        UpdateGear(container, targetClass);
    }
    internal static void UpdateGear(InventoryContainer* container, PlayableClass targetClass)
    {
        try
        {
            for (int i = 0; i < 13; i++)
            {
                if (i == (int)GearSetSlot.Waist)
                    continue;
                var slot = container->GetInventorySlot(i);
                if (slot->ItemID == 0)
                    continue;
                targetClass.Gear[(GearSetSlot)i] = new(slot->ItemID)
                {
                    IsHq = slot->Flags.HasFlag(InventoryItem.ItemFlags.HQ)
                };
                for (int j = 0; j < 5; j++)
                {
                    if (slot->Materia[j] == 0)
                        break;
                    targetClass.Gear[(GearSetSlot)i].AddMateria(new((MateriaCategory)slot->Materia[j], (MateriaLevel)slot->MateriaGrade[j]));
                }
            }
            targetClass.Gear.TimeStamp = DateTime.UtcNow;
        }
        catch (Exception e)
        {
            PluginLog.Error(e, $"Something went wrong getting gear for:{targetClass.Parent?.Name}");
        }
    }
    public static void Dispose()
    {
        if (Hook is not null && !Hook.IsDisposed)
        {
            Hook.Disable();
            Hook.Dispose();
        }
    }
}
