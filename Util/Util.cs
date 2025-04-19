using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace FirstPersonMode.Util;

public static class KeyboardExtensions
{
    // Thank you to 'Margmas' for giving me this snippet from VNEI https://github.com/MSchmoecker/VNEI/blob/master/VNEI/Logic/BepInExExtensions.cs#L21
    // since KeyboardShortcut.IsPressed and KeyboardShortcut.IsDown behave unintuitively
    public static bool IsKeyDown(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsKeyHeld(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }
}

public static class Functions
{
    internal static bool ShouldIgnoreAdjustments(Player localPlayer)
    {
        return Chat.instance?.HasFocus() == true || Console.IsVisible() || InventoryGui.IsVisible() || StoreGui.IsVisible() || Menu.IsVisible() ||
               Minimap.IsOpen() || localPlayer.InCutscene() || localPlayer.InPlaceMode() || IsInAEM();
    }

    public static bool IsInAEM()
    {
        return PPCompat.IsInAEM;
    }

    public static bool IsInFirstPersonMode()
    {
        return FirstPersonModePlugin.DynamicPerson.IsFirstPerson;
    }


    internal static void SetupFP(ref GameCamera __instance, ref Player localPlayer)
    {
        FirstPersonModePlugin.CachedCameraValues.MaxDistance = __instance.m_maxDistance;
        FirstPersonModePlugin.CachedCameraValues.FOV = __instance.m_fov;

        FirstPersonModePlugin.DynamicPerson.NoFp3RdOffset = __instance.m_3rdOffset;
        FirstPersonModePlugin.DynamicPerson.NoFpFPSOffset = __instance.m_fpsOffset;
        __instance.m_3rdOffset = Vector3.zero;
        __instance.m_fpsOffset = Vector3.zero;

        __instance.m_minDistance = 0;
        __instance.m_maxDistance = 0.0f;
        __instance.m_zoomSens = 0;
        __instance.m_nearClipPlaneMax = FirstPersonModePlugin.NearClipPlaneMaxConfig.Value;
        __instance.m_nearClipPlaneMin = FirstPersonModePlugin.NearClipPlaneMinConfig.Value;
        // Default is 65
        __instance.m_fov = FirstPersonModePlugin.DefaultFOV.Value;
        // Make head stuff have no size, same method as mounting legs disappear
        if (FirstPersonModePlugin.NoHeadMode.Value == FirstPersonModePlugin.Toggle.On)
        {
            localPlayer.m_head.localScale = Vector3.zero;
            localPlayer.m_eye.localScale = Vector3.zero;
            __instance.m_nearClipPlaneMax = FirstPersonModePlugin.CachedCameraValues.NearClipPlaneMax;
            __instance.m_nearClipPlaneMin = FirstPersonModePlugin.CachedCameraValues.NearClipPlaneMin;
        }
    }

    internal static void HandleFirstPersonMode(ref GameCamera __instance)
    {
        if (FirstPersonModePlugin.RaiseFOVHotkey.Value.IsKeyDown())
        {
            __instance.m_fov += 1f;
            Console.instance.AddString($"Changed fov to: {__instance.m_fov}");
        }
        else if (FirstPersonModePlugin.LowerFOVHotkey.Value.IsKeyDown())
        {
            __instance.m_fov -= 1f;
            Console.instance.AddString($"Changed fov to: {__instance.m_fov}");
        }
    }

    internal static void HandleNotFirstPersonMode(ref GameCamera __instance, Player localPlayer)
    {
        // Default game camera behavior
        float minDistance = __instance.m_minDistance;
        //float axis = ZInput.GetMouseScrollWheel();
        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (Player.m_debugMode)
            axis = ScrollReturn(__instance, axis);
        __instance.m_distance -= (axis / 3) * __instance.m_zoomSens;
        float max = (localPlayer.GetControlledShip() != null) ? __instance.m_maxDistanceBoat : __instance.m_maxDistance;
        __instance.m_distance = Mathf.Clamp(Mathf.Lerp(__instance.m_distance, Mathf.Clamp(__instance.m_distance, 0f, max), 0.1f), 0f, max);
    }

