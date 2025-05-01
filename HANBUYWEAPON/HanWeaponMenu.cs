using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using System.Data.SqlTypes;
using CounterStrikeSharp.API.Modules.Admin;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class WaeponWasdMenu
{

    public CPointWorldText? worldText { get; private set; }
    public static CHandle<CCSGOViewModel> GetOrCreateViewModels(CCSPlayerPawn playerPawn)
    {
        var handle = new CHandle<CCSGOViewModel>((IntPtr)(playerPawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
        if (!handle.IsValid)
        {
            CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
            viewmodel.DispatchSpawn();
            handle.Raw = viewmodel.EntityHandle.Raw;
            Utilities.SetStateChanged(playerPawn, "CCSPlayerPawnBase", "m_pViewModelServices");
        }
        return handle;
    }

/*
    public void Spawn(CCSPlayerPawn playerPawn, CHandle<CCSGOViewModel> handle, string Text, float shiftX = 0f, float shiftY = 0f,int size = 128)
    {
        worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        if (worldText is null)
            throw new Exception("Failed to create point_worldtext Entity.");

        worldText.MessageText = Data.Text;
        worldText.Enabled = true;
        worldText.FontSize = size;
        worldText.Color = Color.Aquamarine;
        worldText.Fullbright = true;
        worldText.WorldUnitsPerPx = 0.01f;
        worldText.DepthOffset = 0.0f;
        worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
        worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
        worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        //worldText.DepthOffset = Config.Offset_Z;
        QAngle eyeAngles = playerPawn.EyeAngles;
        Vector forward = new Vector(), right = new Vector() ,up = new Vector();
        NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);
        //Vector offset = CalculateHudOffset(forward, right, up);
        Vector offset = new();
        offset += forward * 7;
        offset += right * shiftX;
        offset += up * shiftY;

        QAngle angles = new QAngle()
        {
            Y = eyeAngles.Y + 270,
            Z = 90 - eyeAngles.X,
            X = 0
        };
        worldText.DispatchSpawn();
        worldText.Teleport(playerPawn.AbsOrigin! + offset + new Vector(0, 0, playerPawn.ViewOffset.Z), angles, null);
        worldText.AcceptInput("SetParent", handle.Value, null, "!activator");
        return worldText;
    }

*/

    public static CPointWorldText CreateText(CCSPlayerController client, CHandle<CCSGOViewModel> handle, string Text, float shiftX = 0f, float shiftY = 0f,int size = 128,string FontName = "",Color? color = null)
    {
        var playerPawn = client.PlayerPawn.Value;
        CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;

        worldText.Entity!.Name = "weaponsmenu";
        worldText.OwnerEntity.Raw = client.Pawn.Raw;
        Schema.SetSchemaValue(worldText.Handle, "CBaseEntity", "m_hOwnerEntity", client.Pawn.Raw);
        Marshal.WriteInt64(worldText.Handle, 1392, 2417); // m_pfnTouch = 2417 (BounceTouch)

        worldText.MessageText = Text;
        worldText.Enabled = true;
        worldText.FontSize = size;
        worldText.Fullbright = true;
        worldText.Color = color ?? Color.Aquamarine;
        worldText.DrawBackground = true;
        worldText.BackgroundBorderHeight = 0.0f;
        worldText.BackgroundBorderWidth = 0.2f;
        worldText.BackgroundMaterialName = "";
        worldText.WorldUnitsPerPx = 0.01f;
        worldText.FontName = FontName;
        worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
        worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
        worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;
        //worldText.DepthOffset = Config.Offset_Z;
        QAngle eyeAngles = playerPawn.EyeAngles;
        Vector forward = new Vector(), right = new Vector() ,up = new Vector();
        NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);
        //Vector offset = CalculateHudOffset(forward, right, up);
        Vector offset = new();
        offset += forward * 7;
        offset += right * shiftX;
        offset += up * shiftY;

        QAngle angles = new QAngle()
        {
            Y = eyeAngles.Y + 270,
            Z = 90 - eyeAngles.X,
            X = 0
        };
        worldText.DispatchSpawn();
        worldText.Teleport(playerPawn.AbsOrigin! + offset + new Vector(0, 0, playerPawn.ViewOffset.Z), angles, null);
        worldText.AcceptInput("ClearParent");
        worldText.AcceptInput("SetParent", handle.Value, null, "!activator");
        
        
        return worldText;
        
    }

    

  

    


}