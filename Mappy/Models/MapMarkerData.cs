﻿using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DailyDuty.System;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using KamiLib.Caching;
using Lumina.Excel.GeneratedSheets;
using Mappy.System;
using Mappy.System.Modules;
using Mappy.Utility;
using Action = System.Action;

namespace Mappy.DataModels;

public enum MapMarkerType
{
    Unknown = -1,
    Standard,
    MapLink,
    InstanceLink,
    Aetheryte,
    Aethernet
}

public class MapMarkerData
{
    private readonly MapIconConfig settings;
    
    private readonly MapMarker data;
    private Vector2 Position => new(data.X, data.Y);
    public TextureWrap? Icon => IconCache.Instance.GetIcon(data.Icon);
    private PlaceName PlaceName => LuminaCache<PlaceName>.Instance.GetRow(data.PlaceNameSubtext.Row)!;
    private Map DataMap => LuminaCache<Map>.Instance.GetRow(data.DataKey)!;
    private Aetheryte DataAetheryte => LuminaCache<Aetheryte>.Instance.GetRow(data.DataKey)!;
    private PlaceName DataPlaceName => LuminaCache<PlaceName>.Instance.GetRow(data.DataKey)!;
    private byte DataType => data.DataType;
    public MapMarkerType Type => (MapMarkerType) data.DataType;
    public uint IconId => data.Icon;
    
    [MemberNotNullWhen(true, nameof(Icon))]
    private bool HasIcon => Icon != null && data.Icon != 0;
    
    public MapMarkerData(MapMarker marker, MapIconConfig config)
    {
        data = marker;
        settings = config;
    }

    public void Draw()
    {
        if (!HasIcon) return;
        
        DrawUtilities.DrawIcon(Icon, Position, settings.IconScale);
        if(settings.ShowTooltip) DrawTooltip();
        OnClick();
    }

    private void DrawTooltip()
    {
        if (!HasIcon) return;
        if (!ImGui.IsItemHovered()) return;

        var displayString = GetDisplayString();
        
        if (displayString is not null && displayString != string.Empty)
        {
            ImGui.BeginTooltip();
            ImGui.TextColored(GetDisplayColor(), displayString);
            ImGui.EndTooltip();
        }
    }

    private void OnClick()
    {
        if (!ImGui.IsItemClicked()) return;

        GetClickAction()?.Invoke();
    }

    private Action? GetClickAction() => (MapMarkerType?) DataType switch
    {
        MapMarkerType.Standard => null,
        MapMarkerType.MapLink => MapLinkAction,
        MapMarkerType.InstanceLink => null,
        MapMarkerType.Aetheryte => AetheryteAction,
        MapMarkerType.Aethernet => null,
        _ => null
    };

    private void MapLinkAction() => MappySystem.MapTextureController.LoadMap(DataMap.RowId);
    private void AetheryteAction() => TeleporterController.Instance.Teleport(DataAetheryte);
    
    private string? GetDisplayString() => (MapMarkerType?) DataType switch
    {
        MapMarkerType.Standard => GetStandardMarkerString(),
        MapMarkerType.MapLink => PlaceName.Name.ToDalamudString().TextValue,
        MapMarkerType.InstanceLink => DataMap.PlaceName.Value?.Name.ToDalamudString().TextValue,
        MapMarkerType.Aetheryte => DataAetheryte.PlaceName.Value?.Name.ToDalamudString().TextValue,
        MapMarkerType.Aethernet => DataPlaceName.Name.ToDalamudString().TextValue,
        _ => null
    };

    private Vector4 GetDisplayColor() => (MapMarkerType?) DataType switch
    {
        MapMarkerType.Standard => settings.StandardColor,
        MapMarkerType.MapLink => settings.MapLinkColor,
        MapMarkerType.InstanceLink => settings.InstanceLinkColor,
        MapMarkerType.Aetheryte => settings.AetheryteColor,
        MapMarkerType.Aethernet => settings.AethernetColor,
        _ => settings.StandardColor
    };

    private string? GetStandardMarkerString()
    {
        var placeName = PlaceName.Name.ToDalamudString().TextValue;
        if (placeName != string.Empty) return placeName;

        var mapSymbol = LuminaCache<MapSymbol>.Instance.GetRow(data.Icon);
        return mapSymbol?.PlaceName.Value?.Name.ToDalamudString().TextValue;
    }
}