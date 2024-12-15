using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace FirstPersonMode.Util;

public class PPCompat
{
    public static bool IsInAEM = false;

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("Azumatt_and_ValheimPlusDevs.PerfectPlacement", out PluginInfo ppPluginInfo)) return;
        if (ppPluginInfo == null || ppPluginInfo.Instance == null) return;
        if (ppPluginInfo.Metadata.Version.Major > 1 || (ppPluginInfo.Metadata.Version.Major == 1 && ppPluginInfo.Metadata.Version.Minor > 1) ||
            (ppPluginInfo.Metadata.Version.Major == 1 && ppPluginInfo.Metadata.Version.Minor == 1 && ppPluginInfo.Metadata.Version.Build >= 10))
        {
            FirstPersonModePlugin.Instance._harmony.PatchAll(typeof(PPCompat));
        }

    }

    [HarmonyPatch("PerfectPlacement.Patches.AEM, PerfectPlacement", "IsInAemMode"), HarmonyPostfix]
    public static void IsInAemMode(ref bool __result)
    {
        if (Player.m_localPlayer != null)
        {
            IsInAEM = __result;
        }
    }
}