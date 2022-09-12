﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ColorHelper;
using Dalamud.Interface;
using HimbeertoniRaidTool.Connectors;
using HimbeertoniRaidTool.Data;
using ImGuiNET;
using Lumina.Excel.Extensions;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    internal class EditPlayerWindow : HrtUI
    {
        private readonly RaidGroup RaidGroup;
        private readonly Player Player;
        private readonly Player PlayerCopy;
        private readonly AsyncTaskWithUiResult CallBack;
        private readonly bool IsNew;
        private static readonly string[] Worlds;
        private static readonly uint[] WorldIDs;
        private int newJob = 0;

        internal PositionInRaidGroup Pos => Player.Pos;

        static EditPlayerWindow()
        {
            List<(uint, string)> WorldList = new();
            for (uint i = 21; i < (Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.RowCount ?? 0); i++)
            {
                string? worldName = Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.GetRow(i)?.Name ?? "";
                if (!worldName.Equals("") && !worldName.Contains('-') && !worldName.Contains('_') && !worldName.Contains("contents"))
                    WorldList.Add((i, worldName));
            }
            Worlds = new string[WorldList.Count + 1];
            WorldIDs = new uint[WorldList.Count + 1];
            Worlds[0] = "";
            WorldIDs[0] = 0;
            for (int i = 0; i < WorldList.Count; i++)
                (WorldIDs[i + 1], Worlds[i + 1]) = WorldList[i];
        }
        internal EditPlayerWindow(out AsyncTaskWithUiResult callBack, RaidGroup group, PositionInRaidGroup pos) : base()
        {
            RaidGroup = group;
            callBack = CallBack = new();
            Player = group[pos];
            PlayerCopy = new();
            var target = Helper.TargetChar;
            IsNew = !Player.Filled;
            if (IsNew && target is not null)
            {
                PlayerCopy.MainChar.Name = target.Name.TextValue;
                PlayerCopy.MainChar.HomeWorldID = target.HomeWorld.Id;
                PlayerCopy.MainChar.MainJob = target.GetJob();
                //Ensure Main class is created if applicable
                var c = PlayerCopy.MainChar.MainClass;
            }
            else if (Player.Filled)
            {
                PlayerCopy = Player.Clone();
            }
        }

        protected override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(480, 210 + (27 * PlayerCopy.MainChar.Classes.Count)), ImGuiCond.Always);
            if (ImGui.Begin($"{Localize("Edit Player ", "Edit Player ")} {Player.NickName} ({RaidGroup.Name})##{Player.Pos}",
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
            {
                //Player Data
                ImGui.InputText(Localize("Player Name", "Player Name"), ref PlayerCopy.NickName, 50);
                //Character Data
                if (ImGui.InputText(Localize("Character Name", "Character Name"), ref PlayerCopy.MainChar.Name, 50))
                    PlayerCopy.MainChar.HomeWorldID = 0;
                int worldID = Array.IndexOf(WorldIDs, PlayerCopy.MainChar.HomeWorldID);
                if (worldID < 0 || worldID >= WorldIDs.Length)
                    worldID = 0;
                if (ImGui.Combo(Localize("HomeWorld", "Home World"), ref worldID, Worlds, Worlds.Length))
                    PlayerCopy.MainChar.HomeWorldID = WorldIDs[worldID] < 0 ? 0 : WorldIDs[worldID];
                if (Helper.TryGetChar(PlayerCopy.MainChar.Name) is not null)
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(Localize("Get", "Get"), Localize("FetchHomeworldTooltip", "Fetch home world information based on character name (from local players)")))
                        PlayerCopy.MainChar.HomeWorld = Helper.TryGetChar(PlayerCopy.MainChar.Name)?.HomeWorld.GameData;
                }
                //Class Data
                if (PlayerCopy.MainChar.Classes.Count > 0)
                {
                    int mainClass = PlayerCopy.MainChar.Classes.FindIndex(x => x.Job == PlayerCopy.MainChar.MainJob);
                    if (mainClass < 0 || mainClass >= PlayerCopy.MainChar.Classes.Count)
                    {
                        mainClass = 0;
                        PlayerCopy.MainChar.MainJob = PlayerCopy.MainChar.Classes[mainClass].Job;
                    }
                    if (ImGui.Combo(Localize("Main Job", "Main Job"), ref mainClass, PlayerCopy.MainChar.Classes.ConvertAll(x => x.Job.ToString()).ToArray(), PlayerCopy.MainChar.Classes.Count))
                        PlayerCopy.MainChar.MainJob = PlayerCopy.MainChar.Classes[mainClass].Job;
                }
                else
                {
                    PlayerCopy.MainChar.MainJob = null;
                    ImGui.NewLine();
                    ImGui.Text(Localize("NoClasses", "Character does not have any classes created"));
                }
                ImGui.Columns(2, "Classes", false);
                ImGui.SetColumnWidth(0, 70f);
                ImGui.SetColumnWidth(1, 400f);
                Job? toDelete = null;
                foreach (PlayableClass c in PlayerCopy.MainChar.Classes)
                {
                    if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"delete{c.Job}", $"Delete all data for {c.Job}"))
                        toDelete = c.Job;
                    ImGui.SameLine();
                    ImGui.Text($"{c.Job}  ");
                    ImGui.NextColumn();
                    ImGui.SetNextItemWidth(250f);
                    ImGui.InputText($"{Localize("BIS", "BIS")}##{c.Job}", ref c.BIS.EtroID, 50);
                    ImGui.SameLine();
                    if (ImGuiHelper.Button($"{Localize("Default", "Default")}##BIS#{c.Job}", Localize("DefaultBiSTooltip", "Fetch default BiS from configuration")))
                        c.BIS.EtroID = HRTPlugin.Configuration.GetDefaultBiS(c.Job);
                    ImGui.SameLine();
                    if (ImGuiHelper.Button($"{Localize("Reset", "Reset")}##BIS#{c.Job}", Localize("ResetBisTooltip", "Empty out BiS gear")))
                        c.BIS.EtroID = "";
                    ImGui.NextColumn();
                }
                if (toDelete is not null)
                    PlayerCopy.MainChar.Classes.RemoveAll(x => x.Job == toDelete);
                ImGui.Columns(1);

                var jobsNotUsed = new List<Job>(Enum.GetValues<Job>());
                jobsNotUsed.RemoveAll(x => PlayerCopy.MainChar.Classes.Exists(y => y.Job == x));
                var newJobs = jobsNotUsed.ConvertAll(x => x.ToString()).ToArray();
                ImGui.Combo(Localize("Add Job", "Add Job"), ref newJob, newJobs, newJobs.Length);
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddJob", "Add job"))
                {
                    PlayerCopy.MainChar.Classes.Add(new PlayableClass(jobsNotUsed[newJob], PlayerCopy.MainChar));
                }

                //Buttons
                if (ImGuiHelper.SaveButton(Localize("Save Player", "Save Player")))
                {
                    SavePlayer();
                    Hide();
                }
                ImGui.SameLine();
                if (ImGuiHelper.CancelButton())
                    Hide();
            }
            ImGui.End();
        }
        private void SavePlayer()
        {
            List<(Job, Func<bool>)> bisUpdates = new();
            Player.NickName = PlayerCopy.NickName;
            if (IsNew)
            {
                Character c = new Character(PlayerCopy.MainChar.Name, PlayerCopy.MainChar.HomeWorldID);
                DataManagement.DataManager.GetManagedCharacter(ref c);
                Player.MainChar = c;
                if (c.Classes.Count > 0)
                    return;
            }
            if (Player.MainChar.Name != PlayerCopy.MainChar.Name || Player.MainChar.HomeWorldID != PlayerCopy.MainChar.HomeWorldID)
            {
                uint oldWorld = Player.MainChar.HomeWorldID;
                string oldName = Player.MainChar.Name;
                Player.MainChar.Name = PlayerCopy.MainChar.Name;
                Player.MainChar.HomeWorldID = PlayerCopy.MainChar.HomeWorldID;
                Character c = Player.MainChar;
                DataManagement.DataManager.RearrangeCharacter(oldWorld, oldName, ref c);
                Player.MainChar = c;
            }
            Player.MainChar.MainJob = PlayerCopy.MainChar.MainJob;
            for (int i = 0; i < Player.MainChar.Classes.Count; i++)
            {
                PlayableClass c = Player.MainChar.Classes[i];
                if (!PlayerCopy.MainChar.Classes.Exists(x => x.Job == c.Job))
                {
                    Player.MainChar.Classes.Remove(c);
                    i--;
                }
            }
            foreach (PlayableClass c in PlayerCopy.MainChar.Classes)
            {
                PlayableClass target = Player.MainChar.GetClass(c.Job);
                if (target.BIS.EtroID.Equals(c.BIS.EtroID))
                    continue;
                GearSet set = new()
                {
                    ManagedBy = GearSetManager.Etro,
                    EtroID = c.BIS.EtroID
                };
                if (!set.EtroID.Equals(""))
                {
                    DataManagement.DataManager.GearDB.AddOrGetSet(ref set);
                    bisUpdates.Add((target.Job, () => EtroConnector.GetGearSet(target.BIS)));
                }
                target.BIS = set;
            }
            if (bisUpdates.Count > 0)
            {
                CallBack.Action =
                    (t) =>
                    {
                        string success = "";
                        string error = "";
                        List<(Job, bool)> results = ((Task<List<(Job, bool)>>)t).Result;
                        foreach (var result in results)
                        {
                            if (result.Item2)
                                success += result.Item1 + ",";
                            else
                                error += result.Item1 + ",";
                        }
                        if (success.Length > 0)
                            ImGui.TextColored(HRTColorConversions.Vec4(ColorName.Green),
                                    $"BIS for Character {Player.MainChar.Name} on classes ({success[0..^1]}) succesfully updated");

                        if (error.Length > 0)
                            ImGui.TextColored(HRTColorConversions.Vec4(ColorName.Green),
                                    $"BIS update for Character {Player.MainChar.Name} on classes ({error[0..^1]}) failed");
                    };
                CallBack.Task = Task.Run(() => bisUpdates.ConvertAll((x) => (x.Item1, x.Item2.Invoke())));

            }
        }
        public override bool Equals(object? obj)
        {
            if (!(obj?.GetType().IsAssignableTo(GetType()) ?? false))
                return false;
            return Equals((EditPlayerWindow)obj);
        }
        public bool Equals(EditPlayerWindow other)
        {
            if (!RaidGroup.Equals(other.RaidGroup))
                return false;
            if (Pos != other.Pos)
                return false;

            return true;
        }
        public override int GetHashCode()
        {
            return RaidGroup.GetHashCode() << 3 + (int)Pos;
        }
    }
    internal class EditGroupWindow : HrtUI
    {
        private readonly RaidGroup Group;
        private readonly RaidGroup GroupCopy;
        private readonly Action OnSave;
        private readonly Action OnCancel;

        internal EditGroupWindow(RaidGroup group, Action? onSave = null, Action? onCancel = null)
        {
            Group = group;
            OnSave = onSave ?? (() => { });
            OnCancel = onCancel ?? (() => { });
            GroupCopy = Group.Clone();
            Show();
        }

        protected override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(500, 250), ImGuiCond.Always);
            if (ImGui.Begin(Localize("Edit Group ", "Edit Group ") + Group.Name,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.InputText(Localize("Group Name", "Group Name"), ref GroupCopy.Name, 100);
                int groupType = (int)GroupCopy.Type;
                if (ImGui.Combo(Localize("Group Type", "Group Type"), ref groupType, Enum.GetNames(typeof(GroupType)), Enum.GetNames(typeof(GroupType)).Length))
                {
                    GroupCopy.Type = (GroupType)groupType;
                }
                if (ImGuiHelper.SaveButton())
                {
                    Group.Name = GroupCopy.Name;
                    Group.Type = GroupCopy.Type;
                    OnSave();
                    Hide();
                }
                ImGui.SameLine();
                if (ImGuiHelper.CancelButton())
                {
                    OnCancel();
                    Hide();
                }
                ImGui.End();
            }
        }
    }
    internal class EditGearSetWindow : HrtUI
    {
        private readonly GearSet _gearSet;
        private readonly GearSet _gearSetCopy;
        private readonly bool _canHaveShield;

        internal EditGearSetWindow(GearSet original, bool canHaveShield) : base()
        {
            _canHaveShield = canHaveShield;
            _gearSet = original;
            _gearSetCopy = original.Clone();
        }

        protected override void Draw()
        {
            if (ImGui.Begin($"{Localize("Edit", "Edit")} {(_gearSet.ManagedBy == GearSetManager.HRT ? _gearSet.HrtID : _gearSet.EtroID)}", ref Visible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ImGuiHelper.SaveButton())
                    Save();
                ImGui.SameLine();
                if (ImGuiHelper.CancelButton())
                    Hide();
                ImGui.BeginTable("SoloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
                ImGui.TableSetupColumn("Gear");
                ImGui.TableSetupColumn("Gear");
                ImGui.TableHeadersRow();
                DrawSlot(_gearSetCopy.MainHand);
                if (_canHaveShield)
                    DrawSlot(_gearSetCopy.OffHand);
                else
                    ImGui.TableNextColumn();
                DrawSlot(_gearSetCopy.Head);
                DrawSlot(_gearSetCopy.Ear);
                DrawSlot(_gearSetCopy.Body);
                DrawSlot(_gearSetCopy.Neck);
                DrawSlot(_gearSetCopy.Hands);
                DrawSlot(_gearSetCopy.Wrist);
                DrawSlot(_gearSetCopy.Legs);
                DrawSlot(_gearSetCopy.Ring1);
                DrawSlot(_gearSetCopy.Feet);
                DrawSlot(_gearSetCopy.Ring2);
                ImGui.EndTable();

                ImGui.End();
            }
        }
        private void DrawSlot(GearItem item)
        {
            ImGui.TableNextColumn();
            if (item.Filled && item.Item is not null)
            {
                ImGui.BeginGroup();
                ImGui.Text(item.Item.Name.RawString);
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{item.Slot}changeitem", null))
                    AddChild(new GetGearWindow(item.Slot, x => { _gearSetCopy[item.Slot] = x; }, (x) => { }));
                for (int i = 0; i < item.Materia.Count; i++)
                {
                    if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"Delete{item.Slot}mat{i}", Localize("Remove this materia", "Remove this materia"), i == item.Materia.Count - 1))
                    {
                        item.Materia.RemoveAt(i);
                        i--;
                        continue;
                    }
                    ImGui.SameLine();
                    ImGui.Text(item.Materia[i].Item?.Name.RawString);
                }
                if (item.Materia.Count < (item.Item.IsAdvancedMeldingPermitted ? 5 : item.Item.MateriaSlotCount))
                    if (ImGuiHelper.Button(FontAwesomeIcon.Plus, $"{item.Slot}addmat", null))
                        AddChild(new GetMateriaWindow(x => item.Materia.Add(x), (x) => { }));

                ImGui.EndGroup();
            }
            else
                ImGuiHelper.Button(FontAwesomeIcon.Plus, $"Add{item.Slot}", null);


        }
        private void Save()
        {
            _gearSetCopy.TimeStamp = DateTime.Now;
            _gearSet.CopyFrom(_gearSetCopy);
            Hide();
        }
    }
    internal abstract class GetItemWindow<T> : HrtUI where T : HrtItem
    {
        private Guid _Guid = Guid.NewGuid();
        protected T? Item = null;
        private readonly Action<T> OnSave;
        private readonly Action<T?> OnCancel;
        protected string Title = "";
        protected virtual bool CanSave { get; set; } = true;
        internal GetItemWindow(Action<T> onSave, Action<T?> onCancel)
        {
            (OnSave, OnCancel) = (onSave, onCancel);
        }


        protected override void Draw()
        {
            if (ImGui.Begin($"{Title}##{_Guid}", ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ImGuiHelper.SaveButton(null, CanSave))
                {
                    if (Item != null)
                        OnSave(Item);
                    else
                        OnCancel(Item);
                    Hide();
                }
                ImGui.SameLine();
                if (ImGuiHelper.CancelButton())
                {
                    OnCancel(Item);
                    Hide();
                }
                DrawItemSelection();
                ImGui.End();
            }
        }

        protected abstract void DrawItemSelection();
    }
    internal class GetGearWindow : GetItemWindow<GearItem>
    {
        private static readonly Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item> Sheet = Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()!;
        private readonly GearSetSlot Slot;
        private uint minILvl;
        private uint maxILvl;
        private IEnumerable<Lumina.Excel.GeneratedSheets.Item> _items;
        public GetGearWindow(GearSetSlot slot, Action<GearItem> onSave, Action<GearItem?> onCancel) : base(onSave, onCancel)
        {
            Slot = slot;
            Title = $"{Localize("Get", "Get")} {Slot} {Localize("item", "item")}";
            maxILvl = Slot is GearSetSlot.MainHand or GearSetSlot.OffHand ? HRTPlugin.Configuration.SelectedRaidTier.WeaponItemLevel : HRTPlugin.Configuration.SelectedRaidTier.ArmorItemLevel;
            minILvl = maxILvl - 20;
            _items = reevaluateItems();
        }

        protected override void DrawItemSelection()
        {
            ImGui.Text($"{Localize("Current Item", "Current Item")}:{Item?.Name ?? Localize("Empty", "Empty")}");
            int min = (int)minILvl;
            if (ImGui.InputInt("Min", ref min))
            {
                minILvl = (uint)min;
                reevaluateItems();
            }
            ImGui.SameLine();
            int max = (int)maxILvl;
            if (ImGui.InputInt("Max", ref max))
            {
                maxILvl = (uint)max;
                reevaluateItems();
            }
            foreach (var item in _items)
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, $"{item.RowId}", null))
                    Item = new(item.RowId);
                ImGui.SameLine();
                ImGui.Text(item.Name.RawString);
            }
        }
        private IEnumerable<Lumina.Excel.GeneratedSheets.Item> reevaluateItems()
        {
            return _items = Sheet.Where(x => x.EquipSlotCategory.Value?.ToSlot() == Slot && x.LevelItem.Row <= maxILvl && x.LevelItem.Row >= minILvl);
        }
    }
    internal class GetMateriaWindow : GetItemWindow<HrtMateria>
    {
        private MateriaCategory Cat;
        private byte MateriaLevel;
        private int numMatLEvels;
        protected override bool CanSave => Cat != MateriaCategory.None;
        public GetMateriaWindow(Action<HrtMateria> onSave, Action<HrtMateria?> onCancel) : base(onSave, onCancel)
        {
            Cat = MateriaCategory.None;
            MateriaLevel = HRTPlugin.Configuration.SelectedRaidTier.MaxMateriaLevel;
            numMatLEvels = MateriaLevel + 1;
            Title = Localize("", "");
        }

        protected override void DrawItemSelection()
        {
            int catSlot = Array.IndexOf(Enum.GetValues<MateriaCategory>(), Cat);
            if (ImGui.Combo($"{Localize("Type", "Type")}##Category", ref catSlot, Enum.GetNames<MateriaCategory>(), Enum.GetValues<MateriaCategory>().Length))
            {
                Cat = Enum.GetValues<MateriaCategory>()[catSlot];
                if (Cat != MateriaCategory.None)
                    Item = new(Cat, MateriaLevel);
            }

            int level = MateriaLevel;
            if (ImGui.Combo($"{Localize("Tier", "Tier")}##Level", ref level, Array.ConvertAll(Enumerable.Range(1, numMatLEvels).ToArray(), x => x.ToString()), numMatLEvels))
            {
                MateriaLevel = (byte)level;
                if (Cat != MateriaCategory.None)
                    Item = new(Cat, MateriaLevel);
            }

        }
    }
}
