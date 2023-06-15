﻿using System.Collections;
using FirstPersonMode.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace FirstPersonMode;

[HarmonyPatch(typeof(Character), nameof(Character.SetVisible))]
public static class CharacterSetVisiblePatch
{
    private static bool Prefix(ref Character __instance, bool visible)
    {
        if (FirstPersonModePlugin.firstPersonEnabled.Value != FirstPersonModePlugin.Toggle.On) return true;

        if (__instance.m_lodGroup == null || __instance.m_lodVisible == visible ||
            (__instance.IsPlayer() && !visible)) return false;
        __instance.m_lodVisible = visible;
        __instance.m_lodGroup.localReferencePoint = __instance.m_lodVisible
            ? __instance.m_originalLocalRef
            : new Vector3(999999f, 999999f, 999999f);
        return false;
    }
}

[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Awake))]
public static class GameCameraAwakePatch
{
    private static void Postfix(ref GameCamera __instance)
    {
        if (FirstPersonModePlugin.firstPersonEnabled.Value != FirstPersonModePlugin.Toggle.On) return;
        FirstPersonModePlugin.CameraConstants.zoomSens = __instance.m_zoomSens;
        FirstPersonModePlugin.CameraConstants.minDistance = __instance.m_minDistance;
        FirstPersonModePlugin.CameraConstants.maxDistance = __instance.m_maxDistance;
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped))]
public static class UpdateVisEquipment_SetHelmetequiped2Nothing
{
    private static bool _helmetVisRemovedPrefabrestored;
    private static bool _helmetVisRemoved;

