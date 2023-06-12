using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace FirstPersonMode
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class FirstPersonModePlugin : BaseUnityPlugin
    {
        internal const string ModName = "FirstPersonMode";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource FirstPersonModeLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        public enum Toggle
        {
            On = 1,
            Off = 0
        }
        
        // Struct to hold the dynamic values
        public struct DynamicPerson
        {
            // Are we in first person or not
            public static bool isFirstPerson = false;
            // Holder for old m_3rdOffset value
            public static Vector3 noFP_3rdOffset = Vector3.zero;
            // Holder for old m_fpsOffset value
            public static Vector3 noFP_fpsOffset = Vector3.zero;
        };

        // Struct to hold Camera constants
        public struct CameraConstants
        {
            // Valheim zoom thingy value
            public static float zoomSens = 10f;
            // Min and max distance of camera
            public static float minDistance = 1f;
            public static float maxDistance = 8f;
            // Near Clip Plane max and min
            public static float nearClipPlaneMax = 0.02f;
            public static float nearClipPlaneMin = 0.01f;
        };

        public void Awake()
        {

            // Config for First Person being enabled
            firstPersonEnabled = config("2 - Toggles", "Enable First Person", Toggle.On, "If on, First Person is enabled.");
            noHeadMode = config("2 - Toggles", "Hide Head", Toggle.Off, "If on, the camera will not use the culling mode and will instead shrink the head to hide it. This method is a bit better overall as your armor isn't see through, but looks a little weird. Headless people always do.");
            
            // Default FOV
            defaultFOV = config("3 - Camera", "Default FOV", 65.0f, "Default FOV for First Person.");
            NearClipPlaneMinConfig = config("3 - Camera", "NearClipPlaneMin", 0.3f, "Adjusts the nearest distance at which objects are rendered in first person view. Increase to reduce body visibility; too high might clip nearby objects.");
            NearClipPlaneMaxConfig = config("3 - Camera", "NearClipPlaneMax", 0.3f, "Adjusts the nearest distance at which objects are rendered in first person view. Increase to reduce body visibility; too high might clip nearby objects."); 
            
            // Consider a reasonable range of rotation like -90 to +90 for both pitch and yaw.
            /*MinPitchConfig = Config.Bind("3 - Camera", "MinPitch", -60f, "Minimum vertical rotation angle in degrees");
            MaxPitchConfig = Config.Bind("3 - Camera", "MaxPitch", 60f, "Maximum vertical rotation angle in degrees");
            MinYawConfig = Config.Bind("3 - Camera", "MinYaw", -90f, "Minimum horizontal rotation angle in degrees");
            MaxYawConfig = Config.Bind("3 - Camera", "MaxYaw", 90f, "Maximum horizontal rotation angle in degrees");*/


            // Hotkeys for turning on FOV and controlling the FOV
            toggleFirstPersonHotkey = config("4 - Keyboard Shortcuts", "Toggle First Person Shortcut", new KeyboardShortcut(KeyCode.F, KeyCode.LeftShift), "Keyboard Shortcut needed to toggle First Person.");
            raiseFOVHotkey = config("4 - Keyboard Shortcuts", "Raise FOV Shortcut", new KeyboardShortcut(KeyCode.PageUp, KeyCode.LeftShift), "Keyboard Shortcut needed to raise FOV.");
            lowerFOVHotkey = config("4 - Keyboard Shortcuts", "Lower FOV Shortcut", new KeyboardShortcut(KeyCode.PageDown, KeyCode.LeftShift), "Keyboard Shortcut needed to lower FOV.");


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                FirstPersonModeLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                FirstPersonModeLogger.LogError($"There was an issue loading your {ConfigFileName}");
                FirstPersonModeLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions
        internal static ConfigEntry<Toggle> firstPersonEnabled = null!;
        internal static ConfigEntry<Toggle> noHeadMode = null!;
        internal static ConfigEntry<float> defaultFOV = null!;
        internal static ConfigEntry<KeyboardShortcut> toggleFirstPersonHotkey = null!;
        internal static ConfigEntry<KeyboardShortcut> raiseFOVHotkey = null!;
        internal static ConfigEntry<KeyboardShortcut> lowerFOVHotkey = null!;
        internal static ConfigEntry<float> NearClipPlaneMinConfig = null!;
        internal static ConfigEntry<float> NearClipPlaneMaxConfig = null!;
        internal static ConfigEntry<float> MinPitchConfig = null!;
        internal static ConfigEntry<float> MaxPitchConfig = null!;
        internal static ConfigEntry<float> MinYawConfig = null!;
        internal static ConfigEntry<float> MaxYawConfig = null!;
        internal static ConfigEntry<Vector3> eyeOffset = null!;



        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
            //var configEntry = Config.Bind(group, name, value, description);

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description)
        {
            return config(group, name, value, new ConfigDescription(description));
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase> CustomDrawer = null!;
        }

        #endregion
    }
}