using Dalamud.Game.ClientState.Conditions;
using ImGuiNET = Dalamud.Bindings.ImGui; // CHANGED: Points to SDK's ImGui
using System;
using System.Linq;
using System.Numerics;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using CameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;

namespace BetterTargetingSystem;

public unsafe class DebugMode
{
    private readonly Plugin Plugin;

    public DebugMode(Plugin plugin)
    {
        this.Plugin = plugin;
    }

    public void DrawCones()
    {
        if (Plugin.ClientState.LocalPlayer == null)
            return;

        var pos = Plugin.ClientState.LocalPlayer.Position;

        Plugin.GameGui.WorldToScreen(new Vector3(
            pos.X,
            pos.Y,
            pos.Z),
            out Vector2 v2Local);
        ImGuiNET.ImGui.GetWindowDrawList().PathLineTo(v2Local);

        var cone1HalfAngle = Plugin.Configuration.Cone1Angle / 2;
        var cone2HalfAngle = Plugin.Configuration.Cone2Angle / 2;
        var cone3HalfAngle = Plugin.Configuration.Cone3Angle / 2;
        var rotation = Utils.GetCameraRotation();
        for (var degrees = -cone1HalfAngle; degrees <= cone1HalfAngle; degrees += 0.1f)
        {
            float distance = Plugin.Configuration.Cone1Distance;
            if (Plugin.Configuration.Cone3Enabled && (degrees >= -cone3HalfAngle && degrees < cone3HalfAngle))
                distance = Plugin.Configuration.Cone3Distance;
            else if (Plugin.Configuration.Cone2Enabled && (degrees >= -cone2HalfAngle && degrees < cone2HalfAngle))
                distance = Plugin.Configuration.Cone2Distance;

            float rad = (float)(degrees * Math.PI / 180) + rotation;
            Plugin.GameGui.WorldToScreen(new Vector3(
                    pos.X + distance * (float)Math.Sin(rad),
                    pos.Y,
                    pos.Z + distance * (float)Math.Cos(rad)),
                out Vector2 vector2);

            ImGuiNET.ImGui.GetWindowDrawList().PathLineTo(vector2);
        }

        ImGuiNET.ImGui.GetWindowDrawList().PathLineTo(v2Local);

        var color = ImGuiNET.ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 0.4f));
        ImGuiNET.ImGui.GetWindowDrawList().PathFillConvex(color);
    }

    public void DrawCircle()
    {
        if (Plugin.ClientState.LocalPlayer == null)
            return;

        var distance = Plugin.Configuration.CloseTargetsCircleRadius;
        var pos = Plugin.ClientState.LocalPlayer.Position;
        for (var degrees = 0; degrees <= 360; degrees += 10)
        {
            float rad = (float)(degrees * Math.PI / 180);
            Plugin.GameGui.WorldToScreen(new Vector3(
                    pos.X + distance * (float)Math.Sin(rad),
                    pos.Y,
                    pos.Z + distance * (float)Math.Cos(rad)),
                out Vector2 vector2);

            ImGuiNET.ImGui.GetWindowDrawList().PathLineTo(vector2);
        }

        var color = ImGuiNET.ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 1, 0.4f));
        ImGuiNET.ImGui.GetWindowDrawList().PathFillConvex(color);
    }

    public void Draw()
    {
        if (Plugin.ClientState.LocalPlayer == null)
            return;

        if (Plugin.Condition[ConditionFlag.InCombat] || Plugin.Condition[ConditionFlag.InFlight]
            || Plugin.Condition[ConditionFlag.BoundByDuty] || Plugin.Condition[ConditionFlag.BetweenAreas])
            return;

        ImGuiNET.ImGui.Begin("Canvas",
            ImGuiNET.ImGuiWindowFlags.NoInputs | ImGuiNET.ImGuiWindowFlags.NoNav | ImGuiNET.ImGuiWindowFlags.NoTitleBar |
            ImGuiNET.ImGuiWindowFlags.NoScrollbar | ImGuiNET.ImGuiWindowFlags.NoBackground);
        ImGuiNET.ImGui.SetWindowSize(ImGuiNET.ImGui.GetIO().DisplaySize);

        DrawCones();

        if (Plugin.Configuration.CloseTargetsCircleEnabled)
            DrawCircle();

        var (ConeTargets, CloseTargets, _, OnScreenTargets) = Plugin.GetTargets();
        if (ConeTargets.Count > 0 || CloseTargets.Count > 0)
        {
            foreach (var target in ConeTargets)
                HighlightTarget(target, new Vector4(1, 0, 0, 1));

            foreach (var target in CloseTargets.ExceptBy(ConeTargets.Select(o => o.EntityId), o => o.EntityId))
                HighlightTarget(target, new Vector4(0, 0.8f, 1, 1));
        }
        else
        {
            foreach (var target in OnScreenTargets)
                HighlightTarget(target, new Vector4(0, 1, 0, 1));
        }
        ImGuiNET.ImGui.End();
    }

    private void HighlightTarget(DalamudGameObject target, Vector4 colour)
    {
        Plugin.GameGui.WorldToScreen(target.Position, out var screenPos);
        
        var camera = CameraManager.Instance()->CurrentCamera; 
        var distance = Utils.DistanceBetweenObjects(camera->Object.Position, target.Position, 0);
        
        var go = (GameObject*)target.Address;
        var size = (int)Math.Round(100 * (25 / distance)) * Math.Max(target.HitboxRadius, go->Height);
        
        ImGuiNET.ImGui.GetWindowDrawList().AddRect(
            new Vector2(screenPos.X - (size / 2), screenPos.Y),
            new Vector2(screenPos.X + (size / 2), screenPos.Y - size),
            ImGuiNET.ImGui.GetColorU32(colour),
            0f,
            ImGuiNET.ImDrawFlags.None,
            3f);
    }
}