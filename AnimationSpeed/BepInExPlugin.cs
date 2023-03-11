using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Wish;

namespace AnimationSpeed
{
    [BepInPlugin("aedenthorn.AnimationSpeed", "Animation Speed", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> speedMultScythe;
        public static ConfigEntry<float> speedMultSword;
        public static ConfigEntry<float> speedMultPickaxe;
        public static ConfigEntry<float> speedMultAxe;
        public static ConfigEntry<float> speedMultHoe;
        public static ConfigEntry<float> speedMultWateringCan;
        //public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            speedMultAxe = Config.Bind<float>("Speeds", "SpeedMultAxe", 1, "Speed multiplier for axes.");
            speedMultHoe = Config.Bind<float>("Speeds", "SpeedMultHoe", 1, "Speed multiplier for hoes.");
            speedMultScythe = Config.Bind<float>("Speeds", "SpeedMultScythe", 1, "Speed multiplier for scythe.");
            speedMultSword = Config.Bind<float>("Speeds", "SpeedMultSword", 1, "Speed multiplier for sword.");
            speedMultPickaxe = Config.Bind<float>("Speeds", "SpeedMultPickaxe", 1, "Speed multiplier for pickaxes.");
            speedMultWateringCan = Config.Bind<float>("Speeds", "SpeedMultWateringCan", 1, "Speed multiplier for watering cans.");
            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Weapon), "AttackSpeed")]
        static class SetAnimation_Patch
        {
            static void Postfix(WeaponType ____weaponType, ref float __result, ref float ____frameRate)
            {
                if (!modEnabled.Value)
                    return;

                //Dbgl($"Type: {____weaponType} rate: {____frameRate}");
                switch (____weaponType)
                {
                    case WeaponType.Axe:
                        __result *= speedMultAxe.Value;
                        break;
                    case WeaponType.Hoe:
                        __result *= speedMultHoe.Value;
                        break;
                    case WeaponType.Sword:
                        __result *= speedMultSword.Value;
                        break;
                    case WeaponType.Scythe:
                        __result *= speedMultScythe.Value;
                        break;
                    case WeaponType.Pickaxe:
                        __result *= speedMultPickaxe.Value;
                        break;
                    case WeaponType.WateringCan:
                        __result *= speedMultWateringCan.Value;
                        break;
                }
            }
        }
    }
}
