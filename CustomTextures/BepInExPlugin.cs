using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using Wish;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "0.5.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> dumpNames;
        //public static ConfigEntry<int> nexusID;

        public static Dictionary<string, string> customTextureDict;
        public static Dictionary<string, Texture2D> cachedTextureDict = new Dictionary<string, Texture2D>();
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
            dumpNames = Config.Bind<bool>("General", "DumpNames", false, "Dump names to BepInEx\\plugins\\CustomTextures\\names.txt");
            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);

            LoadCustomTextures();

            foreach(var t in typeof(ClothingLayerData).GetTypeInfo().GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach(var m in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m.Name.Contains("LoadClothingSprites"))
                    {
                        Dbgl($"Found method {t.Name}:{m.Name}");
                        harmony.Patch(
                            original: m,
                            transpiler: new HarmonyMethod(typeof(BepInExPlugin), nameof(BepInExPlugin.ClothingLayerData_LoadClothingSprites_Transpiler))
                        );
                    }
                }
            }
        }

        private void LoadCustomTextures()
        {
            customTextureDict = new Dictionary<string, string>();
            string path = AedenthornUtils.GetAssetPath(this, true);
            foreach(string file in Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
            {
                customTextureDict.Add(Path.GetFileNameWithoutExtension(file), file);
            }
            Dbgl($"Loaded {customTextureDict.Count} textures");

        }
        private static Texture2D GetTexture(string path)
        {
            if (cachedTextureDict.TryGetValue(path, out var tex))
                return tex;
            TextureCreationFlags flags = new TextureCreationFlags();
            tex = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, flags);
            tex.LoadImage(File.ReadAllBytes(path));
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.wrapModeU = TextureWrapMode.Clamp;
            tex.wrapModeV = TextureWrapMode.Clamp;
            tex.wrapModeW = TextureWrapMode.Clamp;
            cachedTextureDict[path] = tex;
            return tex;
        }
        public static IEnumerable<CodeInstruction> ClothingLayerData_LoadClothingSprites_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Dbgl("transpiling ClothingLayerData.LoadClothingSprites");

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.PropertyGetter(typeof(AsyncOperationHandle<ClothingLayerSprites>), nameof(AsyncOperationHandle<ClothingLayerSprites>.Result)))
                {
                    Dbgl("Found getter");
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.HandleResult))));
                    i++;
                }
            }

            return codes.AsEnumerable();
        }

        private static ClothingLayerSprites HandleResult(ClothingLayerSprites result)
        {
            if (result?._clothingLayerInfo == null)
            {
                return result;
            }
            for (int i = 0; i < result._clothingLayerInfo.Count; i++)
            {
                if (result._clothingLayerInfo[i].sprites == null)
                    continue;
                for (int j = 0; j < result._clothingLayerInfo[i].sprites.Length; j++)
                {
                    var name = result._clothingLayerInfo[i].sprites[j]?.texture?.name;
                    if (name is null)
                        continue;
                    if (customTextureDict.TryGetValue(name, out string path))
                    {
                        var oldSprite = result._clothingLayerInfo[i].sprites[j];
                        Dbgl($"replacing sprite {result._clothingLayerInfo[i].clothingLayer} {name}");
                        Sprite newSprite = Sprite.Create(GetTexture(path), oldSprite.rect, new Vector2(oldSprite.pivot.x / oldSprite.rect.width, oldSprite.pivot.y / oldSprite.rect.height), oldSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, oldSprite.border, true);
                        newSprite.name = oldSprite.name;
                        result._clothingLayerInfo[i].sprites[j] = newSprite;
                    }
                }
            }
            return result;
        }

        [HarmonyPatch(typeof(CharacterClothingStyles), "SetupClothingStyleDictionary")]
        static class CharacterClothingStyles_SetupClothingStyleDictionary_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value || !dumpNames.Value)
                    return;
                
                Dbgl($"Setup clothing style dict");

                string path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "names.txt");
                File.WriteAllText(path, "");
                
                HashSet<string> names = new HashSet<string>();
                var fi = AccessTools.Field(typeof(ClothingLayerData), "clothingLayerSprites");
                var fi2 = AccessTools.Field(typeof(ClothingLayerData), "loadHandle");
                for (int i = 0; i < CharacterClothingStyles.AllClothing.Length; i++)
                {
                    try
                    {
                        ClothingLayerData cld = CharacterClothingStyles.AllClothing[i];
                        var loadHandle = Addressables.LoadAssetAsync<ClothingLayerSprites>((AssetReferenceClothingLayerSprites)fi.GetValue(cld));
                        if (!loadHandle.IsValid() || loadHandle.Result == null)
                        {
                            fi2.SetValue(cld, loadHandle);
                            List<string> newNames = new List<string>();
                            loadHandle.Completed += delegate (AsyncOperationHandle<ClothingLayerSprites> x)
                            {
                                if (loadHandle.Result?._clothingLayerInfo != null)
                                {
                                    foreach (ClothingLayerInfo clothingLayerInfo in loadHandle.Result._clothingLayerInfo)
                                    {
                                        try
                                        {
                                            foreach (var s in clothingLayerInfo.sprites)
                                            {
                                                try
                                                {
                                                    if (names.Add(s.texture.name))
                                                    {
                                                        newNames.Add(s.texture.name);
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                if (newNames.Any())
                                {
                                    Dbgl($"writing {newNames.Count} new names");
                                    File.AppendAllLines(path, newNames);
                                }
                            };
                        }
                    }
                    catch { }
                }

                dumpNames.Value = false;
            }
        }
    }
}
