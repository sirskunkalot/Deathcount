using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Profiling;
using static UnityEngine.UI.GridLayoutGroup;

namespace Deathcount
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class Deathcount : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.Deathcount";
        public const string PluginName = "Deathcount";
        public const string PluginVersion = "0.0.1";

        private static ConfigEntry<bool> EnabledConfig;
        private static ConfigEntry<KeyCode> ToggleUIConfig;
        private static ButtonConfig ToggleUIButton;

        private static GameObject StatisticsUI;
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            EnabledConfig = Config.Bind(
                "Settings", "Enabled", true, "Deathcount is enabled and will show total deaths on screen upon entering a world");

            ToggleUIConfig = Config.Bind(
                "Settings", "Detailed Death Statistics UI", KeyCode.F10, "Key to show/hide the death statistics UI");
            
            ToggleUIButton = new ButtonConfig
            {
                Name = "DeathcountToggle",
                Config = ToggleUIConfig,
                ActiveInCustomGUI = true
            };
            InputManager.Instance.AddButton(PluginGUID, ToggleUIButton);

        }

        private void Update()
        {
            if (!EnabledConfig.Value) {
                return;
            }
            
            // No keys without ZInput
            if (ZInput.instance == null)
            {
                return;
            }

            // Toggle UI
            if (EnabledConfig.Value && ZInput.GetButtonDown(ToggleUIButton.Name))
            {
                if (!StatisticsUI)
                {
                    CreateUI();
                }
                else
                {
                    DestroyUI();
                }
            }
        }

        private static void CreateUI()
        {
            if (GUIManager.Instance == null)
            {
                Jotunn.Logger.LogError("GUIManager instance is null");
                return;
            }

            if (!GUIManager.CustomGUIFront)
            {
                Jotunn.Logger.LogError("GUIManager CustomGUI is null");
                return;
            }

            if (StatisticsUI)
            {
                Jotunn.Logger.LogError("StatisticsUI is not null");
                return;
            }

            // Create the panel object
            StatisticsUI = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 850,
                height: 600,
                draggable: false);
            StatisticsUI.SetActive(false);

            // Create the text objects
            GUIManager.Instance.CreateText(
                text: "Death Statistics",
                parent: StatisticsUI.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -50f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);
            
            var profile = Game.instance.GetPlayerProfile();
            if (profile != null)
            {
                GUIManager.Instance.CreateText($"Overall deaths: {profile.m_playerStats[PlayerStatType.Deaths]}",
                    StatisticsUI.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                    GUIManager.Instance.AveriaSerifBold, 30, GUIManager.Instance.ValheimOrange,
                    true, Color.black, 650f, 40f, false);
            }
            
            // Set the active state of the panel
            StatisticsUI.SetActive(true);

            // Toggle input for the player and camera while displaying the GUI
            GUIManager.BlockInput(true);
        }

        private static void DestroyUI()
        {
            if (!StatisticsUI)
            {
                Jotunn.Logger.LogError("StatisticsUI is null");
                return;
            }
            
            GUIManager.BlockInput(false);
            StatisticsUI.SetActive(false);
            Destroy(StatisticsUI);
        }
    }
}

