using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using Wish;

namespace CharacterEdit
{
    [BepInPlugin("aedenthorn.CharacterEdit", "Character Edit", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> hotKey;

        public static CharacterCreation characterCreation = null;
        private static string lastName;
        private static Sprite buttonSprite;

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

            hotKey = Config.Bind<string>("HotKeys", "HotKey", "y", "Hotkey to open character creation UI. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            LoadTexture();

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private static void LoadTexture()
        {
            string path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "button.png");
            if (!File.Exists(path))
            {
                Dbgl($"Couldn't find file at {path}");
                return;
            }
            TextureCreationFlags flags = new TextureCreationFlags();
            Texture2D tex = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, flags);
            tex.LoadImage(File.ReadAllBytes(path));
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.wrapModeU = TextureWrapMode.Clamp;
            tex.wrapModeV = TextureWrapMode.Clamp;
            tex.wrapModeW = TextureWrapMode.Clamp;

            buttonSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }


        [HarmonyPatch(typeof(SavePanel), nameof(SavePanel.SetPlayerImage))]
        static class SavePanel_SetPlayerImage_Patch
        {
            static void Postfix(SavePanel __instance, CharacterData character)
            {
                MainMenuController.Instance.StartCoroutine(FixButtons(__instance, character));
            }
        }

        private static IEnumerator FixButtons(SavePanel savePanel, CharacterData character)
        {
            int index = SingletonBehaviour<GameSave>.Instance.Saves.FindIndex(s => s.characterData == character);
            Dbgl($"fixing panel {index}");
            GameObject button = Instantiate(savePanel.deleteButton.gameObject, savePanel.deleteButton.transform.parent);
            button.GetComponent<RectTransform>().anchoredPosition -= new Vector2(savePanel.deleteButton.gameObject.GetComponent<RectTransform>().rect.width, 0);
            button.GetComponent<Image>().sprite = buttonSprite;
            button.GetComponent<UnityEngine.UI.Button>().onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate ()
            {

                SingletonBehaviour<GameSave>.Instance.LoadCharacter(index);
                MainMenuController.Instance.EnableMenu(MainMenuController.Instance.newCharacterMenu);
                SetupCharacter();
                MainMenuController.Instance.backCharacterButton.onClick.AddListener(SetupButtons);
                MainMenuController.Instance.confirmCharacterButton.onClick.RemoveAllListeners();
                MainMenuController.Instance.confirmCharacterButton.onClick.AddListener(delegate ()
                {
                    Dictionary<ClothingLayer, ClothingLayerData> currentClothingDictionary = (Dictionary<ClothingLayer, ClothingLayerData>)AccessTools.Field(typeof(CharacterCreation), "currentClothingDictionary").GetValue(MainMenuController.Instance.characterCreation);
                    foreach (ClothingLayer clothingLayer in (ClothingLayer[])Enum.GetValues(typeof(ClothingLayer)))
                    {
                        ClothingLayerData style = SingletonBehaviour<CharacterClothingStyles>.Instance.GetStyle(clothingLayer, MainMenuController.Instance.characterCreation.CurrentCharacter.styleData[(byte)clothingLayer]);
                        if (style)
                        {
                            Dbgl($"Setting: {clothingLayer}: {MainMenuController.Instance.characterCreation.CurrentCharacter.styleData[(byte)clothingLayer]}");
                            currentClothingDictionary[clothingLayer] = style;
                            MainMenuController.Instance.characterCreation.SetClothingLayerData(style, clothingLayer, false);
                        }
                    }
                    foreach (ClothingLayer clothingLayer in (ClothingLayer[])Enum.GetValues(typeof(ClothingLayer)))
                    {
                        Dbgl($"After: {clothingLayer}: {MainMenuController.Instance.characterCreation.CurrentCharacter.styleData[(byte)clothingLayer]} {SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.styleData[(byte)clothingLayer]}");
                    }

                    SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData = MainMenuController.Instance.characterCreation.CurrentCharacter;
                    SingletonBehaviour<GameSave>.Instance.WriteCharacterToFile(false, false);
                    if (MainMenuController.Instance.characterCreation.CurrentCharacter.characterName != lastName)
                    {
                        Dbgl($"Name change, deleting old profile {lastName}");

                        string path = Path.Combine(Application.persistentDataPath, "Saves", lastName + ".save");
                        if (File.Exists(path))
                            File.Delete(path);

                        SingletonBehaviour<GameSave>.Instance.LoadAllCharacters();
                    }
                    MainMenuController.Instance.EnableMenu(MainMenuController.Instance.homeMenu);
                    MainMenuController.Instance.SetupButtons();
                });
            });

            yield break;
        }

        private static void SetupButtons()
        {
            MainMenuController.Instance.SetupButtons();
            MainMenuController.Instance.backCharacterButton.onClick.RemoveListener(SetupButtons);
        }

        private static void SetupCharacter()
        {
            lastName = MainMenuController.Instance.characterCreation.CurrentCharacter.characterName;
            MainMenuController.Instance.characterCreation.nameInputField.text = lastName;

            if(!MainMenuController.Instance.characterCreation.CurrentCharacter.male)
                MainMenuController.Instance.characterCreation.SetFemale();
            else
                MainMenuController.Instance.characterCreation.SetMale();

            Dictionary<ClothingLayer, ClothingLayerData> currentClothingDictionary = (Dictionary<ClothingLayer, ClothingLayerData>)AccessTools.Field(typeof(CharacterCreation), "currentClothingDictionary").GetValue(MainMenuController.Instance.characterCreation);
            foreach (ClothingLayer clothingLayer in (ClothingLayer[])Enum.GetValues(typeof(ClothingLayer)))
            {
                Dbgl($"{clothingLayer}: {SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.styleData[(byte)clothingLayer]}");
                ClothingLayerData style = SingletonBehaviour<CharacterClothingStyles>.Instance.GetStyle(clothingLayer, SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.styleData[(byte)clothingLayer]);
                if (style)
                {
                    currentClothingDictionary[clothingLayer] = style;
                    MainMenuController.Instance.characterCreation.SetClothingLayerData(style, clothingLayer, false);
                }
            }
            AccessTools.Method(typeof(CharacterCreation), "UpdateStartingItems").Invoke(MainMenuController.Instance.characterCreation, new object[0]);
        }
    }
}
