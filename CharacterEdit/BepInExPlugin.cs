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
using Wish;

namespace CharacterEdit
{
    [BepInPlugin("aedenthorn.CharacterEdit", "Character Edit", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> hotKey;

        public static CharacterCreation characterCreation = null;
        private static string lastName;

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

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(LoadCharacterMenu), "SetupSavePanels")]
        static class LoadCharacterMenu_SetupSavePanels_Patch
        {
            static void Postfix(Transform ____characterSelectPanel)
            {
                MainMenuController.Instance.StartCoroutine(FixButtons(____characterSelectPanel));
            }
        }

        private static IEnumerator FixButtons(Transform characterSelectPanel)
        {
            Dbgl($"fixing buttons");
            yield return new WaitForEndOfFrame();
            Dbgl($"{characterSelectPanel.childCount}");

            for (int i = 0; i < characterSelectPanel.childCount; i++)
            {
                Dbgl($"{i}, {(i + 1)} of {characterSelectPanel.childCount}");
                int index = i;
                SavePanel savePanel = characterSelectPanel.GetChild(index).GetComponent<SavePanel>();
                GameObject button = Instantiate(savePanel.deleteButton.gameObject, savePanel.deleteButton.transform.parent);
                button.GetComponent<RectTransform>().anchoredPosition -= new Vector2(savePanel.deleteButton.gameObject.GetComponent<RectTransform>().rect.width, 0);
                button.transform.GetComponentInChildren<TextMeshProUGUI>().text = "Edit";
                button.GetComponent<UnityEngine.UI.Button>().onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate ()
                {
                    Dbgl($"clicked edit {index}");

                    SingletonBehaviour<GameSave>.Instance.LoadCharacter(index);
                    MainMenuController.Instance.EnableMenu(MainMenuController.Instance.newCharacterMenu);
                    SetupCharacter();
                    MainMenuController.Instance.backCharacterButton.onClick.AddListener(SetupButtons);
                    MainMenuController.Instance.confirmCharacterButton.onClick.RemoveAllListeners();
                    MainMenuController.Instance.confirmCharacterButton.onClick.AddListener(delegate ()
                    {
                        Dictionary<ClothingLayer, ClothingLayerData> currentClothingDictionary = (Dictionary<ClothingLayer, ClothingLayerData>)AccessTools.Field(typeof(CharacterCreation), "currentClothingDictionary").GetValue(MainMenuController.Instance.characterCreation);
                        foreach (ClothingLayerData clothingLayerData in currentClothingDictionary.Values.ToList())
                        {
                            if (clothingLayerData.armorData != null)
                            {
                                int vanityIndexByArmorType = PlayerInventory.GetVanityIndexByArmorType(clothingLayerData.armorData.armorType);
                                MainMenuController.Instance.characterCreation.CurrentCharacter.Items[(short)vanityIndexByArmorType] = new InventoryItemData
                                {
                                    Amount = 1,
                                    Item = clothingLayerData.armorData.GenerateArmorItem()
                                };
                                Debug.Log("Index" + vanityIndexByArmorType);
                                ClothingLayer clothingLayer = clothingLayerData.ClotherLayerInfo[0].clothingLayer;
                                MainMenuController.Instance.characterCreation.SetClothingLayerData(MainMenuController.Instance.characterCreation.defaultLayers[clothingLayer], clothingLayer, false);
                            }
                        }
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
            }

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

            MainMenuController.Instance.characterCreation.SetRaceImages();
            
            if(!MainMenuController.Instance.characterCreation.CurrentCharacter.male)
                MainMenuController.Instance.characterCreation.SetFemale();
            else
                MainMenuController.Instance.characterCreation.SetMale();

            Dictionary<ClothingLayer, ClothingLayerData> currentClothingDictionary = (Dictionary<ClothingLayer, ClothingLayerData>)AccessTools.Field(typeof(CharacterCreation), "currentClothingDictionary").GetValue(MainMenuController.Instance.characterCreation);

            foreach (ClothingLayer clothingLayer in (ClothingLayer[])Enum.GetValues(typeof(ClothingLayer)))
            {
                ClothingLayerData style = SingletonBehaviour<CharacterClothingStyles>.Instance.GetStyle(clothingLayer, MainMenuController.Instance.characterCreation.CurrentCharacter.styleData[(byte)clothingLayer]);
                if (style)
                {
                    Dbgl($"Setting style for layer {clothingLayer} {style.menuName}");
                    currentClothingDictionary[clothingLayer] = style;
                    MainMenuController.Instance.characterCreation.SetClothingLayerData(style, clothingLayer, true);
                    MainMenuController.Instance.characterCreation.SetClothingColors(style, clothingLayer);
                }
            }

            MainMenuController.Instance.mainMenuPlayerController.InitializeAsMainMenuPlayer(MainMenuController.Instance.characterCreation.CurrentCharacter);
        }


        /*
        public void SetupButtons()
        {
            this.confirmCharacterButton.onClick.RemoveAllListeners();
            if (!GameManager.Multiplayer)
            {
                this.confirmCharacterButton.onClick.AddListener(delegate ()
                {
                    this.characterCreation.AddNewCharacter();
                    this.PlayGame(SingletonBehaviour<GameSave>.Instance.Saves.Count - 1);
                });
                this.backCharacterButton.gameObject.SetActive(true);
                return;
            }
            if (GameManager.Host)
            {
                this.confirmCharacterButton.onClick.AddListener(delegate ()
                {
                    this.characterCreation.AddNewCharacter();
                    if (!NetworkManager.Instance)
                    {
                        if (this.platform == MultiplayerPlatform.Steam)
                        {
                            this.steamMenu.HostServer();
                            return;
                        }
                        this.nativeMenu.HostServer();
                    }
                });
                return;
            }
            this.confirmCharacterButton.onClick.AddListener(delegate ()
            {
                this.characterCreation.AddNewCharacter();
                SingletonBehaviour<GameSave>.Instance.LoadCharacter(SingletonBehaviour<GameSave>.Instance.Saves.Count - 1);
                this.LoadConnectMenu();
            });
        }
        */
    }
}
