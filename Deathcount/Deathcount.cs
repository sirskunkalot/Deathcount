using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

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
        
        private void Awake()
        {
            EnabledConfig = Config.Bind(
                "Settings", "Enabled", true, "Deathcount is enabled and will show total deaths on screen upon entering a world");
            EnabledConfig.SettingChanged += (_, _) =>
            {
                if (EnabledConfig.Value)
                {

                }
                else
                {
                    DestroyStatisticsUI();
                }
            };

            ToggleUIConfig = Config.Bind(
                "Settings", "Death Statistics UI", KeyCode.F10, "Key to show/hide the death statistics UI");
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
            if (ZInput.instance == null)
            {
                return;
            }

            if (ZInput.GetButtonDown(ToggleUIButton.Name))
            {
                if (!StatisticsUI)
                {
                    CreateStatisticsUI();
                }
                else
                {
                    DestroyStatisticsUI();
                }
            }
        }

        private static void CreateStatisticsUI()
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
            
            if (Game.instance) {
                StatisticsUI = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0),
                    width: 450f,
                    height: 500f,
                    draggable: false);
                //StatisticsUI.AddComponent<DragWindowCntrl>();
                StatisticsUI.SetActive(false);

                GUIManager.Instance.CreateText(
                    text: "Death Statistics",
                    parent: StatisticsUI.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(45f, -45f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);
            
                var scrollView = GUIManager.Instance.CreateScrollView(
                    StatisticsUI.transform,
                    false, true, 10f, 5f,
                    GUIManager.Instance.ValheimScrollbarHandleColorBlock, Color.black,
                    300f, 350f);

                var viewport =
                    scrollView.transform.Find("Scroll View/Viewport/Content") as RectTransform;

                var profile = Game.instance.GetPlayerProfile();
                if (profile != null)
                {
                    GUIManager.Instance.CreateText($"Deaths: {profile.m_playerStats[PlayerStatType.Deaths]}",
                        viewport.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                        GUIManager.Instance.AveriaSerif, 20, GUIManager.Instance.ValheimOrange,
                        true, Color.black, 300f, 40f, false);

                    var deathValues = Enum.GetValues(typeof(PlayerStatType))
                        .Cast<PlayerStatType>()
                        .Where(e => e.ToString().StartsWith("DeathB") || e.ToString().StartsWith("Tombstone"))
                        .OrderBy(e => e.ToString());

                    foreach (var value in deathValues)
                    {
                        GUIManager.Instance.CreateText($"{value}: {profile.m_playerStats[value]}",
                            viewport.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                            GUIManager.Instance.AveriaSerif, 20, GUIManager.Instance.ValheimOrange,
                            true, Color.black, 300f, 40f, false);
                    }
                }
                
                StatisticsUI.SetActive(true);
                GUIManager.BlockInput(true);
            }
        }

        private static void DestroyStatisticsUI()
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

