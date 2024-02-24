﻿using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class EditWindowFactory
{
    private static HrtDataManager DataManager => ServiceManager.HrtDataManager;
    public static HrtWindowWithModalChild? Create<T>(HrtId id, Action<T>? onSave = null, Action<T>? onCancel = null,
                                                     object? param = null)
        where T : IHasHrtId, new()
    {
        HrtId.IdType type = new T().IdType;
        if (id.Type != type) return null;
        switch (type)
        {
            case HrtId.IdType.Group:
                if (DataManager.RaidGroupDb.TryGet(id, out RaidGroup? group))
                    return Create(group, onSave as Action<RaidGroup>, onCancel as Action<RaidGroup>, param);
                break;
            case HrtId.IdType.Player:
                if (DataManager.PlayerDb.TryGet(id, out Player? player))
                    return Create(player, onSave as Action<Player>, onCancel as Action<Player>, param);
                break;
            case HrtId.IdType.Character:
                if (DataManager.CharDb.TryGet(id, out Character? character))
                    return Create(character, onSave as Action<Character>, onCancel as Action<Character>, param);
                break;
            case HrtId.IdType.Gear:
                if (DataManager.GearDb.TryGet(id, out GearSet? gearSet))
                    return Create(gearSet, onSave as Action<GearSet>, onCancel as Action<GearSet>, param);
                break;
            case HrtId.IdType.None:
            default:
                return null;
        }
        return null;
    }
    public static HrtWindowWithModalChild? Create<S>(S data, Action<S>? onSave = null, Action<S>? onCancel = null,
                                                     object? param = null)
        where S : IHasHrtId => data.IdType switch
    {
        HrtId.IdType.Player when data is Player p => new EditPlayerWindow(p, onSave as Action<Player>,
                                                                          onCancel as Action<Player>),
        HrtId.IdType.Character when data is Character c => new EditCharacterWindow(c, onSave as Action<Character>,
            onCancel as Action<Character>),
        HrtId.IdType.Gear when data is GearSet gs => new EditGearSetWindow(gs, onSave as Action<GearSet>,
                                                                           onCancel as Action<GearSet>, param as Job?),
        HrtId.IdType.Group when data is RaidGroup rg => new EditGroupWindow(rg, onSave as Action<RaidGroup>,
                                                                            onCancel as Action<RaidGroup>),
        HrtId.IdType.None => null,
        _                 => null,
    };

    private abstract class EditWindow<T> : HrtWindowWithModalChild where T : IHrtDataTypeWithId
    {
        private readonly Action<T>? _onCancel;

        private readonly Action<T>? _onSave;

        private T _original;
        protected T DataCopy;

        protected EditWindow(T original, Action<T>? onSave, Action<T>? onCancel)
        {
            _original = original;
            DataCopy = _original.Clone();
            _onCancel = onCancel;
            _onSave = onSave;
            Title = string.Format(GeneralLoc.EditUi_Title, _original.DataTypeName, _original.Name).CapitaliezSentence();
        }
        public override sealed void Draw()
        {
            ImGui.Text($"{GeneralLoc.EditUi_Txt_LocalId}: {(_original.LocalId.IsEmpty ? "-" : _original.LocalId)}");
            //Buttons
            if (ImGuiHelper.SaveButton())
            {
                Save(_original);
                _onSave?.Invoke(_original);
                Hide();
            }

            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
            {
                Cancel();
                _onCancel?.Invoke(_original);
                Hide();
            }
            DrawContent();
        }
        protected abstract void DrawContent();
        protected void Save() => Save(_original);

        protected void ReplaceOriginal(T newOrg)
        {
            _original = newOrg;
            DataCopy = _original.Clone();
        }

        protected abstract void Save(T destination);
        protected abstract void Cancel();
    }

    private class EditGroupWindow : EditWindow<RaidGroup>
    {

        internal EditGroupWindow(RaidGroup group, Action<RaidGroup>? onSave = null,
                                 Action<RaidGroup>? onCancel = null) :
            base(group, onSave, onCancel)
        {
            Size = new Vector2(500, 170 + (group.RolePriority != null ? 180 : 0));
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Save(RaidGroup destination)
        {
            destination.Name = DataCopy.Name;
            destination.Type = DataCopy.Type;
            destination.RolePriority = DataCopy.RolePriority;
            if (destination.LocalId.IsEmpty) ServiceManager.HrtDataManager.RaidGroupDb.TryAdd(destination);
            ServiceManager.HrtDataManager.Save();
        }
        protected override void Cancel() { }

        protected override void DrawContent()
        {
            //Name + Type
            ImGui.InputText(GeneralLoc.CommonTerms_Name, ref DataCopy.Name, 100);
            ImGui.BeginDisabled(DataCopy.TypeLocked);
            ImGuiHelper.Combo(GeneralLoc.EditGroupUi_in_type, ref DataCopy.Type);
            ImGui.EndDisabled();
            //Role priority
            bool overrideRolePriority = DataCopy.RolePriority != null;
            if (ImGui.Checkbox(GeneralLoc.EditGroupUi_cb_OverrideRolePriority, ref overrideRolePriority))
            {
                DataCopy.RolePriority = overrideRolePriority ? new RolePriority() : null;
                Vector2 curSize = ImGui.GetWindowSize();
                Resize(curSize with
                {
                    Y = curSize.Y + (overrideRolePriority ? 1 : -1) * 180f * ScaleFactor,
                });
            }

            if (overrideRolePriority)
            {
                ImGui.Text(LootmasterLoc.ConfigUi_hdg_RolePriority);
                ImGui.Text($"{LootmasterLoc.ConfigUi_txt_currentPrio}: {DataCopy.RolePriority}");
                DataCopy.RolePriority?.DrawEdit(ImGui.InputInt);
            }
        }
    }

    private class EditPlayerWindow : EditWindow<Player>
    {
        internal EditPlayerWindow(Player p, Action<Player>? onSave = null, Action<Player>? onCancel = null)
            : base(p, onSave, onCancel)
        {
            Size = new Vector2(450, 250);
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Cancel() { }

        protected override void DrawContent()
        {
            //Player Data
            ImGui.SetCursorPosX(
                (ImGui.GetWindowWidth() - ImGui.CalcTextSize(GeneralLoc.EditPlayerUi_hdg_playerData).X) / 2f);
            ImGui.Text(GeneralLoc.EditPlayerUi_hdg_playerData);
            ImGui.InputText(GeneralLoc.CommonTerms_Name, ref DataCopy.NickName, 50);
            //Character Data
            ImGui.SetCursorPosX(
                (ImGui.GetWindowWidth() - ImGui.CalcTextSize(GeneralLoc.EditPlayerUi_hdg_characterData).X) / 2f);
            ImGui.Text(GeneralLoc.EditPlayerUi_hdg_characterData);
            Character mainChar = DataCopy.MainChar;
            foreach (Character character in DataCopy.Characters)
            {
                ImGui.PushID(character.LocalId.ToString());
                if (ImGuiHelper.DeleteButton(character, "##delete"))
                    DataCopy.RemoveCharacter(character);
                ImGui.SameLine();
                if (ImGuiHelper.EditButton(character, "##edit"))
                    AddChild(Create<Character>(character.LocalId));

                ImGui.SameLine();
                bool isMain = mainChar.Equals(character);
                if (isMain) ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
                if (ImGuiHelper.Button(
                        isMain ? GeneralLoc.EditPlayerUi_btn_IsMain : GeneralLoc.EditPlayerUi_btn_MakeMain,
                        null))
                    DataCopy.MainChar = character;
                if (isMain) ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.Text($"{character}");
                ImGui.PopID();
            }
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "addEmpty",
                                   string.Format(GeneralLoc.Ui_btn_tt_addEmpty, Character.DataTypeNameStatic)))
            {
                AddChild(Create(new Character(), c =>
                {
                    if (ServiceManager.HrtDataManager.CharDb.TryAdd(c))
                        DataCopy.AddCharacter(c);
                }));
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "addFromDb",
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting, Character.DataTypeNameStatic)))
            {
                AddChild(ServiceManager.HrtDataManager.CharDb.OpenSearchWindow(c => DataCopy.AddCharacter(c)));
            }
        }

        protected override void Save(Player destination)
        {
            if (destination.LocalId.IsEmpty)
            {
                ServiceManager.HrtDataManager.PlayerDb.TryAdd(destination);
            }
            //Player Data
            destination.NickName = DataCopy.NickName;
            //Characters
            var toRemove = destination.Characters.Where(c => !DataCopy.Characters.Contains(c));
            foreach (Character c in toRemove)
            {
                destination.RemoveCharacter(c);
            }
            var toAdd = DataCopy.Characters.Where(c => !destination.Characters.Contains(c));
            foreach (Character c in toAdd)
            {
                if (c.LocalId.IsEmpty)
                    if (!ServiceManager.HrtDataManager.CharDb.TryAdd(c))
                        continue;
                destination.AddCharacter(c);
            }
            destination.MainChar = DataCopy.MainChar;
            if (destination.MainChar.LocalId.IsEmpty) ServiceManager.HrtDataManager.CharDb.TryAdd(destination.MainChar);
        }
    }

    private class EditCharacterWindow : EditWindow<Character>
    {
        private const int CLASS_HEIGHT = 27 + 4;
        private Job _newJob = Job.ADV;
        internal EditCharacterWindow(Character character, Action<Character>? onSave = null,
                                     Action<Character>? onCancel = null) : base(character, onSave, onCancel)
        {
            Size = new Vector2(500, 295 + CLASS_HEIGHT * DataCopy.Classes.Count());
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Cancel() { }

        protected override void DrawContent()
        {
            ImGui.SetCursorPosX((ImGui.GetWindowWidth()
                               - ImGui.CalcTextSize(GeneralLoc.EditPlayerUi_hdg_characterData).X) / 2f);
            ImGui.Text(GeneralLoc.EditPlayerUi_hdg_characterData);
            if (ImGui.InputText(GeneralLoc.CommonTerms_Name, ref DataCopy.Name, 50)
             && ServiceManager.CharacterInfoService.TryGetChar(out PlayerCharacter? pc, DataCopy.Name))
                DataCopy.HomeWorld ??= pc.HomeWorld.GameData;
            if (ImGuiHelper.ExcelSheetCombo(GeneralLoc.EditCharUi_in_HomeWorld + "##" + Title, out World? w,
                                            _ => DataCopy.HomeWorld?.Name.RawString ?? "",
                                            x => x.Name.RawString, x => x.IsPublic))
                DataCopy.HomeWorld = w;

            //ImGuiHelper.Combo(Localize("Gender", "Gender"), ref PlayerCopy.MainChar.Gender);
            string GetGenderedTribeName(Tribe? tribe)
            {
                return (DataCopy.Gender == Gender.Male ? tribe?.Masculine.RawString : tribe?.Feminine.RawString) ??
                       string.Empty;
            }

            if (ImGuiHelper.ExcelSheetCombo(GeneralLoc.CommonTerms_Tribe + "##" + Title, out Tribe? t,
                                            _ => GetGenderedTribeName(DataCopy.Tribe), GetGenderedTribeName))
                DataCopy.TribeId = t.RowId;
            //Class Data
            ImGui.SetCursorPosX((ImGui.GetWindowWidth()
                               - ImGui.CalcTextSize(GeneralLoc.EditCharUi_hdg_JobData).X) / 2f);
            ImGui.Text(GeneralLoc.EditCharUi_hdg_JobData);
            if (DataCopy.Classes.Any())
            {

                if (DataCopy.Classes.All(x => x.Job != DataCopy.MainJob))
                    DataCopy.MainJob = DataCopy.Classes.First().Job;
                if (ImGui.BeginCombo(GeneralLoc.EditCharUi_in_mainJob, DataCopy.MainJob.ToString()))
                    foreach (PlayableClass curJob in DataCopy)
                    {
                        if (ImGui.Selectable(curJob.Job.ToString()))
                            DataCopy.MainJob = curJob.Job;
                    }
                ImGui.Separator();
                ImGui.TextColored(Colors.TextSoftRed, GeneralLoc.EditCharUi_text_BisNotice);
            }
            else
            {
                DataCopy.MainJob = null;
                ImGui.NewLine();
                ImGui.Text(GeneralLoc.EditCharUi_text_noJobs);
            }

            ImGui.Separator();
            Job? toDelete = null;
            foreach (PlayableClass c in DataCopy.Classes)
            {
                ImGui.PushID(c.Job.ToString());

                if (ImGuiHelper.DeleteButton(c, "##delete"))
                    toDelete = c.Job;
                ImGui.SameLine();
                ImGui.Text($"{c.Job}  ");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                if (ImGui.InputInt(GeneralLoc.CommonTerms_Level, ref c.Level))
                    c.Level = Math.Clamp(c.Level, 1, Common.Services.ServiceManager.GameInfo.CurrentExpansion.MaxLevel);
                ImGui.SameLine();
                ImGui.Checkbox(GeneralLoc.EditCharUi_cb_hideJob, ref c.HideInUi);
                ImGui.Separator();
                ImGui.PopID();
            }
            if (toDelete is not null) DataCopy.RemoveClass(toDelete.Value);
            if (ImGuiHelper.SearchableCombo("##addJobCombo", out Job job, _newJob.ToString(),
                                            Enum.GetValues<Job>(), j => j.ToString(),
                                            (j, s) => j.ToString()
                                                       .Contains(s, StringComparison.CurrentCultureIgnoreCase),
                                            j => DataCopy.Classes.All(y => y.Job != j)))
                _newJob = job;
            ImGui.SameLine();
            if (ImGuiHelper.AddButton(PlayableClass.DataTypeNameStatic, "##addJobBtn"))
            {
                PlayableClass? newClass = DataCopy[_newJob];
                if (newClass == null)
                {
                    newClass = DataCopy.AddClass(_newJob);
                    ServiceManager.HrtDataManager.GearDb.TryAdd(newClass.CurGear);
                    ServiceManager.HrtDataManager.GearDb.TryAdd(newClass.CurBis);
                }

                newClass.CurBis.ManagedBy = GearSetManager.Etro;
                newClass.CurBis.EtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(_newJob);
            }
        }

        protected override void Save(Character destination)
        {
            //Copy safe data
            if (destination.Name != DataCopy.Name ||
                destination.HomeWorldId != DataCopy.HomeWorldId)
            {
                destination.Name = DataCopy.Name;
                destination.HomeWorldId = DataCopy.HomeWorldId;
                ServiceManager.HrtDataManager.CharDb.ReindexCharacter(destination.LocalId);
            }

            destination.TribeId = DataCopy.TribeId;
            destination.MainJob = DataCopy.MainJob;
            //Remove classes that were removed in Ui
            for (int i = 0; i < destination.Classes.Count(); i++)
            {
                PlayableClass c = destination.Classes.ElementAt(i);
                if (DataCopy.Classes.Any(x => x.Job == c.Job)) continue;
                destination.RemoveClass(c.Job);
                i--;
            }

            //Add missing classes and update Bis/Level for existing ones
            foreach (PlayableClass c in DataCopy.Classes)
            {
                PlayableClass? target = destination[c.Job];
                GearDb gearSetDb = ServiceManager.HrtDataManager.GearDb;

                if (target == null)
                {
                    target = destination.AddClass(c.Job);
                    gearSetDb.TryAdd(target.CurGear);
                    gearSetDb.TryAdd(target.CurBis);
                }

                target.Level = c.Level;
                target.HideInUi = c.HideInUi;
            }
            if (destination.LocalId.IsEmpty) ServiceManager.HrtDataManager.CharDb.TryAdd(destination);
            ServiceManager.HrtDataManager.Save();
        }
    }

    private class EditGearSetWindow : EditWindow<GearSet>
    {
        private Job _job;
        private string _etroIdInput = "";
        internal EditGearSetWindow(GearSet original, Action<GearSet>? onSave = null,
                                   Action<GearSet>? onCancel = null, Job? job = null) :
            base(original, onSave, onCancel)
        {
            _job = job ?? original[GearSetSlot.MainHand].Jobs.FirstOrDefault(Job.ADV);
            MinSize = new Vector2(550, 300);
        }

        protected override void Cancel() { }

        protected override void DrawContent()
        {
            bool isFromEtro = DataCopy.ManagedBy == GearSetManager.Etro;
            ImGui.SameLine();
            if (ImGuiHelper.DeleteButton(DataCopy, "##delete", true, new Vector2(50, 25)))
            {
                GearSet newSet = new();
                if (ServiceManager.HrtDataManager.GearDb.TryAdd(newSet))
                {
                    DataCopy = newSet;
                    Save();
                    Hide();
                }
            }
            //Replace with etro section
            ImGui.Text(GeneralLoc.EditGearSetUi_text_getEtro);
            foreach ((string etroId, string name) in ServiceManager.ConnectorPool.EtroConnector.GetBiS(_job))
            {
                ImGui.SameLine();
                if (ImGuiHelper.Button($"{name}##BIS#{etroId}", $"{etroId}"))
                {
                    if (ServiceManager.HrtDataManager.GearDb.TryGetSetByEtroId(etroId, out GearSet? newSet))
                    {
                        ReplaceOriginal(newSet);
                        return;
                    }
                    ReplaceOriginal(new GearSet(GearSetManager.Etro, name)
                    {
                        EtroId = etroId,
                    });
                    ServiceManager.ConnectorPool.EtroConnector.GetGearSetAsync(DataCopy);
                    return;
                }
            }
            const string customEtroPopupId = "customEtroPopupID";
            ImGui.SameLine();
            if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_CustomEtro,
                                   GeneralLoc.EditGearSetUi_btn_tt_CustomEtro))
            {
                _etroIdInput = "";
                ImGui.OpenPopup(customEtroPopupId);
            }

            if (ImGui.BeginPopupContextItem(customEtroPopupId))
            {
                ImGui.InputText("CustomEtroID", ref _etroIdInput, 200);
                if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_tt_cutsomEtro,
                                       GeneralLoc.EditGearSetUi_btn_tt_cutsomEtro_Get))
                {
                    if (_etroIdInput.StartsWith(EtroConnector.GEARSET_WEB_BASE_URL))
                        _etroIdInput = _etroIdInput[EtroConnector.GEARSET_WEB_BASE_URL.Length..];
                    if (ServiceManager.HrtDataManager.GearDb.TryGetSetByEtroId(_etroIdInput, out GearSet? newSet))
                    {
                        ReplaceOriginal(newSet);
                        return;
                    }
                    ReplaceOriginal(new GearSet(GearSetManager.Etro)
                    {
                        EtroId = _etroIdInput,
                    });
                    ServiceManager.ConnectorPool.EtroConnector.GetGearSetAsync(DataCopy);
                    ImGui.CloseCurrentPopup();
                    return;
                }
                ImGui.EndPopup();
            }
            //Source information
            if (isFromEtro)
            {
                ImGui.Text($"{GeneralLoc.EditGearSetUi_Source}: {GearSetManager.Etro.FriendlyName()}");
                ImGui.SameLine();
                if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_MakeLocal,
                                       GeneralLoc.EditGearSetUi_btn_tt_MakeLocal))
                {
                    GearSet newSet = DataCopy.Clone();
                    newSet.LocalId = HrtId.Empty;
                    newSet.RemoteIDs = new List<HrtId>();
                    newSet.ManagedBy = GearSetManager.Hrt;
                    newSet.EtroId = string.Empty;
                    ServiceManager.HrtDataManager.GearDb.TryAdd(newSet);
                    ReplaceOriginal(newSet);
                }

                ImGui.Text($"{GeneralLoc.CommonTerms_EtroID}: {DataCopy.EtroId}");
                ImGui.SameLine();
                if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_openEtro, null))
                    Util.OpenLink(EtroConnector.GEARSET_WEB_BASE_URL + DataCopy.EtroId);

                ImGui.Text(string.Format(GeneralLoc.EditGearSetUi_txt_lastUpdate, DataCopy.EtroFetchDate));
            }
            else
            {
                ImGui.Text($"{GeneralLoc.EditGearSetUi_txt_Source}: {GearSetManager.Hrt.FriendlyName()}");
                if (DataCopy.IsSystemManaged)
                    ImGui.TextColored(Colors.TextRed, GeneralLoc.Gearset_Warning_SysManaged);
            }
            //
            ImGui.Text(GeneralLoc.EditGearSetUi_txt_job);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60 * ScaleFactor);
            ImGuiHelper.Combo("##JobSelection", ref _job);
            ImGui.Text(string.Format(GeneralLoc.EditGearSetUi_txt_LastChange, DataCopy.TimeStamp));
            ImGui.BeginDisabled(isFromEtro);
            ImGui.Text($"{GeneralLoc.CommonTerms_Name}: ");
            ImGui.SameLine();
            ImGui.InputText("", ref DataCopy.Name, 100);
            ImGui.NewLine();
            //Gear slots
            if (ImGui.BeginTable("##GearEditTable", 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("##GearL");
                ImGui.TableSetupColumn("##GearR");
                DrawSlot(GearSetSlot.MainHand);
                if (_job.CanHaveShield())
                    DrawSlot(GearSetSlot.OffHand);
                else
                    ImGui.TableNextColumn();
                DrawSlot(GearSetSlot.Head);
                DrawSlot(GearSetSlot.Ear);
                DrawSlot(GearSetSlot.Body);
                DrawSlot(GearSetSlot.Neck);
                DrawSlot(GearSetSlot.Hands);
                DrawSlot(GearSetSlot.Wrist);
                DrawSlot(GearSetSlot.Legs);
                DrawSlot(GearSetSlot.Ring1);
                DrawSlot(GearSetSlot.Feet);
                DrawSlot(GearSetSlot.Ring2);
                ImGui.EndTable();
            }

            ImGui.EndDisabled();
            return;
            void DrawSlot(GearSetSlot slot)
            {
                ImGui.BeginDisabled(ChildIsOpen);
                ImGui.TableNextColumn();
                UiHelpers.DrawGearEdit(this, slot, DataCopy[slot], i =>
                {
                    foreach (HrtMateria mat in DataCopy[slot].Materia)
                    {
                        i.AddMateria(mat);
                    }
                    DataCopy[slot] = i;
                }, _job);
                ImGui.EndDisabled();
            }
        }

        protected override void Save(GearSet destination)
        {
            if (destination.LocalId.IsEmpty) ServiceManager.HrtDataManager.GearDb.TryAdd(destination);
            DataCopy.TimeStamp = DateTime.Now;
            destination.CopyFrom(DataCopy);
            ServiceManager.HrtDataManager.Save();
        }
    }
}

