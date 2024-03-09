﻿using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public interface IDataBaseTable<T> where T : IHasHrtId
{
    internal bool Load(JsonSerializerSettings jsonSettings, string data);
    internal bool TryGet(HrtId id, [NotNullWhen(true)] out T? value);
    internal bool TryAdd(in T value);
    internal IEnumerable<T> GetValues();
    internal HrtWindow OpenSearchWindow(Action<T> onSelect, Action? onCancel = null);
    public HashSet<HrtId> GetReferencedIds();
    internal ulong GetNextSequence();
    internal bool Contains(HrtId hrtId);
    public void RemoveUnused(HashSet<HrtId> referencedIds);
    public void FixEntries();
    internal string Serialize(JsonSerializerSettings settings);
}

public abstract class DataBaseTable<T> : IDataBaseTable<T> where T : class, IHasHrtId
{

    protected readonly Dictionary<HrtId, T> Data = new();
    protected readonly IImmutableList<JsonConverter> RefConverters;
    protected readonly IIdProvider IdProvider;
    protected ulong NextSequence = 0;
    protected bool LoadError = false;
    protected bool IsLoaded = false;
    protected DataBaseTable(IIdProvider idProvider, IEnumerable<JsonConverter> converters)
    {
        IdProvider = idProvider;
        RefConverters = ImmutableList.CreateRange(converters);

    }
    public bool Load(JsonSerializerSettings settings, string serializedData)
    {
        List<JsonConverter> savedConverters = new(settings.Converters);
        foreach (JsonConverter jsonConverter in RefConverters)
        {
            settings.Converters.Add(jsonConverter);
        }
        var data = JsonConvert.DeserializeObject<List<T>>(serializedData, settings);
        settings.Converters = savedConverters;
        if (data is null)
        {
            ServiceManager.PluginLog.Error($"Could not load {typeof(T)} database");
            LoadError = true;
            return IsLoaded;
        }
        foreach (T value in data)
        {
            if (value.LocalId.IsEmpty)
            {
                ServiceManager.PluginLog.Error(
                    $"{typeof(T).Name} {value} was missing an ID and was removed from the database");
                continue;
            }
            if (Data.TryAdd(value.LocalId, value))
                NextSequence = Math.Max(NextSequence, value.LocalId.Sequence);
        }
        NextSequence++;
        ServiceManager.PluginLog.Information($"Database contains {Data.Count} entries of type {typeof(T).Name}");
        IsLoaded = true;
        return IsLoaded;
    }
    public virtual bool TryGet(HrtId id, [NotNullWhen(true)] out T? value) => Data.TryGetValue(id, out value);
    public virtual bool TryAdd(in T c)
    {
        if (c.LocalId.IsEmpty)
            c.LocalId = IdProvider.CreateId(c.IdType);
        return Data.TryAdd(c.LocalId, c);
    }
    public void RemoveUnused(HashSet<HrtId> referencedIds)
    {
        ServiceManager.PluginLog.Debug($"Begin pruning of {typeof(T).Name} database.");
        IEnumerable<HrtId> keyList = new List<HrtId>(Data.Keys);
        foreach (HrtId id in keyList.Where(id => !referencedIds.Contains(id)))
        {
            Data.Remove(id);
            ServiceManager.PluginLog.Information($"Removed {id} from {typeof(T).Name} database");
        }
        ServiceManager.PluginLog.Debug($"Finished pruning of {typeof(T).Name} database.");
    }
    public bool Contains(HrtId hrtId) => Data.ContainsKey(hrtId);
    public IEnumerable<T> GetValues() => Data.Values;
    public ulong GetNextSequence() => NextSequence++;
    public abstract HrtWindow OpenSearchWindow(Action<T> onSelect, Action? onCancel = null);
    public string Serialize(JsonSerializerSettings settings)
    {
        List<JsonConverter> savedConverters = new(settings.Converters);
        foreach (JsonConverter jsonConverter in RefConverters)
        {
            settings.Converters.Add(jsonConverter);
        }
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        settings.Converters = savedConverters;
        return result;
    }
    public abstract HashSet<HrtId> GetReferencedIds();
    public virtual void FixEntries() { }

    internal abstract class SearchWindow<Q, R> : HrtWindow where R : IDataBaseTable<Q> where Q : IHasHrtId
    {
        protected readonly R Database;
        private readonly Action<Q> _onSelect;
        private readonly Action? _onCancel;

        protected Q? Selected;
        protected SearchWindow(R dataBase, Action<Q> onSelect, Action? onCancel)
        {
            _onSelect = onSelect;
            _onCancel = onCancel;
            Database = dataBase;
        }

        protected void Save()
        {
            if (Selected == null)
                return;
            _onSelect.Invoke(Selected!);
            Hide();
        }

        public override void Draw()
        {
            if (ImGuiHelper.SaveButton(null, Selected is not null))
            {
                Save();
            }
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
            {
                _onCancel?.Invoke();
                Hide();
            }
            DrawContent();
        }
        protected abstract void DrawContent();
    }
}