using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using System;
using System.Numerics; // Standard Vector3
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
        => DistanceBetweenObjects(source.Position, target.Position, target.HitboxRadius);

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
        // Define sourcePos as System.Numerics.Vector3
        Vector3 sourcePos;
        
        if (useCamera)
        {
            var cam = CameraManager.Instance()->CurrentCamera;
            // Explicit cast from ClientStructs Vector3 to System.Numerics Vector3
            var camPos = cam->Object.Position;
            sourcePos = new Vector3(camPos.X, camPos.Y, camPos.Z);
        }
        else
        {
            if (Plugin.Instance.ClientState.LocalPlayer == null) return false;
            var player = (GameObject*)Plugin.Instance.ClientState.LocalPlayer.Address;
            var pPos = player->Position;
            sourcePos = new Vector3(pPos.X, pPos.Y + 2, pPos.Z);
        }

        var tPos = target->Position;
        // Explicit cast for target
        var targetPos = new Vector3(tPos.X, tPos.Y + 2, tPos.Z);

        // Now we subtract two System.Numerics.Vector3 objects (No ambiguity)
        var direction = targetPos - sourcePos;
        var distance = direction.Length();

        direction = Vector3.Normalize(direction);

        // Convert back to ClientStructs Vector3 for the Raycast call
        var csOrigin = new FFXIVClientStructs.FFXIV.Common.Math.Vector3(sourcePos.X, sourcePos.Y, sourcePos.Z);
        var csDirection = new FFXIVClientStructs.FFXIV.Common.Math.Vector3(direction.X, direction.Y, direction.Z);

        RaycastHit hit;
        var flags = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
        
        var isLoSBlocked = CSFramework.Instance()->BGCollisionModule->RaycastMaterialFilter(&hit, &csOrigin, &csDirection, distance, 1, flags);

        return isLoSBlocked == false;
    }

    internal static uint[] GetEnemyListObjectIds()
    {
        var addonPtr = Plugin.Instance.GameGui.GetAddonByName("_EnemyList", 1);
        if (addonPtr == IntPtr.Zero)
            return Array.Empty<uint>();

        // Explicitly cast the wrapper to nint (IntPtr), then to the pointer
        var addon = (AddonEnemyList*)(nint)addonPtr;
        
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