internal abstract class SelectItemWindow<T> : HrtWindow where T : HrtItem
{
    // ReSharper disable once StaticMemberInGenericType
    protected static readonly ExcelSheet<Item> Sheet = ServiceManager.DataManager.GetExcelSheet<Item>()!;
    private readonly Action<T?> _onCancel;
    private readonly Action<T> _onSave;
    protected T? Item;

    internal SelectItemWindow(Action<T> onSave, Action<T?> onCancel)
    {
        (_onSave, _onCancel) = (onSave, onCancel);
        Flags = ImGuiWindowFlags.NoCollapse;
    }
    protected virtual bool CanSave { get; set; } = true;


    public override void Draw()
    {
        if (CanSave && ImGuiHelper.SaveButton())
            Save();
        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
            Cancel();
        DrawItemSelection();
        ImGui.End();
    }

    protected void Save(T? item = null)
    {
        if (item != null)
            Item = item;
        if (Item != null)
            _onSave(Item);
        else
            _onCancel(Item);
        Hide();
    }

    protected void Cancel()
    {
        _onCancel(Item);
        Hide();
    }

    protected abstract void DrawItemSelection();
}

internal class SelectGearItemWindow : SelectItemWindow<GearItem>
{
    private readonly bool _lockJob;
    private readonly bool _lockSlot;
    private List<Item> _items;
    private Job? _job;
    private int _maxILvl;
    private int _minILvl;
    private IEnumerable<GearSetSlot> _slots;

