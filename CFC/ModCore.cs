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
        internal const string ModVersion = "1.0.6";
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
        internal static ConfigEntry<int>? FuelingDistance = null!;
        internal static ConfigEntry<int>? LowFuelValue = null!;
        internal static ConfigEntry<float>? SearchInterval = null!;
        internal static ConfigEntry<bool>? ShouldSearchWardedAreas = null!;
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            ServerConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
            ChestDistance = config("2 - CraftFromChest", "Distance To Check", 15,
                new ConfigDescription("This is how far to check chests away from players no clue why bep displays this as % its in meters",
                    new AcceptableValueRange<int>(0, 100))); 
            FuelingDistance = config("2 - FuelFromChest", "Distance To Check", 15,
                new ConfigDescription("This is how far to check chests away from players fore fire fuel no clue why bep displays this as % its in meters",
                    new AcceptableValueRange<int>(0, 100)));
            LowFuelValue = config("2 - FuelFromChest LowFuel", "What count of fuel to start hunting for more in chests", 1,
                new ConfigDescription("This the volume of fuel when mod starts hunting for more fuel (wood)"));
            SearchInterval = config("2 - FuelFromChest Search Interval", "How often should mod hunt for fuel in chests", 0.05f,
                new ConfigDescription("this number is in seconds so 1.0 is 1 second the default setting is to check every quarter of a second"));
            ShouldSearchWardedAreas = config("1 - General", "Should mod hunt warded chests", false,
                new ConfigDescription(
                    "This setting dictates whether the mod should hunt chests in a warded area or not"));
            configSync.AddLockingConfigEntry(ServerConfigLocked);
        }
    }
}
