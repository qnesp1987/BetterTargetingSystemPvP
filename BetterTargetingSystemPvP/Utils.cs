using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using System;
using System.Numerics; // Uses System.Numerics directly
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using CameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;
using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace BetterTargetingSystem;

public unsafe class Utils
{
    private static RaptureAtkModule* RaptureAtkModule => CSFramework.Instance()->GetUIModule()->GetRaptureAtkModule();
    internal static bool IsTextInputActive => RaptureAtkModule->AtkModule.IsTextInputActive();

    internal static bool CanAttack(IGameObject obj) => true; 

    internal static float DistanceBetweenObjects(IGameObject source, IGameObject target)
    {
        return DistanceBetweenObjects(source.Position, target.Position, target.HitboxRadius);
    }

    internal static float DistanceBetweenObjects(Vector3 sourcePos, Vector3 targetPos, float targetHitboxRadius = 0)
    {
        var distance = Vector3.Distance(sourcePos, targetPos);
        distance -= targetHitboxRadius;
        return distance;
    }

    internal static float GetCameraRotation()
    {
        var cameraRotation = RaptureAtkModule->AtkModule.AtkArrayDataHolder.NumberArrays[24]->IntArray[3];
        var sign = Math.Sign(cameraRotation) == -1 ? -1 : 1;
        var rotation = (float)((Math.Abs(cameraRotation * (Math.PI / 180)) - Math.PI) * sign);
        return rotation;
    }

    internal static bool IsInFrontOfCamera(IGameObject obj, float maxAngle)
    {
        if (Plugin.Instance.ClientState.LocalPlayer == null)
            return false;

        var rotation = GetCameraRotation();
        var faceVec = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));

        var dir = obj.Position - Plugin.Instance.ClientState.LocalPlayer.Position;
        var dirVec = new Vector2(dir.Z, dir.X);
        var angle = Math.Acos(Vector2.Dot(dirVec, faceVec) / dirVec.Length() / faceVec.Length());
        return angle <= Math.PI * maxAngle / 360;
    }

    internal static bool IsInLineOfSight(GameObject* target, bool useCamera = false)
    {
        // 1. Setup Source (System.Numerics)
        Vector3 sourcePos;
        if (useCamera)
        {
            var cam = CameraManager.Instance()->CurrentCamera;
            sourcePos = cam->Object.Position; // Auto-converts in new ClientStructs
        }
        else
        {
            if (Plugin.Instance.ClientState.LocalPlayer == null) return false;
            var player = (GameObject*)Plugin.Instance.ClientState.LocalPlayer.Address;
            sourcePos = player->Position;
            sourcePos.Y += 2;
        }

        // 2. Setup Target (System.Numerics)
        var targetPos = target->Position;
        targetPos.Y += 2;

        // 3. Math
        var direction = targetPos - sourcePos;
        var distance = direction.Length();
        direction = Vector3.Normalize(direction);

        // 4. Raycast (Now accepts System.Numerics.Vector3 pointers)
        RaycastHit hit;
        var flags = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
        
        var isLoSBlocked = CSFramework.Instance()->BGCollisionModule->RaycastMaterialFilter(&hit, &sourcePos, &direction, distance, 1, flags);

        return isLoSBlocked == false;
    }

    internal static uint[] GetEnemyListObjectIds()
    {
        var addonPtr = Plugin.Instance.GameGui.GetAddonByName("_EnemyList", 1);
        if (addonPtr == IntPtr.Zero)
            return Array.Empty<uint>();

        var addon = (AddonEnemyList*)addonPtr; // Implicit conversion should handle this now
        
        var numArray = RaptureAtkModule->AtkModule.AtkArrayDataHolder.NumberArrays[21];
        var list = new List<uint>(addon->EnemyCount);
        for (var i = 0; i < addon->EnemyCount; i++)
        {
            var id = (uint)numArray->IntArray[8 + (i * 6)];
            list.Add(id);
        }
        return list.ToArray();
    }
}