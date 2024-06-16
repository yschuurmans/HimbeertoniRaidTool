﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Plugin.Services;

internal unsafe class CharacterInfoService
{
    private readonly IObjectTable _gameObjects;
    private readonly IPartyList _partyList;
    private readonly InfoModule* _infoModule;
    private InfoProxyPartyMember* PartyInfo =>
        (InfoProxyPartyMember*)_infoModule->GetInfoProxyById(InfoProxyId.PartyMember);
    private readonly Dictionary<string, ulong> _cache = new();
    private readonly HashSet<string> _notFound = new();
    private DateTime _lastPrune;
    private static readonly TimeSpan _pruneInterval = TimeSpan.FromSeconds(10);

    internal CharacterInfoService(IObjectTable gameObjects, IPartyList partyList)
    {
        _gameObjects = gameObjects;
        _partyList = partyList;
        _lastPrune = DateTime.Now;
        _infoModule = Framework.Instance()->UIModule->GetInfoModule();
    }

    public long GetLocalPlayerContentId() => (long)_infoModule->LocalContentId;

    public ulong GetContentId(PlayerCharacter? character)
    {
        if (character == null)
            return 0;
        foreach (PartyMember partyMember in _partyList)
        {
            bool found =
                partyMember.ObjectId == character.GameObjectId
             || partyMember.Name.Equals(character.Name) && partyMember.World.Id == character.CurrentWorld.Id;
            if (found)
                return (ulong)partyMember.ContentId;
        }

        if (PartyInfo == null)
            return 0;
        for (uint i = 0; i < PartyInfo->InfoProxyCommonList.DataSize; ++i)
        {
            var entry = PartyInfo->InfoProxyCommonList.GetEntry(i);
            if (entry == null) continue;
            if (entry->HomeWorld != character.HomeWorld.Id) continue;
            string name = entry->Name.ToString();
            if (name.Equals(character.Name.TextValue))
                return entry->ContentId;
        }

        return 0;
    }

    public bool TryGetChar([NotNullWhen(true)] out PlayerCharacter? result, string name, World? w = null)
    {
        Update();
        if (_cache.TryGetValue(name, out ulong id))
        {
            result = _gameObjects.SearchById(id) as PlayerCharacter;
            if (result?.Name.TextValue == name
             && (w is null || w.RowId == result.HomeWorld.Id))
                return true;
            else
                _cache.Remove(name);
        }

        if (_notFound.Contains(name))
        {
            result = null;
            return false;
        }

        //This is really slow (comparatively)
        result = ServiceManager.ObjectTable.FirstOrDefault(
            o =>
            {
                var p = o as PlayerCharacter;
                return p != null
                    && p.Name.TextValue == name
                    && (w is null || p.HomeWorld.Id == w.RowId);
            }, null) as PlayerCharacter;
        if (result != null)
        {
            _cache.Add(name, result.GameObjectId);
            return true;
        }
        else
        {
            _notFound.Add(name);
            return false;
        }
    }

    private void Update()
    {
        if (DateTime.Now - _lastPrune <= _pruneInterval) return;
        _lastPrune = DateTime.Now;
        if (_notFound.Count == 0) return;
        _notFound.Clear();
    }
}