    internal static float ScrollReturn(GameCamera cam, float scroll)
    {
        if (ZInput.GetKey(KeyCode.LeftShift) && ZInput.GetKey(KeyCode.C) && !Console.IsVisible())
        {
            Vector2 mouseDelta = ZInput.GetMouseDelta();
            EnvMan.instance.m_debugTimeOfDay = true;
            EnvMan.instance.m_debugTime = (float)((EnvMan.instance.m_debugTime + mouseDelta.y * 0.004999999888241291) % 1.0);
            if (EnvMan.instance.m_debugTime < 0.0)
                ++EnvMan.instance.m_debugTime;
            cam.m_fov += mouseDelta.x * 1f;
            cam.m_fov = Mathf.Clamp(cam.m_fov, 0.5f, 165f);
            cam.m_camera.fieldOfView = cam.m_fov;
            cam.m_skyCamera.fieldOfView = cam.m_fov;
            if (Player.m_localPlayer && Player.m_localPlayer.IsDebugFlying())
            {
                if (scroll > 0.0)
                    Character.m_debugFlySpeed = (int)Mathf.Clamp(Character.m_debugFlySpeed * 1.1f, Character.m_debugFlySpeed + 1, 300f);
                else if (scroll < 0.0 && Character.m_debugFlySpeed > 1)
                    Character.m_debugFlySpeed = (int)Mathf.Min(Character.m_debugFlySpeed * 0.9f, Character.m_debugFlySpeed - 1);
            }

            scroll = 0.0f;
        }

        return scroll;
    }

    internal static void SetCameraTransform(ref GameCamera __instance, Player localPlayer, float dt)
    {
        if (IsInFirstPersonMode())
        {
            // Update camera position
            Vector3 headPoint = localPlayer.GetHeadPoint();
            Vector3 offset = localPlayer.m_eye.transform.rotation * new Vector3(0f, 0.15f, 0.071f);
            if (localPlayer.IsDrawingBow())
            {
                // Offset should be different when drawing a bow, it should be more to the right
                // offset = localPlayer.m_eye.transform.rotation * new Vector3(FirstPersonModePlugin.OffsetWhenAiming.Value.x, FirstPersonModePlugin.OffsetWhenAiming.Value.y, FirstPersonModePlugin.OffsetWhenAiming.Value.z);
                offset = new Vector3(0, 0.2f, 0);
            }

            if (Utils.FindChild(localPlayer.transform, "Azu_transform"))
            {
                var vis = Utils.FindChild(localPlayer.transform, "Azu_transform");
                headPoint = Utils.FindChild(vis, "Head").position;
                //568
                offset = localPlayer.m_eye.transform.rotation * new Vector3(0f, 0.15f, 0.142f);
                __instance.m_nearClipPlaneMax = 0.47f;
            }

            __instance.m_nearClipPlaneMax = FirstPersonModePlugin.NearClipPlaneMaxConfig.Value;
            Vector3 currentPosition = __instance.transform.position;
            __instance.transform.position = Vector3.Lerp(currentPosition, headPoint + offset, 1f);
            
            /*CameraHighFrequencyUpdater.Instance.UpdateTarget(headPoint + offset, __instance.transform.rotation);
            var (smoothPos, smoothRot) = CameraHighFrequencyUpdater.Instance.GetSmoothedTransform();
            __instance.transform.position = smoothPos;
            __instance.transform.rotation = smoothRot;*/

            // Check mouse scroll input
            if ((localPlayer.TakeInput() && Input.GetAxis("Mouse ScrollWheel") < 0 && !localPlayer.InPlaceMode())
                || (ZInput.GetButton("JoyAltKeys") && !Hud.InRadial() && ZInput.GetButton("JoyCamZoomOut"))) // If scrolling down
            {
                __instance.m_minDistance += 2f; // Increment m_minDistance
            }

            // Update neck twist
            if (localPlayer.InDodge() || localPlayer.m_attached) return;
            float deviationAngle = 0f - CalculateDeviationAngle(localPlayer.m_eye.rotation.eulerAngles.y, localPlayer.m_body.rotation.eulerAngles.y);
            if (!(Math.Abs(deviationAngle) > FirstPersonModePlugin.DynamicPerson.MaxDeviation)) return;
            float num = localPlayer.m_body.rotation.eulerAngles.y * 1f;
            num += (Math.Abs(deviationAngle) - FirstPersonModePlugin.DynamicPerson.MaxDeviation) * Math.Sign(deviationAngle);
            var rotation = localPlayer.m_body.rotation;
            FirstPersonModePlugin.DynamicPerson.PlayerRotation = Quaternion.Euler(rotation.eulerAngles.x, num, rotation.eulerAngles.z);
            FirstPersonModePlugin.DynamicPerson.PlayerRigidbody = localPlayer.m_body;
            FirstPersonModePlugin.DynamicPerson.PlayerRigidbody.rotation = Quaternion.Slerp(FirstPersonModePlugin.DynamicPerson.PlayerRigidbody.rotation, FirstPersonModePlugin.DynamicPerson.PlayerRotation, dt * FirstPersonModePlugin.SlerpMult.Value);
        }
        else
        {
            // Update camera position and rotation in non-first-person mode
            __instance.GetCameraPosition(dt, out Vector3 position, out Quaternion rotation);
            __instance.transform.position = position;
            __instance.transform.rotation = rotation;
        }
    }

    public static float CalculateDeviationAngle(float angle1, float angle2)
    {
        float difference = (angle2 - angle1 + 540f) % 360f - 180f;
        return (difference >= -180f) ? difference : difference + 360f;
    }
}