using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Wish;

namespace TimeSpeed
{
    [BepInPlugin("aedenthorn.TimeSpeed", "Time Speed", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> showNotifications;
        public static ConfigEntry<float> timeSpeed;
        public static ConfigEntry<string> hotKeySpeedIncrease;
        public static ConfigEntry<string> hotKeySpeedDecrease;
        public static ConfigEntry<string> hotKeySpeedReset;
        public static ConfigEntry<string> hotKeySpeedToggle;
        public static ConfigEntry<string> hotKeyRewindTime;
        public static ConfigEntry<string> hotKeyAdvanceTime;
        public static ConfigEntry<string> hotKeyModKey;
        //public static ConfigEntry<int> nexusID;

        public static float lastSpeed = 60;

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
            showNotifications = Config.Bind<bool>("Options", "ShowNotifications", true, "Show notifications when changing time speed.");
            timeSpeed = Config.Bind<float>("Options", "TimeSpeed", 60, "Speed multiplier.");
            hotKeySpeedDecrease = Config.Bind<string>("HotKeys", "SpeedDecrease", "[-]", "Hotkey to decrease time speed. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeySpeedIncrease = Config.Bind<string>("HotKeys", "SpeedIncrease", "[+]", "Hotkey to increase time speed. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeySpeedReset = Config.Bind<string>("HotKeys", "SpeedReset", "[/]", "Hotkey to reset time speed to 1x. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeySpeedToggle = Config.Bind<string>("HotKeys", "SpeedToggle", "[*]", "Hotkey to toggle time. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeyRewindTime = Config.Bind<string>("HotKeys", "TimeRewind", "[0]", "Hotkey to rewind time by one hour (use mod key to rewind by 10 min). Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeyAdvanceTime = Config.Bind<string>("HotKeys", "TimeAdvance", "[.]", "Hotkey to advance time by one hour (use mod key to advance by 10 min). Use https://docs.unity3d.com/Manual/class-InputManager.html");
            hotKeyModKey = Config.Bind<string>("HotKeys", "ModKey", "left ctrl", "Modifier key to advance/rewind time by one hour and decrease / increase time speed by 1x. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            lastSpeed = timeSpeed.Value;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(PlaySettings), "daySpeed")]
        [HarmonyPatch(MethodType.Getter)]
        static class daySpeed_Get_Patch
        {
            static bool Prefix(ref float __result)
            {
                if (!modEnabled.Value)
                    return true;

                __result = timeSpeed.Value;

                return false;
            }
        }
        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                if (AedenthornUtils.CheckKeyDown(hotKeyRewindTime.Value))
                {
                    DateTime time = SingletonBehaviour<DayCycle>.Instance.Time;
                    if (time.Hour == 6 && (time.Minute < 10 || AedenthornUtils.CheckKeyHeld(hotKeyModKey.Value, false)))
                    {
                        SendNotification("Cannot rewind time any further!");
                    }
                    else
                    {
                        SingletonBehaviour<DayCycle>.Instance.Time = AedenthornUtils.CheckKeyHeld(hotKeyModKey.Value, false) ? time.AddHours(-1) : time.AddMinutes(-10);
                        SendNotification("Time rewound.");
                    }
                    return;
                }
                if (AedenthornUtils.CheckKeyDown(hotKeyAdvanceTime.Value))
                {
                    DateTime time = SingletonBehaviour<DayCycle>.Instance.Time;
                    if (time.Hour == 23 && (time.Minute > 49 || AedenthornUtils.CheckKeyHeld(hotKeyModKey.Value, false)))
                    {
                        SendNotification("Cannot advance time any further!");
                    }
                    else
                    {
                        SingletonBehaviour<DayCycle>.Instance.Time = AedenthornUtils.CheckKeyHeld(hotKeyModKey.Value, false) ? time.AddHours(1) : time.AddMinutes(10);
                        SendNotification("Time advanced.");
                        return;
                    }
                    return;
                }

                if (AedenthornUtils.CheckKeyDown(hotKeySpeedDecrease.Value))
                {
                    if (timeSpeed.Value == 0)
                    {
                        SendNotification("Time already paused.");
                    }
                    else
                    {
                        timeSpeed.Value = Math.Max(0, timeSpeed.Value  - (AedenthornUtils.CheckKeyHeld(hotKeyModKey.Value, false) ? 60 : 6));
                        SendNotification($"Time speed decreased to {GetTimeString()}.");
                    }

                    lastSpeed = timeSpeed.Value;
                }
                else if (AedenthornUtils.CheckKeyDown(hotKeySpeedIncrease.Value))
                {
                    timeSpeed.Value = timeSpeed.Value + (AedenthornUtils.CheckKeyHeld(hotKeyModKey.Value, false) ? 60 : 6);
                    SendNotification($"Time speed increased to {GetTimeString()}.");
                    lastSpeed = timeSpeed.Value;
                }
                else if (AedenthornUtils.CheckKeyDown(hotKeySpeedReset.Value))
                {
                    if (timeSpeed.Value == 60)
                    {
                        SendNotification("Time speed already at 1x.");
                    }
                    else
                    {
                        timeSpeed.Value = 60;
                        SendNotification($"Time speed reset to 1x.");
                    }
                    lastSpeed = timeSpeed.Value;
                }
                else if (AedenthornUtils.CheckKeyDown(hotKeySpeedToggle.Value))
                {
                    if (timeSpeed.Value == 0)
                    {
                        if(lastSpeed == 0)
                            timeSpeed.Value = 60;
                        else
                            timeSpeed.Value = lastSpeed;

                        lastSpeed = timeSpeed.Value;
                        SendNotification($"Time restarted ({GetTimeString()}).");
                    }
                    else
                    {
                        lastSpeed = timeSpeed.Value;
                        timeSpeed.Value = 0;
                        SendNotification($"Time speed stopped.");
                    }
                }
            }

        }
        private static void SendNotification(string message)
        {
            if (showNotifications.Value)
                SingletonBehaviour<NotificationStack>.Instance.SendNotification(message);
        }
        private static string GetTimeString()
        {
            return Math.Round(timeSpeed.Value / 60f, 1) + "x";
        }
    }
}
