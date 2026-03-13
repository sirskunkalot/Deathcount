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

            StatisticsUI = new GameObject("StatisticsUI", typeof(RectTransform));
            StatisticsUI.transform.SetParent(GUIManager.CustomGUIFront.transform, false);

            var rt = (RectTransform)StatisticsUI.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(30f, 0f);

            var bg = StatisticsUI.AddComponent<Image>();
            bg.type = Image.Type.Sliced;
            bg.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
            bg.color = Color.white;
            bg.sprite = GUIManager.Instance.GetSprite("woodpanel_trophys");

            var layout = StatisticsUI.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 4f;
            layout.padding = new RectOffset(25, 25, 25, 25);

            var fitter = StatisticsUI.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddStatText("Death Statistics", 30, GUIManager.Instance.NorseBold, TextAnchor.MiddleCenter, GUIManager.Instance.ValheimOrange);
            AddSpacer(10f);

            AddStatText($"Deaths: {profile.m_playerStats[PlayerStatType.Deaths]}", 18,
                GUIManager.Instance.AveriaSerif, TextAnchor.MiddleLeft, Color.white);

            var stats = Enum.GetValues(typeof(PlayerStatType))
                .Cast<PlayerStatType>()
                .Where(e => e.ToString().StartsWith("DeathB") || e.ToString().StartsWith("Tombstone"))
                .OrderBy(e => e.ToString());

            foreach (var stat in stats)
                AddStatText($"{stat}: {profile.m_playerStats[stat]}", 18,
                    GUIManager.Instance.AveriaSerif, TextAnchor.MiddleLeft, Color.white);

            StatisticsUI.SetActive(true);
            return;

            void AddStatText(string text, int fontSize, Font font, TextAnchor alignment, Color color)
            {
                var go = GUIManager.Instance.CreateText(
                    text, StatisticsUI.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f),
                    font, fontSize, color,
                    true, Color.black, 300f, 30f, false);
                go.GetComponent<Text>().alignment = alignment;
            }

            void AddSpacer(float height)
            {
                var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
                spacer.transform.SetParent(StatisticsUI.transform, false);
                spacer.GetComponent<LayoutElement>().preferredHeight = height;
            }
        }

        private static void DestroyStatisticsUI()
        {
            if (!StatisticsUI)
                return;

            StatisticsUI.SetActive(false);
            Destroy(StatisticsUI);
            StatisticsUI = null;
        }
    }
}