using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace FirstPersonMode
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("Azumatt.BuildCameraCHE", BepInDependency.DependencyFlags.SoftDependency)]
    public class FirstPersonModePlugin : BaseUnityPlugin
    {
        internal const string ModName = "FirstPersonMode";
        internal const string ModVersion = "1.2.7";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static bool CHEIsLoaded;
        private Assembly _cheAssembly = null!;
        public static MethodInfo? CHEInBuildMode;
        public static readonly ManualLogSource FirstPersonModeLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        // Struct to hold the dynamic values
        public struct DynamicPerson
        {
            // Are we in first person or not
            public static bool IsFirstPerson = false;

            // Holder for old m_3rdOffset value
            public static Vector3 NoFp3RdOffset = Vector3.zero;

            // Holder for old m_fpsOffset value
            public static Vector3 NoFpFPSOffset = Vector3.zero;

            public static float MaxDeviation = 40f;

            public static Quaternion PlayerRotation = Quaternion.identity;

            public static Rigidbody PlayerRigidbody = null!;
        };

        // Struct to hold Camera constants
        public struct CameraConstants
        {
            // Valheim zoom thingy value
            public static float ZoomSens = 10f;

            // Min and max distance of camera
            public static float MinDistance = 1.0f;

            public static float MaxDistance = 8f;

            // Near Clip Plane max and min
            public static float NearClipPlaneMax = 0.02f;
            public static float NearClipPlaneMin = 0.01f;
        };

        public void Awake()
        {
            // Config for First Person being enabled
            FirstPersonEnabled = config("1 - Toggles", "Enable First Person", Toggle.On, "If on, First Person is enabled.");
            NoHeadMode = config("1 - Toggles", "Hide Head", Toggle.Off, "If on, the camera will not use the culling mode and will instead shrink the head to hide it. This method is a bit better overall as your armor isn't see through, but looks a little weird. Headless people always do.");

            // Default FOV
            DefaultFOV = config("2 - Camera", "Default FOV", 65.0f, "Default FOV for First Person.");
            NearClipPlaneMinConfig = config("2 - Camera", "NearClipPlaneMin", 0.17f, "Adjusts the nearest distance at which objects are rendered in first person view. Increase to reduce body visibility; too high might clip nearby objects.");
            NearClipPlaneMaxConfig = config("2 - Camera", "NearClipPlaneMax", 0.17f, "Adjusts the nearest distance at which objects are rendered in first person view. Increase to reduce body visibility; too high might clip nearby objects.");


            // Hotkeys for turning on FOV and controlling the FOV
            ToggleFirstPersonHotkey = config("3 - Keyboard Shortcuts", "Toggle First Person Shortcut", new KeyboardShortcut(KeyCode.H, KeyCode.LeftShift), "Keyboard Shortcut needed to toggle First Person.");
            RaiseFOVHotkey = config("3 - Keyboard Shortcuts", "Raise FOV Shortcut", new KeyboardShortcut(KeyCode.PageUp, KeyCode.LeftShift), "Keyboard Shortcut needed to raise FOV.");
            LowerFOVHotkey = config("3 - Keyboard Shortcuts", "Lower FOV Shortcut", new KeyboardShortcut(KeyCode.PageDown, KeyCode.LeftShift), "Keyboard Shortcut needed to lower FOV.");

            CHEIsLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Azumatt.BuildCameraCHE");


            if (CHEIsLoaded)
            {
                _cheAssembly = BepInEx.Bootstrap.Chainloader.PluginInfos["Azumatt.BuildCameraCHE"].Instance.GetType().Assembly;

                // Get the type for the BuildCameraCHEPlugin, go into the Valheim_Build_Camera namespace, and the Utils class. Get the public method InBuildMode
                CHEInBuildMode = _cheAssembly.GetType("Valheim_Build_Camera.Utils")?.GetMethod("InBuildMode", BindingFlags.Public | BindingFlags.Static);
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void Start()
        {
            AutoDoc();
        }

        private void AutoDoc()
        {
#if DEBUG
            // Store Regex to get all characters after a [
            Regex regex = new(@"\[(.*?)\]");

            // Strip using the regex above from Config[x].Description.Description
            string Strip(string x) => regex.Match(x).Groups[1].Value;
            StringBuilder sb = new();
            string lastSection = "";
            foreach (ConfigDefinition x in Config.Keys)
            {
                // skip first line
                if (x.Section != lastSection)
                {
                    lastSection = x.Section;
                    sb.Append($"{Environment.NewLine}`{x.Section}`{Environment.NewLine}");
                }

                sb.Append($"\n{x.Key} [{Strip(Config[x].Description.Description)}]" +
                          $"{Environment.NewLine}   * {Config[x].Description.Description.Replace("[Synced with Server]", "").Replace("[Not Synced with Server]", "")}" +
                          $"{Environment.NewLine}     * Default Value: {Config[x].GetSerializedValue()}{Environment.NewLine}");
            }

            File.WriteAllText(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"{ModName}_AutoDoc.md"),
                sb.ToString());
#endif
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

        internal static ConfigEntry<Toggle> FirstPersonEnabled = null!;
        internal static ConfigEntry<Toggle> NoHeadMode = null!;
        internal static ConfigEntry<float> DefaultFOV = null!;
        internal static ConfigEntry<KeyboardShortcut> ToggleFirstPersonHotkey = null!;
        internal static ConfigEntry<KeyboardShortcut> RaiseFOVHotkey = null!;
        internal static ConfigEntry<KeyboardShortcut> LowerFOVHotkey = null!;
        internal static ConfigEntry<float> NearClipPlaneMinConfig = null!;
        internal static ConfigEntry<float> NearClipPlaneMaxConfig = null!;


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
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase> CustomDrawer = null!;
        }

        #endregion
    }
}