    private static void Prefix(
        VisEquipment __instance,
        bool ___m_isPlayer,
        int hash,
        int hairHash,
        ref int ___m_currentHelmetItemHash,
        ref GameObject ___m_helmetItemInstance,
        ref bool ___m_helmetHideHair,
        ref Transform ___m_helmet)
    {
        if (FirstPersonModePlugin.noHeadMode.Value == FirstPersonModePlugin.Toggle.On)
        {
            return;
        }

        if (!__instance.gameObject.GetComponent<Player>())
            return;
        if (__instance.gameObject.GetComponent<Player>() != Player.m_localPlayer)
            return;
        if (___m_currentHelmetItemHash != hash)
            _helmetVisRemoved = false;
        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
        if (!itemPrefab)
            return;
        if (FirstPersonModePlugin.DynamicPerson.isFirstPerson && !_helmetVisRemoved)
        {
            int childCount = itemPrefab.transform.childCount;
            for (int index = 0; index < childCount; ++index)
            {
                Transform child = itemPrefab.transform.GetChild(index);
                if (child.gameObject.name is "attach" or "attach_skin")
                {
                    foreach (Renderer componentsInChild in child.gameObject.GetComponentsInChildren<Renderer>())
                        componentsInChild.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }

            ___m_currentHelmetItemHash = -1;
            _helmetVisRemoved = true;
            _helmetVisRemovedPrefabrestored = false;
        }
        else
        {
            if (FirstPersonModePlugin.DynamicPerson.isFirstPerson || !_helmetVisRemoved)
                return;
            _helmetVisRemoved = false;
            ___m_currentHelmetItemHash = -1;
        }
    }


    private static void Postfix(
        VisEquipment __instance,
        bool ___m_isPlayer,
        int hash,
        int hairHash,
        ref int ___m_currentHelmetItemHash,
        ref GameObject ___m_helmetItemInstance,
        ref bool ___m_helmetHideHair,
        ref Transform ___m_helmet)
    {
        if (FirstPersonModePlugin.noHeadMode.Value == FirstPersonModePlugin.Toggle.On)
        {
            return;
        }

        if (!__instance.gameObject.GetComponent<Player>() || _helmetVisRemovedPrefabrestored)
            return;
        if (__instance.gameObject.GetComponent<Player>() != Player.m_localPlayer)
            return;
        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
        if (!itemPrefab)
            return;
        int childCount = itemPrefab.transform.childCount;
        for (int index = 0; index < childCount; ++index)
        {
            Transform child = itemPrefab.transform.GetChild(index);
            if (child.gameObject.name is "attach" or "attach_skin")
            {
                foreach (Renderer componentsInChild in child.gameObject.GetComponentsInChildren<Renderer>())
                    componentsInChild.shadowCastingMode = ShadowCastingMode.On;
            }
        }

        _helmetVisRemovedPrefabrestored = true;
    }
}

[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
public static class GameCameraUpdatePatch
{
    private static void Postfix(ref GameCamera __instance, float dt)
    {
        if (FirstPersonModePlugin.firstPersonEnabled.Value != FirstPersonModePlugin.Toggle.On) return;

        if (__instance.m_freeFly)
        {
            __instance.UpdateFreeFly(dt);
            __instance.UpdateCameraShake(dt);
            return;
        }

        Player localPlayer = Player.m_localPlayer;

        if (localPlayer == null) return;

        bool isFirstPerson = FirstPersonModePlugin.DynamicPerson.isFirstPerson;
        bool isToggleInitiated = FirstPersonModePlugin.toggleFirstPersonHotkey.Value.IsKeyDown();

        // Scrolling in to first person
        if (__instance.m_distance <= 1 && !isFirstPerson && !isToggleInitiated)
        {
            isFirstPerson = true;
            FirstPersonModePlugin.DynamicPerson.isFirstPerson = true;
            Functions.SetupFP(ref __instance, ref localPlayer);
        }
        // Scrolling out to third person
        else if (__instance.m_minDistance > 0.0 && isFirstPerson && !isToggleInitiated)
        {
            isFirstPerson = false;
            FirstPersonModePlugin.DynamicPerson.isFirstPerson = false;

            // Exiting first person mode
            __instance.m_3rdOffset = FirstPersonModePlugin.DynamicPerson.noFP_3rdOffset;
            __instance.m_fpsOffset = FirstPersonModePlugin.DynamicPerson.noFP_fpsOffset;

            // Revert to stored constants
            __instance.m_minDistance = FirstPersonModePlugin.CameraConstants.minDistance;
            __instance.m_maxDistance = FirstPersonModePlugin.CameraConstants.maxDistance;
            __instance.m_zoomSens = FirstPersonModePlugin.CameraConstants.zoomSens;

            // Default Field Of View value
            __instance.m_fov = 65f;

            // Normalize head scale
            localPlayer.m_head.localScale = Vector3.one;
            localPlayer.m_eye.localScale = Vector3.one;
        }

        // Toggling using hotkey
        if (isToggleInitiated)
        {
            isFirstPerson = !isFirstPerson;


            FirstPersonModePlugin.DynamicPerson.isFirstPerson = isFirstPerson;

            if (isFirstPerson)
            {
                Functions.SetupFP(ref __instance, ref localPlayer);
            }
            else
            {
                // Jump camera back to prevent auto forcing you back into first person mode.
                __instance.m_distance = 1.5f;
                // Exiting first person mode
                __instance.m_3rdOffset = FirstPersonModePlugin.DynamicPerson.noFP_3rdOffset;
                __instance.m_fpsOffset = FirstPersonModePlugin.DynamicPerson.noFP_fpsOffset;

                // Revert to stored constants
                __instance.m_minDistance = FirstPersonModePlugin.CameraConstants.minDistance;
                __instance.m_maxDistance = FirstPersonModePlugin.CameraConstants.maxDistance;
                __instance.m_zoomSens = FirstPersonModePlugin.CameraConstants.zoomSens;

                // Default Field Of View value
                __instance.m_fov = 65f;

                // Normalize head scale
                localPlayer.m_head.localScale = Vector3.one;
                localPlayer.m_eye.localScale = Vector3.one;
            }
        }

        __instance.m_camera.fieldOfView = __instance.m_fov;
        __instance.m_skyCamera.fieldOfView = __instance.m_fov;

        if (!Functions.ShouldIgnoreAdjustments(localPlayer))
        {
            // Camera adjustments based on person mode
            if (FirstPersonModePlugin.DynamicPerson.isFirstPerson)
            {
                Functions.HandleFirstPersonMode(ref __instance);
            }
            else
            {
                Functions.HandleNotFirstPersonMode(ref __instance, localPlayer);
            }
        }

        // Camera positioning based on player state
        if (localPlayer.IsDead() && localPlayer.GetRagdoll())
        {
            __instance.transform.LookAt(localPlayer.GetRagdoll().GetAverageBodyPosition());
        }
        else
        {
            Functions.SetCameraTransform(ref __instance, localPlayer, dt);
        }

        __instance.UpdateCameraShake(dt);
    }
}

[HarmonyPatch(typeof(DamageText), nameof(DamageText.Awake))]
static class DamageTextAwakePatch
{
    static void Prefix(DamageText __instance)
    {
        // Attempt to show the player the damage they are doing by moving the text distance
        __instance.m_maxTextDistance = 100f;
        __instance.m_smallFontDistance = 35f;
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.UpdateShipHud))]
static class UpdateHud_fixshiphud
{
    private static void Postfix(Hud __instance, Player player)
    {
        if (FirstPersonModePlugin.firstPersonEnabled.Value != FirstPersonModePlugin.Toggle.On)
            return;
        Camera mainCamera = Utils.GetMainCamera();
        if (mainCamera == null)
            return;
        if (FirstPersonModePlugin.DynamicPerson.isFirstPerson)
        {
            __instance.m_shipControlsRoot.transform.position = new Vector3(mainCamera.pixelWidth * 0.5f, mainCamera.pixelHeight * 0.2f, 0.0f);
        }
        else
        {
            Ship controlledShip = player.GetControlledShip();
            if (controlledShip != null)
            {
                // Normal position
                __instance.m_shipControlsRoot.transform.position = mainCamera.WorldToScreenPoint(controlledShip.m_controlGuiPos.position);
            }
        }
    }
}