    public SelectGearItemWindow(Action<GearItem> onSave, Action<GearItem?> onCancel, GearItem? currentItem = null,
                                GearSetSlot? slot = null, Job? job = null, int maxItemLevel = 0) : base(
        onSave, onCancel)
    {
        Item = currentItem;
        if (slot.HasValue)
        {
            _lockSlot = true;
            _slots = new[] { slot.Value };
        }
        else
        {
            _slots = Item?.Slots ?? Array.Empty<GearSetSlot>();
        }

        _lockJob = job.HasValue;
        _job = job;
        Title = GeneralLoc.SelectGearItemui_Title
              + $"{string.Join(',', _slots.Select((e, _) => e.FriendlyName()))}";
        _maxILvl = maxItemLevel;
        _minILvl = _maxILvl > 30 ? _maxILvl - 30 : 0;
        _items = ReevaluateItems();
    }
    protected override bool CanSave => false;

    protected override void DrawItemSelection()
    {
        //Draw selection bar
        ImGui.SetNextItemWidth(65f * ScaleFactor);
        ImGui.BeginDisabled(_lockJob);
        if (ImGuiHelper.Combo("##job", ref _job))
            ReevaluateItems();
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(125f * ScaleFactor);
        ImGui.BeginDisabled(_lockSlot);
        GearSetSlot slot = _slots.FirstOrDefault(GearSetSlot.None);
        if (ImGuiHelper.Combo("##slot", ref slot, t => t.FriendlyName()))
        {
            _slots = new[] { slot };
            ReevaluateItems();
        }

        ImGui.SameLine();
        ImGui.EndDisabled();
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        int min = _minILvl;
        if (ImGui.InputInt("-##min", ref min, 5))
        {
            _minILvl = min;
            ReevaluateItems();
        }

        ImGui.SameLine();
        int max = _maxILvl;
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        if (ImGui.InputInt("iLvL##Max", ref max, 5))
        {
            _maxILvl = max;
            ReevaluateItems();
        }

        //Draw item list
        foreach (Item item in _items)
        {
            bool isCurrentItem = item.RowId == Item?.Id;
            if (isCurrentItem)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{item.RowId}", GeneralLoc.SelectItemUi_btn_tt_useThis,
                                   true,
                                   new Vector2(32f, 32f)))
            {
                if (isCurrentItem)
                    Cancel();
                else
                    Save(new GearItem(item.RowId) { IsHq = item.CanBeHq });
            }

