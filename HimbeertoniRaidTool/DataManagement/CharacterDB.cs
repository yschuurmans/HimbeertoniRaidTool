﻿using System.Diagnostics.CodeAnalysis;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDb : DataBaseTable<Character,GearSet>
{
    private readonly Dictionary<ulong, HrtId> _charIdLookup = new();
    private readonly Dictionary<HrtId, HrtId> _idReplacement = new();
    private readonly Dictionary<(uint, string), HrtId> _nameLookup = new();
    private readonly HashSet<uint> _usedWorlds = new();

    internal CharacterDb(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<GearSet> conv,
        JsonSerializerSettings settings,GearDb gearDb) : base(idProvider,serializedData,conv,settings)
    {
        if(!LoadError)
        {
            HashSet<HrtId> knownGear = new();
            foreach ((HrtId id,Character c) in Data)
            {
                _usedWorlds.Add(c.HomeWorldId);
                if (!_nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId))
                {
                    ServiceManager.PluginLog.Warning(
                        $"Database contains {c.Name} @ {c.HomeWorld?.Name} twice. Characters were merged");
                    Data.TryGetValue(_nameLookup[(c.HomeWorldId, c.Name)], out Character? other);
                    _idReplacement.Add(c.LocalId, other!.LocalId);
                    other.MergeInfos(c);
                    Data.Remove(c.LocalId);
                    continue;
                }

                if (c.CharId > 0)
                    _charIdLookup.TryAdd(c.CharId, c.LocalId);
                foreach (PlayableClass job in c)
                {
                    job.SetParent(c);
                    if (knownGear.Contains(job.Gear.LocalId))
                    {
                        //Only BiS gearset are meant to be shared
                        GearSet gearCopy = job.Gear.Clone();
                        ServiceManager.PluginLog.Debug(
                            $"Found Gear duplicate with Sequence: {gearCopy.LocalId.Sequence}");
                        gearCopy.LocalId = HrtId.Empty;
                        gearDb.TryAdd(gearCopy);
                        job.Gear = gearCopy;
                    }
                    else
                    {
                        knownGear.Add(job.Gear.LocalId);
                    }
                }
                
            }
        }

        
        
    }

    internal IEnumerable<uint> GetUsedWorlds() => _usedWorlds;

    internal IReadOnlyList<string> GetKnownCharacters(uint worldId)
    {
        List<string> result = new();
        foreach (Character character in Data.Values.Where(c => c.HomeWorldId == worldId))
            result.Add(character.Name);
        return result;
    }

    public override bool TryAdd(in Character c)
    {
        if (!base.TryAdd(c))
            return false;

        _usedWorlds.Add(c.HomeWorldId);
        _nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId);
        if (c.CharId > 0)
            _charIdLookup.TryAdd(c.CharId, c.LocalId);
        return true;
    }

    internal bool TryGetCharacterByCharId(ulong charId, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (_charIdLookup.TryGetValue(charId, out HrtId? id))
            return TryGet(id, out c);
        id = Data.FirstOrDefault(x => x.Value.CharId == charId).Key;
        if (id is not null)
        {
            _charIdLookup.Add(charId, id);
            c = Data[id];
            return true;
        }

        ServiceManager.PluginLog.Debug($"Did not find character with ID: {charId} in database");
        return false;
    }

    internal bool SearchCharacter(uint worldId, string name, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (_nameLookup.TryGetValue((worldId, name), out HrtId? id))
            return TryGet(id, out c);
        id = Data.FirstOrDefault(x => x.Value.HomeWorldId == worldId && x.Value.Name.Equals(name)).Key;
        if (id is not null)
        {
            _nameLookup.Add((worldId, name), id);
            c = Data[id];
            return true;
        }

        ServiceManager.PluginLog.Debug($"Did not find character {name}@{worldId} in database");
        return false;
    }

    public override bool TryGet(HrtId id, [NotNullWhen(true)] out Character? c)
    {
        if (_idReplacement.ContainsKey(id))
            id = _idReplacement[id];
        return Data.TryGetValue(id, out c);
    }

    internal void ReindexCharacter(HrtId localId)
    {
        if (!TryGet(localId, out Character? c))
            return;
        _usedWorlds.Add(c.HomeWorldId);
        _nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId);
        if (c.CharId > 0)
            _charIdLookup.TryAdd(c.CharId, c.LocalId);
    }

    internal IEnumerable<HrtId> FindOrphanedGearSets(IEnumerable<HrtId> possibleOrphans)
    {
        HashSet<HrtId> orphanSets = new(possibleOrphans);
        foreach (PlayableClass job in Data.Values.SelectMany(character => character.Classes))
        {
            orphanSets.Remove(job.Gear.LocalId);
            orphanSets.Remove(job.Bis.LocalId);
        }

        ServiceManager.PluginLog.Information($"Found {orphanSets.Count} orphaned gear sets.");
        return orphanSets;
    }

    internal string Serialize(HrtIdReferenceConverter<GearSet> conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        settings.Converters.Remove(conv);
        return result;
    }

    internal void Prune(HrtDataManager hrtDataManager)
    {
        ServiceManager.PluginLog.Debug("Begin pruning of character database.");
        foreach (HrtId toPrune in hrtDataManager.FindOrphanedCharacters(Data.Keys))
        {
            if (!Data.TryGetValue(toPrune, out Character? character)) continue;
            ServiceManager.PluginLog.Information(
                $"Removed {character.Name} @ {character.HomeWorld?.Name} ({character.LocalId}) from DB");
            Data.Remove(toPrune);
        }

        ServiceManager.PluginLog.Debug("Finished pruning of character database.");
    }
}