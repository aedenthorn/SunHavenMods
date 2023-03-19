using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Wish;

namespace Fixes
{
    [BepInPlugin("aedenthorn.Fixes", "Fixes", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

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
            

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(DataTile), nameof(DataTile.Clone))]
        static class DataTile_Clone_Patch
        {
            static bool Prefix(DataTile __instance)
            {
                if (!modEnabled.Value)
                    return true;

                var tile = (DataTile)DataTile.CreateInstance(typeof(DataTile));
                tile.treeType = __instance.treeType;
                tile.farmable = __instance.farmable;
                tile.forageType = __instance.forageType;
                tile.waterType = __instance.waterType;
                tile.placementType = __instance.placementType;
                tile.foliageType = __instance.foliageType;
                return tile;
            }
        }
        [HarmonyPatch(typeof(GameManager), "GetDataTile")]
        private static class GameManager_GetDataTile_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl("transpiling GameManager.GetDataTile");

                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(SerializedDataTile), nameof(SerializedDataTile.ToDataTile)))
                    {
                        Dbgl("Found method");
                        codes[i].operand = AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetDataTile));
                        codes.RemoveAt(i-2);
                        codes.RemoveAt(i-2);
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }
        [HarmonyPatch(typeof(Platform), "LateUpdate")]
        private static class Platform_LateUpdate_Patch
        {
            static void Prefix(Platform __instance, List<PlatformCollider> ___colliders)
            {
                if (!modEnabled.Value)
                    return;

                for(int i = ___colliders.Count - 1; i >= 0; i--)
                {
                    if (___colliders[i]?.collider == null)
                    {
                        ___colliders.RemoveAt(i);
                    }
                }
            }
        }

        private static DataTile GetDataTile(SerializedDataTile stile)
        {
            var tile = (DataTile)DataTile.CreateInstance(typeof(DataTile));
            tile.treeType = stile.treeType;
            tile.farmable = stile.farmable;
            tile.forageType = stile.forageType;
            tile.waterType = stile.waterType;
            tile.placementType = stile.placementType;
            tile.foliageType = stile.foliageType;
            return tile;
        }
    }
}
