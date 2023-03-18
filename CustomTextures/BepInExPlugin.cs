using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Wish;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "0.4.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        //public static ConfigEntry<int> nexusID;

        public static Dictionary<string, Texture2D> customTextureDict;
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
            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            LoadCustomTextures();
        }

        private void LoadCustomTextures()
        {
            customTextureDict = new Dictionary<string, Texture2D>();
            string path = AedenthornUtils.GetAssetPath(this, true);
            foreach(string file in Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
            {
                TextureCreationFlags flags = new TextureCreationFlags();
                Texture2D tex = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, flags);
                tex.LoadImage(File.ReadAllBytes(file));
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.wrapModeU = TextureWrapMode.Clamp;
                tex.wrapModeV = TextureWrapMode.Clamp;
                tex.wrapModeW = TextureWrapMode.Clamp;
                customTextureDict.Add(Path.GetFileNameWithoutExtension(file), tex);
            }
            Dbgl($"Loaded {customTextureDict.Count} textures");
        }

        [HarmonyPatch(typeof(CharacterClothingStyles), "SetupClothingStyleDictionary")]
        static class ClothingLayerData_ClotherLayerInfo_Patch
        {
            static void Prefix()
            {
                foreach (ClothingLayerData cld in CharacterClothingStyles.AllClothing)
                {
                    foreach (ClothingLayerInfo cli in cld.ClothingLayerInfo)
                    {
                        for(int i = 0; i < cli.sprites.Length; i++)
                        {
                            if (cli.sprites[i] && customTextureDict.ContainsKey(cli.sprites[i].texture.name))
                            {
                                Dbgl($"replacing sprite {cli.clothingLayer} {cli.sprites[i].name}");
                                customTextureDict[cli.sprites[i].texture.name].name = cli.sprites[i].texture.name;
                                //File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace, cli.sprites[i].name+".png"), cli.sprites[i].texture.EncodeToPNG());
                                Sprite newSprite = Sprite.Create(customTextureDict[cli.sprites[i].texture.name], cli.sprites[i].rect, new Vector2(cli.sprites[i].pivot.x / cli.sprites[i].rect.width, cli.sprites[i].pivot.y / cli.sprites[i].rect.height), cli.sprites[i].pixelsPerUnit, 0, SpriteMeshType.FullRect, cli.sprites[i].border, true);
                                newSprite.name = cli.sprites[i].name;
                                cli.sprites[i] = newSprite;
                            }
                        }
                    }
                }
            }
        }
    }
}
