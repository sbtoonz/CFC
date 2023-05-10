using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;

namespace CFC
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class CFCMod : BaseUnityPlugin
    {
        internal const string ModName = "CFCMod";
        internal const string ModVersion = "1.0.2";
        private const string ModGUID = "CFCMod";
        private static Harmony harmony = null!;

        #region ConfigSync
        ConfigSync configSync = new(ModGUID) 
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion};
        internal static ConfigEntry<bool> ServerConfigLocked = null!;
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        #endregion

        internal static ConfigEntry<int>? ChestDistance = null!;
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            ServerConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
            ChestDistance = config("2 - CraftFromChest", "Distance To Check", 15,
                new ConfigDescription("This is how far to check chests away from players no clue why bep displays this as % its in meters",
                    new AcceptableValueRange<int>(0, 100)));
            configSync.AddLockingConfigEntry(ServerConfigLocked);
        }
    }
}
