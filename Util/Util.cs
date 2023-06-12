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
        return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) &&
               shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsKeyHeld(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) &&
               shortcut.Modifiers.All(Input.GetKey);
    }
}

public static class Functions
{
    internal static bool ShouldIgnoreAdjustments(Player localPlayer)
    {
        return Chat.instance?.HasFocus() == true || Console.IsVisible() || 
               InventoryGui.IsVisible() || StoreGui.IsVisible() || Menu.IsVisible() || 
               Minimap.IsOpen() || localPlayer.InCutscene() || localPlayer.InPlaceMode();
    }
    
    // Put outside just to clean up
    internal static void SetupFP(ref GameCamera __instance, ref Player localPlayer)
    {
        // Save old offsets and then use our own
        FirstPersonModePlugin.DynamicPerson.noFP_3rdOffset = __instance.m_3rdOffset;
        FirstPersonModePlugin.DynamicPerson.noFP_fpsOffset = __instance.m_fpsOffset;
        __instance.m_3rdOffset = Vector3.zero;
        __instance.m_fpsOffset = Vector3.zero;
        // Set the camera stuff to 0 or our new value
        __instance.m_minDistance = 0;
        __instance.m_maxDistance = 0;
        __instance.m_zoomSens = 0;
        __instance.m_nearClipPlaneMax = FirstPersonModePlugin.NearClipPlaneMaxConfig.Value;
        __instance.m_nearClipPlaneMin = FirstPersonModePlugin.NearClipPlaneMinConfig.Value;
        // What Field Of View value, default is 65
        __instance.m_fov = FirstPersonModePlugin.defaultFOV.Value;
        // Make head stuff have no size, same method as mounting legs disappear
        if (FirstPersonModePlugin.noHeadMode.Value == FirstPersonModePlugin.Toggle.On)
        {
            localPlayer.m_head.localScale = Vector3.zero;
            localPlayer.m_eye.localScale = Vector3.zero;
            __instance.m_nearClipPlaneMax = FirstPersonModePlugin.CameraConstants.nearClipPlaneMax;
            __instance.m_nearClipPlaneMin = FirstPersonModePlugin.CameraConstants.nearClipPlaneMin;
        }
    }

    internal static void HandleFirstPersonMode(ref GameCamera __instance)
    {
        if (FirstPersonModePlugin.raiseFOVHotkey.Value.IsKeyDown())
        {
            __instance.m_fov += 1f;
            Console.instance.AddString($"Changed fov to: {__instance.m_fov}");
        }
        else if (FirstPersonModePlugin.lowerFOVHotkey.Value.IsKeyDown())
        {
            __instance.m_fov -= 1f;
            Console.instance.AddString($"Changed fov to: {__instance.m_fov}");
        }
    }

    internal static void HandleNotFirstPersonMode(ref GameCamera __instance, Player localPlayer)
    {
        // Default game camera behavior
        float minDistance = __instance.m_minDistance;
        float axis = Input.GetAxis("Mouse ScrollWheel");
        __instance.m_distance -= axis * __instance.m_zoomSens;
        float max = (localPlayer.GetControlledShip() != null) ? __instance.m_maxDistanceBoat : __instance.m_maxDistance;
        __instance.m_distance = Mathf.Clamp(__instance.m_distance, 0f, max);
    }

    internal static void SetCameraTransform(ref GameCamera __instance, Player localPlayer, float dt)
    {
        if (FirstPersonModePlugin.DynamicPerson.isFirstPerson)
        {
            __instance.transform.position = localPlayer.m_head.position + new Vector3(0, 0.2f, 0);
        }
        else
        {
            Vector3 position;
            Quaternion rotation;
            __instance.GetCameraPosition(dt, out position, out rotation);
            __instance.transform.position = position;
            __instance.transform.rotation = rotation;
        }
    }
}