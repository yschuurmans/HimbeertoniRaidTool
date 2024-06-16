﻿using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

namespace HimbeertoniRaidTool.Plugin.Services;

//Inspired by InventoryTools by Critical-Impact 
public class IconCache(ITextureProvider textureProvider)
{
    private readonly Dictionary<uint, ISharedImmediateTexture> _icons = new();

    public IDalamudTextureWrap this[uint id] => LoadIcon(id);

    public IDalamudTextureWrap LoadIcon(uint id, bool hq = false)
    {
        if (_icons.TryGetValue(id, out ISharedImmediateTexture? icon))
            return icon.GetWrapOrEmpty();
        _icons[id] = icon = textureProvider.GetFromGameIcon(new GameIconLookup(id, hq));
        return icon.GetWrapOrEmpty();
    }
}