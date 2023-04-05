using System;
using BepInEx.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TownOfHost
{
    public class ClientOptionItem
    {
        public ConfigEntry<bool> Config;
        public ToggleButtonBehaviour ToggleButton;

        public static SpriteRenderer CustomBackground;
        private static int numOptions = 0;

        private ClientOptionItem(
            string label,
            string objectName,
            ConfigEntry<bool> config,
            OptionsMenuBehaviour optionsMenuBehaviour,
            Action additionalOnClickAction = null)
        {
            try
            {
                Config = config;

                var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

                // 1つ目のボタンの生成時に背景も生成
                if (CustomBackground == null)
                {
                    numOptions = 0;
                    CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
                    CustomBackground.name = "CustomBackground";
                    CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
                    CustomBackground.transform.localPosition += Vector3.back * 8;
                    CustomBackground.gameObject.SetActive(false);

                    var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                    closeButton.transform.localPosition = new(1.3f, -2.3f, -6f);
                    closeButton.name = "Close";
                    closeButton.Text.text = Translator.GetString("Close");
                    closeButton.Background.color = Palette.DisabledGrey;
                    var closePassiveButton = closeButton.GetComponent<PassiveButton>();
                    closePassiveButton.OnClick = new();
                    closePassiveButton.OnClick.AddListener(new Action(() =>
                    {
                        CustomBackground.gameObject.SetActive(false);
                    }));

                    UiElement[] selectableButtons = optionsMenuBehaviour.ControllerSelectable.ToArray();
                    PassiveButton leaveButton = null;
                    PassiveButton returnButton = null;
                    for (int i = 0; i < selectableButtons.Length; i++)
                    {
                        var button = selectableButtons[i];
                        if (button == null)
                        {
                            continue;
                        }

                        if (button.name == "LeaveGameButton")
                        {
                            leaveButton = button.GetComponent<PassiveButton>();
                        }
                        else if (button.name == "ReturnToGameButton")
                        {
                            returnButton = button.GetComponent<PassiveButton>();
                        }
                    }
                    var generalTab = mouseMoveToggle.transform.parent.parent.parent;

                    var modOptionsButton = Object.Instantiate(mouseMoveToggle, generalTab);
                    modOptionsButton.transform.localPosition = leaveButton?.transform?.localPosition ?? new(0f, -2.4f, 1f);
                    modOptionsButton.name = "TOHOptions";
                    modOptionsButton.Text.text = Translator.GetString("TOHOptions");
                    modOptionsButton.Background.color = new Color32(0x00, 0xbf, 0xff, 0xff);
                    var modOptionsPassiveButton = modOptionsButton.GetComponent<PassiveButton>();
                    modOptionsPassiveButton.OnClick = new();
                    modOptionsPassiveButton.OnClick.AddListener(new Action(() =>
                    {
                        CustomBackground.gameObject.SetActive(true);
                    }));

                    if (leaveButton != null)
                    {
                        leaveButton.transform.localPosition = new(-1.35f, -2.411f, -1f);
                    }
                    if (returnButton != null)
                    {
                        returnButton.transform.localPosition = new(1.35f, -2.411f, -1f);
                    }
                }

                // ボタン生成
                ToggleButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                ToggleButton.transform.localPosition = new Vector3(
                    // 現在のオプション数を基に位置を計算
                    numOptions % 2 == 0 ? -1.3f : 1.3f,
                    2.2f - (0.5f * (numOptions / 2)),
                    -6f);
                ToggleButton.name = objectName;
                ToggleButton.Text.text = label;
                var passiveButton = ToggleButton.GetComponent<PassiveButton>();
                passiveButton.OnClick = new();
                passiveButton.OnClick.AddListener(new Action(() =>
                {
                    config.Value = !config.Value;
                    UpdateToggle();
                    additionalOnClickAction?.Invoke();
                }));
                UpdateToggle();
            }
            finally
            {
                numOptions++;
            }
        }

        public static ClientOptionItem Create(
            string label,
            string objectName,
            ConfigEntry<bool> config,
            OptionsMenuBehaviour optionsMenuBehaviour,
            Action additionalOnClickAction = null)
        {
            return new(label, objectName, config, optionsMenuBehaviour, additionalOnClickAction);
        }

        public void UpdateToggle()
        {
            if (ToggleButton == null)
            {
                return;
            }

            var color = Config.Value ? Color.green : Color.red;
            ToggleButton.Background.color = color;
            if (ToggleButton.Rollover != null)
            {
                ToggleButton.Rollover.ChangeOutColor(color);
            }
        }
    }
}