            if (isCurrentItem)
                ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Image(ServiceManager.IconCache.LoadIcon(item.Icon, item.CanBeHq).ImGuiHandle,
                        new Vector2(32f, 32f));
            ImGui.SameLine();
            ImGui.Text($"{item.Name.RawString} (IL {item.LevelItem.Row})");
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }
        }
    }

    private List<Item> ReevaluateItems()
    {
        _items = Sheet.Where(x =>
                                 x.ClassJobCategory.Row != 0
                              && (!_slots.Any() ||
                                  _slots.Any(slot => slot == GearSetSlot.None
                                                  || x.EquipSlotCategory.Value.Contains(slot)))
                              && (_maxILvl == 0 || x.LevelItem.Row <= _maxILvl)
                              && x.LevelItem.Row >= _minILvl
                              && (_job.GetValueOrDefault(0) == 0
                               || x.ClassJobCategory.Value.Contains(_job.GetValueOrDefault()))
        ).Take(50).ToList();
        _items.Sort((x, y) => (int)y.LevelItem.Row - (int)x.LevelItem.Row);
        return _items;
    }
}

internal class SelectMateriaWindow : SelectItemWindow<HrtMateria>
{
    private static readonly Dictionary<MateriaLevel, Dictionary<MateriaCategory, HrtMateria>> _allMateria;

