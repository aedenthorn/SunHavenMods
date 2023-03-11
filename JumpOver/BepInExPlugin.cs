using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Wish;

namespace JumpOver
{
    [BepInPlugin("aedenthorn.JumpOver", "Jump Over", "1.0.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> airSkipOver;
        public static ConfigEntry<bool> jumpOver;
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
            airSkipOver = Config.Bind<bool>("Options", "AirSkipOver", true, "Cross over when using air skip ability.");
            jumpOver = Config.Bind<bool>("Options", "JumpOver", true, "Cross over when jumping normally.");
            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_LateUpdate_Patch
        {
            static void Postfix(Player __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!__instance.Grounded && ((airSkipOver.Value &&  __instance.AirSkipsUsed >= __instance.MaxAirSkips) || (jumpOver.Value && __instance.AirSkipsUsed < __instance.MaxAirSkips)))
                {
                    __instance.rigidbody.bodyType = RigidbodyType2D.Kinematic;
                }
                else
                {
                    __instance.rigidbody.bodyType = RigidbodyType2D.Dynamic;
                }
            }
        }
    }
}
