using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Wish;

namespace MovementSpeed
{
    [BepInPlugin("aedenthorn.MovementSpeed", "Movement Speed", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> showNotifications;
        public static ConfigEntry<float> speedMult;
        public static ConfigEntry<string> hotKeySpeedIncrease;
        public static ConfigEntry<string> hotKeySpeedDecrease;
        public static ConfigEntry<string> hotKeySpeedReset;
        public static ConfigEntry<string> hotKeyModKey;
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

            speedMult = Config.Bind<float>("Options", "TimeSpeed", 1, "Speed multiplier.");
            showNotifications = Config.Bind<bool>("Options", "ShowNotifications", true, "Show notifications when changing time speed.");

            hotKeySpeedDecrease = Config.Bind<string>("HotKeys", "SpeedDecrease", "-", "Hotkey to decrease movement speed by 1x. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeySpeedIncrease = Config.Bind<string>("HotKeys", "SpeedIncrease", "=", "Hotkey to increase movement speed by 1x. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeySpeedReset = Config.Bind<string>("HotKeys", "SpeedReset", "\\", "Hotkey to reset movement speed to 1x. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeyModKey = Config.Bind<string>("HotKeys", "ModKey", "left ctrl", "Modifier key to decrease / increase movement speed by 0.1x. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.FinalMovementSpeed))]
        [HarmonyPatch(MethodType.Getter)]
        static class FinalMovementSpeed_Get_Patch
        {
            static void Postfix(ref float __result)
            {
                if (!modEnabled.Value)
                    return;

                __result *= speedMult.Value;
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;


                if (AedenthornUtils.CheckKeyDown(hotKeySpeedDecrease.Value))
                {
                    if (speedMult.Value <= 0.1f)
                    {
                        SendNotification("Movement already at slowest.");
                    }
                    else
                    {
                        speedMult.Value = Math.Max(0.1f, speedMult.Value - (AedenthornUtils.CheckKeyHeld(hotKeyModKey.Value, false) ? 0.1f : 1f));
                        SendNotification($"Movement speed decreased to {speedMult.Value}x.");
                    }
                }
                else if (AedenthornUtils.CheckKeyDown(hotKeySpeedIncrease.Value))
                {
                    speedMult.Value = speedMult.Value + (AedenthornUtils.CheckKeyHeld(hotKeyModKey.Value, false) ? 0.1f : 1f);
                    SendNotification($"Movement speed increased to {speedMult.Value}x.");
                }
                else if (AedenthornUtils.CheckKeyDown(hotKeySpeedReset.Value))
                {
                    if (speedMult.Value == 1)
                    {
                        SendNotification("Movement speed already at 1x.");
                    }
                    else
                    {
                        speedMult.Value = 1;
                        SendNotification($"Movement speed reset to 1x.");
                    }
                }
            }
        }
        private static void SendNotification(string message)
        {
            if (showNotifications.Value)
                SingletonBehaviour<NotificationStack>.Instance.SendNotification(message);
        }
    }
}
