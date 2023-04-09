using System;
using BepInEx.Configuration;
using UnityEngine;

namespace TownOfHost
{
    public sealed class ClientOptionItem : ClientActionItem
    {
        public ConfigEntry<bool> Config { get; private set; }

        private ClientOptionItem(
            string name,
            ConfigEntry<bool> config,
            OptionsMenuBehaviour optionsMenuBehaviour)
        : base(
            name,
            optionsMenuBehaviour)
        {
            Config = config;
            UpdateToggle();
        }

        public static ClientOptionItem Create(
            string name,
            ConfigEntry<bool> config,
            OptionsMenuBehaviour optionsMenuBehaviour,
            Action additionalOnClickAction = null)
        {
            var item = new ClientOptionItem(name, config, optionsMenuBehaviour);
            item.OnClickAction = () =>
            {
                config.Value = !config.Value;
                item.UpdateToggle();
                additionalOnClickAction?.Invoke();
            };
            return item;
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