    private readonly MateriaLevel _maxLvl;

    static SelectMateriaWindow()
    {
        _allMateria = new Dictionary<MateriaLevel, Dictionary<MateriaCategory, HrtMateria>>();
        foreach (MateriaLevel lvl in Enum.GetValues<MateriaLevel>())
        {
            Dictionary<MateriaCategory, HrtMateria> mats = new();
            foreach (MateriaCategory cat in Enum.GetValues<MateriaCategory>())
            {
                mats[cat] = new HrtMateria(cat, lvl);
            }
            _allMateria[lvl] = mats;
        }
    }

    public SelectMateriaWindow(Action<HrtMateria> onSave, Action<HrtMateria?> onCancel, MateriaLevel maxMatLvl,
                               MateriaLevel? matLevel = null) : base(onSave, onCancel)
    {
        _maxLvl = matLevel ?? maxMatLvl;
        Title = GeneralLoc.SelectMateriaUi_Title;
        string longestName = Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "";
        Size = new Vector2(ImGui.CalcTextSize(longestName).X + 200f, 120f);
    }
    protected override bool CanSave => false;

    protected override void DrawItemSelection()
    {
        //1 Row per Level
        for (MateriaLevel lvl = _maxLvl; lvl != MateriaLevel.None; --lvl)
        {
            ImGui.Text($"{lvl}");
            foreach (MateriaCategory cat in Enum.GetValues<MateriaCategory>())
            {
                if (cat == MateriaCategory.None) continue;
                DrawButton(cat, lvl);
                ImGui.SameLine();
            }

            ImGui.NewLine();
            ImGui.Separator();
        }

        void DrawButton(MateriaCategory cat, MateriaLevel lvl)
        {
            HrtMateria mat = _allMateria[lvl][cat];
            if (ImGui.ImageButton(ServiceManager.IconCache[mat.Icon].ImGuiHandle, new Vector2(32)))
                Save(mat);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                mat.Draw();
                ImGui.EndTooltip();
            }
        }
    }
}