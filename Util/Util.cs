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
               Minimap.IsOpen() || localPlayer.InCutscene() || localPlayer.InPlaceMode();
    }

    // Put outside just to clean up
    internal static void SetupFP(ref GameCamera __instance, ref Player localPlayer)
    {
        FirstPersonModePlugin.CachedCameraValues.MaxDistance = __instance.m_maxDistance;
        FirstPersonModePlugin.CachedCameraValues.FOV = __instance.m_fov;
        // Save old offsets and then use our own
        FirstPersonModePlugin.DynamicPerson.NoFp3RdOffset = __instance.m_3rdOffset;
        FirstPersonModePlugin.DynamicPerson.NoFpFPSOffset = __instance.m_fpsOffset;
        __instance.m_3rdOffset = Vector3.zero;
        __instance.m_fpsOffset = Vector3.zero;
        // Set the camera stuff to 0 or our new value
        __instance.m_minDistance = 0;
        __instance.m_maxDistance = 0.0f;
        __instance.m_zoomSens = 0;
        __instance.m_nearClipPlaneMax = FirstPersonModePlugin.NearClipPlaneMaxConfig.Value;
        __instance.m_nearClipPlaneMin = FirstPersonModePlugin.NearClipPlaneMinConfig.Value;
        // What Field Of View value, default is 65
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
        float axis = ZInput.GetMouseScrollWheel();
        if (Player.m_debugMode)
            axis = ScrollReturn(__instance, axis);
        __instance.m_distance -= axis * __instance.m_zoomSens;
        float max = (localPlayer.GetControlledShip() != null) ? __instance.m_maxDistanceBoat : __instance.m_maxDistance;
        __instance.m_distance = Mathf.Clamp(__instance.m_distance, 0f, max);
    }

    internal static float ScrollReturn(GameCamera cam, float scroll)
    {
        if (ZInput.GetKey(KeyCode.LeftShift) && ZInput.GetKey(KeyCode.C) && !Console.IsVisible())
        {
            Vector2 mouseDelta = ZInput.GetMouseDelta();
            EnvMan.instance.m_debugTimeOfDay = true;
            EnvMan.instance.m_debugTime = (float)(((double)EnvMan.instance.m_debugTime + (double)mouseDelta.y * 0.004999999888241291) % 1.0);
            if ((double)EnvMan.instance.m_debugTime < 0.0)
                ++EnvMan.instance.m_debugTime;
            cam.m_fov += mouseDelta.x * 1f;
            cam.m_fov = Mathf.Clamp(cam.m_fov, 0.5f, 165f);
            cam.m_camera.fieldOfView = cam.m_fov;
            cam.m_skyCamera.fieldOfView = cam.m_fov;
            if (Player.m_localPlayer && Player.m_localPlayer.IsDebugFlying())
            {
                if ((double)scroll > 0.0)
                    Character.m_debugFlySpeed = (int)Mathf.Clamp((float)Character.m_debugFlySpeed * 1.1f, (float)(Character.m_debugFlySpeed + 1), 300f);
                else if ((double)scroll < 0.0 && Character.m_debugFlySpeed > 1)
                    Character.m_debugFlySpeed = (int)Mathf.Min((float)Character.m_debugFlySpeed * 0.9f, (float)(Character.m_debugFlySpeed - 1));
            }

            scroll = 0.0f;
        }

        return scroll;
    }

    internal static void SetCameraTransform(ref GameCamera __instance, Player localPlayer, float dt)
    {
        if (FirstPersonModePlugin.DynamicPerson.IsFirstPerson)
        {
            // Update camera position
            Vector3 headPoint = localPlayer.GetHeadPoint();
            Vector3 offset = localPlayer.m_eye.transform.rotation * new Vector3(0f, 0.15f, 0.071f);
            if (localPlayer.IsDrawingBow())
            {
                // Offset should be different when drawing a bow, it should be more to the right
                offset = localPlayer.m_eye.transform.rotation * new Vector3(FirstPersonModePlugin.OffsetWhenAiming.Value.x, FirstPersonModePlugin.OffsetWhenAiming.Value.y, FirstPersonModePlugin.OffsetWhenAiming.Value.z);
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
            //__instance.transform.position = headPoint + offset;
            __instance.transform.position = Vector3.Lerp(currentPosition, headPoint + offset, 2f);

            // Check mouse scroll input
            if (localPlayer.TakeInput() && Input.GetAxis("Mouse ScrollWheel") < 0 && !localPlayer.InPlaceMode()) // If scrolling down
            {
                __instance.m_minDistance += 2f; // Increment m_minDistance
            }

            if (ZInput.GetButton("JoyAltKeys") && !Hud.InRadial())
            {
                if (__instance.m_camZoomToggle && ZInput.GetButton("JoyCamZoom"))
                    __instance.m_minDistance += 2f;
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
            FirstPersonModePlugin.DynamicPerson.PlayerRigidbody.rotation = FirstPersonModePlugin.DynamicPerson.PlayerRotation;
        }
        else
        {
            // Update camera position and rotation in non-first-person mode
            Vector3 position;
            Quaternion rotation;
            __instance.GetCameraPosition(dt, out position, out rotation);
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