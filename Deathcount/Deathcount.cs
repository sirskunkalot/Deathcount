using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Deathcount
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class Deathcount : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.Deathcount";
        public const string PluginName = "Deathcount";
        public const string PluginVersion = "0.0.2";

        private static ConfigEntry<bool> EnabledConfig;
        private static ConfigEntry<float> PosXConfig;
        private static ConfigEntry<float> PosYConfig;
        private static ConfigEntry<int> FontSizeConfig;
        private static ConfigEntry<Color> FontColorConfig;
        private static ConfigEntry<Color> FontOutlineColorConfig;
        private static ConfigEntry<KeyCode> ToggleStatisticsUIConfig;

        private static ButtonConfig ToggleStatisticsUIButton;
        private static GameObject DeathcountUI;
        private static GameObject StatisticsUI;

        private void Awake()
        {
            EnabledConfig = Config.Bind(
                "Deathcount", "Enabled", true,
                "Deathcount is enabled and will show total deaths on screen upon entering a world");
            EnabledConfig.SettingChanged += (_, _) =>
            {
                if (EnabledConfig.Value)
                {
                    if (!DeathcountUI)
                        CreateDeathcountUI();
                }
                else
                {
                    if (DeathcountUI)
                        DestroyDeathcountUI();
                    if (StatisticsUI)
                        DestroyStatisticsUI();
                }
            };

            PosXConfig = Config.Bind(
                "Deathcount", "X Position", 0f,
                "X Position of the Deathcount display. Screen position will be saved automatically on logout when the display was dragged.");
            PosYConfig = Config.Bind(
                "Deathcount", "Y Position", -45f,
                "Y Position of the Deathcount display. Screen position will be saved automatically on logout when the display was dragged.");

            FontSizeConfig = Config.Bind(
                "Deathcount", "Font size", 30,
                "Size of the Deathcount display font.");
            FontSizeConfig.SettingChanged += (_, _) =>
            {
                if (DeathcountUI)
                    DeathcountUI.GetComponent<Text>().fontSize = FontSizeConfig.Value;
            };

            FontColorConfig = Config.Bind(
                "Deathcount", "Font color", GUIManager.Instance.ValheimOrange,
                "Font color of the Deathcount display.");
            FontColorConfig.SettingChanged += (_, _) =>
            {
                if (DeathcountUI)
                    DeathcountUI.GetComponent<Text>().color = FontColorConfig.Value;
            };

            FontOutlineColorConfig = Config.Bind(
                "Deathcount", "Font outline color", Color.black,
                "Font outline color of the Deathcount display.");
            FontOutlineColorConfig.SettingChanged += (_, _) =>
            {
                if (DeathcountUI)
                    DeathcountUI.GetComponent<Outline>().effectColor = FontOutlineColorConfig.Value;
            };

            ToggleStatisticsUIConfig = Config.Bind(
                "Statistics", "Statistics UI Key", KeyCode.F10,
                "Key to show/hide the death statistics UI");
            ToggleStatisticsUIButton = new ButtonConfig
            {
                Name = "StatisticsUIKey",
                Config = ToggleStatisticsUIConfig,
                ActiveInCustomGUI = true
            };
            InputManager.Instance.AddButton(PluginGUID, ToggleStatisticsUIButton);

            GUIManager.OnCustomGUIAvailable += () =>
            {
                if (!EnabledConfig.Value)
                    return;
                CreateDeathcountUI();
            };

            Harmony.CreateAndPatchAll(typeof(Deathcount), PluginGUID);
        }

        private void Update()
        {
            if (ZInput.instance != null && ZInput.GetButtonDown(ToggleStatisticsUIButton.Name))
            {
                if (!StatisticsUI)
                    CreateStatisticsUI();
                else
                    DestroyStatisticsUI();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath)), HarmonyPostfix]
        private static void PostfixPlayerOnDeath(Player __instance)
        {
            if (__instance == Player.m_localPlayer && DeathcountUI)
                UpdateDeathcountUI();
        }

        [HarmonyPatch(typeof(Game), nameof(Game.Shutdown)), HarmonyPostfix]
        private static void PostfixGameShutdown(Game __instance)
        {
            if (DeathcountUI)
                DestroyDeathcountUI();
            if (StatisticsUI)
                DestroyStatisticsUI();
        }

        private static void CreateDeathcountUI()
        {
            if (GUIManager.Instance == null || !GUIManager.CustomGUIBack || DeathcountUI || !Game.instance)
                return;
            var profile = Game.instance.GetPlayerProfile();
            if (profile == null)
                return;

            DeathcountUI = GUIManager.Instance.CreateText(
                $"Deaths: {profile.m_playerStats[PlayerStatType.Deaths]}",
                GUIManager.CustomGUIBack.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(PosXConfig.Value, PosYConfig.Value),
                GUIManager.Instance.NorseBold, FontSizeConfig.Value, FontColorConfig.Value,
                true, FontOutlineColorConfig.Value, 100f, 50f, false);

            var fitter = DeathcountUI.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var text = DeathcountUI.GetComponent<Text>();
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            DeathcountUI.AddComponent<Jotunn.GUI.DragWindowCntrl>();
            DeathcountUI.SetActive(true);
        }
        
        private static void UpdateDeathcountUI()
        {
            if (!DeathcountUI || !Game.instance)
                return;
            var profile = Game.instance.GetPlayerProfile();
            if (profile == null)
                return;

            DeathcountUI.GetComponent<Text>().text =
                $"Deaths: {profile.m_playerStats[PlayerStatType.Deaths]}";
        }

        private static void DestroyDeathcountUI()
        {
            if (!DeathcountUI)
                return;

            PosXConfig.Value = ((RectTransform)DeathcountUI.transform).anchoredPosition.x;
            PosYConfig.Value = ((RectTransform)DeathcountUI.transform).anchoredPosition.y;

            DeathcountUI.SetActive(false);
            Destroy(DeathcountUI);
            DeathcountUI = null;
        }

        private static void CreateStatisticsUI()
        {
            if (GUIManager.Instance == null || !GUIManager.CustomGUIFront || StatisticsUI || !Game.instance)
                return;
            var profile = Game.instance.GetPlayerProfile();
            if (profile == null)
                return;

            StatisticsUI = GUIManager.Instance.CreateWoodpanel(
                GUIManager.CustomGUIFront.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), 450f, 500f, false);
            StatisticsUI.SetActive(false);

            GUIManager.Instance.CreateText(
                "Death Statistics", StatisticsUI.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(45f, -45f),
                GUIManager.Instance.AveriaSerifBold, 30, GUIManager.Instance.ValheimOrange,
                true, Color.black, 300f, 40f, false);

            var scrollView = GUIManager.Instance.CreateScrollView(
                StatisticsUI.transform, false, true, 10f, 5f,
                GUIManager.Instance.ValheimScrollbarHandleColorBlock, Color.black, 300f, 350f);
            var content = scrollView.transform.Find("Scroll View/Viewport/Content");

            var stats = Enum.GetValues(typeof(PlayerStatType))
                .Cast<PlayerStatType>()
                .Where(e => e == PlayerStatType.Deaths
                            || e.ToString().StartsWith("DeathB")
                            || e.ToString().StartsWith("Tombstone"))
                .OrderBy(e => e.ToString());

            foreach (var stat in stats)
            {
                GUIManager.Instance.CreateText(
                    $"{stat}: {profile.m_playerStats[stat]}", content,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                    GUIManager.Instance.AveriaSerif, 20, GUIManager.Instance.ValheimOrange,
                    true, Color.black, 300f, 40f, false);
            }

            StatisticsUI.SetActive(true);
            GUIManager.BlockInput(true);
        }

        private static void DestroyStatisticsUI()
        {
            if (!StatisticsUI)
                return;

            GUIManager.BlockInput(false);
            StatisticsUI.SetActive(false);
            Destroy(StatisticsUI);
            StatisticsUI = null;
        }
    }
}