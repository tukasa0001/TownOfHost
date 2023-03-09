using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using VentLib.Logging;
using Object = UnityEngine.Object;

namespace TOHTOR.GUI.Menus.CustomNameMenu;

public class CustomNameMenu
{
    private static GameObject NamePositionMenu;
    public static DynamicName Name;
    private static CustomTextButton openMenuButton;
    private static GameOptionsMenu gsm;
    private static CustomNameMenuPane pane;
    private static DynamicName preview;
    private static UI activeComponent;
    private static CustomTextButton playerName;
    private static CustomTextButton roleName;
    private static CustomTextButton subrole;
    private static CustomTextButton abilityCounter;
    private static CustomTextButton abilityCooldown;
    private static CustomTextButton misc;
    private static CustomTextButton defButton;
    private static CustomTextButton[] selectionButtons;

    [HarmonyPatch(typeof(GameOptionsMenu), "Start")]
    [HarmonyPriority(Priority.Last)]
    public class CustomNameMenuRegister
    {
        public static void Postfix()
        {
            var template = Object.Instantiate(Object.FindObjectsOfType<StringOption>().FirstOrDefault());
            if (template == null) return;

            var gameSettings = GameObject.Find("Game Settings");
            if (gameSettings == null) return;


            Name = new DynamicName();
            Name.Deserialize();
            gsm = gameSettings.transform
                .FindChild("GameGroup")
                .FindChild("SliderInner")
                .GetComponent<GameOptionsMenu>();
            gsm.GetComponentInParent<Scroller>().ContentYBounds.max = 6.2f;


            CustomTextButton openMenuButton = CustomTextButton.Create(gsm.transform);
            openMenuButton.Transform.localPosition = template.transform.localPosition + new Vector3(0, -2.5f);

            openMenuButton.SetHoverBackgroundColor(Color.blue);
            openMenuButton.SetHoverTextColor(Color.yellow);
            openMenuButton.Text.text = "Modify Name Position";
            openMenuButton.AddOnClickHandle(ShowNameScreen);

            if (NamePositionMenu != null) return;

            NamePositionMenu = new();
            NamePositionMenu.transform.position = gameSettings.transform.position + new Vector3(1.75f, -3f);
            AddButtons();
            pane = new(NamePositionMenu.transform);
            pane.SetName(Name.PreviewName());

            NamePositionMenu.SetActive(false);
            ControllerManager.Instance.ClearDestroyedSelectableUiElements();
        }


        private static void AddButtons()
        {
            const float iconVerticalFloat = -1.425f;
            const float iconHorizontalFloat = 1.35f;
            const float buttonVerticalFloat = 1.5f;
            const float buttonHorizontalFloat = -1.3f;

            playerName = CustomTextButton.Create(NamePositionMenu.transform);
            playerName.Text.text = "Player Name";
            playerName.Button.transform.localPosition += new Vector3(buttonHorizontalFloat, 0.5f + buttonVerticalFloat);
            Vector2 fixedSize = new Vector2(playerName.Background.size.x - 2.3f, playerName.Background.size.y);
            playerName.Background.size = fixedSize;

            roleName = CustomTextButton.Create(NamePositionMenu.transform);
            roleName.Text.text = "Role Name";
            roleName.Button.transform.localPosition += new Vector3(buttonHorizontalFloat, buttonVerticalFloat);
            roleName.Background.size = fixedSize;

            subrole = CustomTextButton.Create(NamePositionMenu.transform);
            subrole.Text.text = "Subrole";
            subrole.Button.transform.localPosition += new Vector3(buttonHorizontalFloat, - 0.5f + buttonVerticalFloat);
            subrole.Background.size = fixedSize;

            abilityCounter = CustomTextButton.Create(NamePositionMenu.transform);
            abilityCounter.Text.text = "Ability Counter";
            abilityCounter.Button.transform.localPosition +=
                new Vector3(buttonHorizontalFloat, -1f + buttonVerticalFloat);
            abilityCounter.Background.size = fixedSize;

            abilityCooldown = CustomTextButton.Create(NamePositionMenu.transform);
            abilityCooldown.Text.text = "Ability Cooldown";
            abilityCooldown.Transform.localPosition += new Vector3(buttonHorizontalFloat, -1.5f + buttonVerticalFloat);
            abilityCooldown.Background.size = fixedSize;

            misc = CustomTextButton.Create(NamePositionMenu.transform);
            misc.Text.text = "Miscellaneous";
            misc.Transform.localPosition += new Vector3(buttonHorizontalFloat, -2f + buttonVerticalFloat);
            misc.Background.size = fixedSize;

            defButton = CustomTextButton.Create(NamePositionMenu.transform);
            defButton.Text.text = "Revert to Default";
            defButton.Transform.localPosition += new Vector3(buttonHorizontalFloat, -2.5f + buttonVerticalFloat);
            defButton.Background.size = fixedSize;
            defButton.SetScale(new Vector3(0.95f, 1, 1));
            defButton.SetHoverBackgroundColor(Color.blue);
            defButton.SetHoverTextColor(new Color(0.92f, 0.8f, 0.2f));


            CustomTextButton left = CustomTextButton.Create(NamePositionMenu.transform);
            left.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.ArrowLeft.png", 100f);
            left.Background.size = new Vector2(0.5f, 0.5f);
            left.Text.text = "";
            left.Button.transform.localPosition += new Vector3(-0.5f + iconHorizontalFloat, 0 + iconVerticalFloat);
            left.PassiveButton.transform.localPosition = left.Button.transform.localPosition;
            left.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            left.Background.transform.localScale = new Vector3(4f, 1);

            CustomTextButton right = CustomTextButton.Create(NamePositionMenu.transform);
            right.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.ArrowRight.png", 100f);
            right.Background.size = new Vector2(0.5f, 0.5f);
            right.Text.text = "";
            right.Button.transform.localPosition += new Vector3(iconHorizontalFloat, iconVerticalFloat);
            right.PassiveButton.transform.localPosition = right.Button.transform.localPosition;
            right.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            right.Background.transform.localScale = new Vector3(4f, 1);

            CustomTextButton visibility = CustomTextButton.Create(NamePositionMenu.transform);
            visibility.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.Visibility.png", 100f);
            visibility.Background.size = new Vector2(0.5f, 0.5f);
            visibility.Text.text = "";
            visibility.Button.transform.localPosition += new Vector3(1f + iconHorizontalFloat, 0 + iconVerticalFloat);
            visibility.PassiveButton.transform.localPosition = visibility.Button.transform.localPosition;
            visibility.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            visibility.Background.transform.localScale = new Vector3(4f, 1);

            CustomTextButton up = CustomTextButton.Create(NamePositionMenu.transform);
            up.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.ArrowUp.png", 100f);
            up.Background.size = new Vector2(0.5f, 0.5f);
            up.Text.text = "";
            up.Button.transform.localPosition += new Vector3(0.5f + iconHorizontalFloat, 0.5f + iconVerticalFloat);
            up.PassiveButton.transform.localPosition = up.Button.transform.localPosition;
            up.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            up.Background.transform.localScale = new Vector3(4f, 1);

            CustomTextButton down = CustomTextButton.Create(NamePositionMenu.transform);
            down.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.ArrowDown.png", 100f);
            down.Background.size = new Vector2(0.5f, 0.5f);
            down.Text.text = "";
            down.Button.transform.localPosition += new Vector3(0.5f + iconHorizontalFloat, 0 + iconVerticalFloat);
            down.PassiveButton.transform.localPosition = down.Button.transform.localPosition;
            down.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            down.Background.transform.localScale = new Vector3(4f, 1);

            CustomTextButton zoomIn = CustomTextButton.Create(NamePositionMenu.transform);
            zoomIn.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.ZoomIn.png", 100f);
            zoomIn.Background.size = new Vector2(0.5f, 0.5f);
            zoomIn.Text.text = "";
            zoomIn.Button.transform.localPosition += new Vector3(0 + iconHorizontalFloat, 0.5f + iconVerticalFloat);
            zoomIn.PassiveButton.transform.localPosition = zoomIn.Button.transform.localPosition;
            zoomIn.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            zoomIn.Background.transform.localScale = new Vector3(4f, 1);

            CustomTextButton zoomOut = CustomTextButton.Create(NamePositionMenu.transform);
            zoomOut.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.ZoomOut.png", 100f);
            zoomOut.Background.size = new Vector2(0.5f, 0.5f);
            zoomOut.Text.text = "";
            zoomOut.Button.transform.localPosition += new Vector3(-0.5f + iconHorizontalFloat, 0.5f + iconVerticalFloat);
            zoomOut.PassiveButton.transform.localPosition = zoomOut.Button.transform.localPosition;
            zoomOut.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            zoomOut.Background.transform.localScale = new Vector3(4f, 1);

            CustomTextButton addSpace = CustomTextButton.Create(NamePositionMenu.transform);
            addSpace.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.AddSpace.png", 100f);
            addSpace.Background.size = new Vector2(0.5f, 0.5f);
            addSpace.Text.text = "";
            addSpace.Button.transform.localPosition += new Vector3(-1f + iconHorizontalFloat, 0.5f + iconVerticalFloat);
            addSpace.PassiveButton.transform.localPosition = addSpace.Button.transform.localPosition;
            addSpace.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            addSpace.Background.transform.localScale = new Vector3(4f, 1);

            CustomTextButton removeSpace = CustomTextButton.Create(NamePositionMenu.transform);
            removeSpace.Background.sprite = Utils.LoadSprite("TOHTOR.assets.NameMenu.RemoveSpace.png", 100f);
            removeSpace.Background.size = new Vector2(0.5f, 0.5f);
            removeSpace.Text.text = "";
            removeSpace.Button.transform.localPosition += new Vector3(-1f + iconHorizontalFloat, iconVerticalFloat);
            removeSpace.PassiveButton.transform.localPosition = removeSpace.Button.transform.localPosition;
            removeSpace.PassiveButton.transform.localScale = new Vector3(0.25f, 1);
            removeSpace.Background.transform.localScale = new Vector3(4f, 1);


            selectionButtons = new[] { playerName, roleName, subrole, abilityCounter, abilityCooldown, misc };
            selectionButtons.Do(b =>
            {
                b.SetHoverBackgroundColor(Color.blue);
                b.SetHoverTextColor(new Color(0.92f, 0.8f, 0.2f));
                b.SetToggleTextColor(new Color(0.92f, 0.8f, 0.2f));
                b.SetToggleBackgroundColor(Color.blue);
                b.AllowToggle(true);
            });

            playerName.AddMouseEnterHandle(() => HoverComponent(UI.Name));
            roleName.AddMouseEnterHandle(() => HoverComponent(UI.Role));
            subrole.AddMouseEnterHandle(() => HoverComponent(UI.Subrole));
            abilityCounter.AddMouseEnterHandle(() => HoverComponent(UI.Counter));
            abilityCooldown.AddMouseEnterHandle(() => HoverComponent(UI.Cooldown));
            misc.AddMouseEnterHandle(() => HoverComponent(UI.Misc));

            playerName.AddMouseExitHandle(() => UnhoverComponent(UI.Name));
            roleName.AddMouseExitHandle(() => UnhoverComponent(UI.Role));
            subrole.AddMouseExitHandle(() => UnhoverComponent(UI.Subrole));
            abilityCounter.AddMouseExitHandle(() => UnhoverComponent(UI.Counter));
            abilityCooldown.AddMouseExitHandle(() => UnhoverComponent(UI.Cooldown));
            misc.AddMouseExitHandle(() => UnhoverComponent(UI.Misc));

            playerName.AddOnClickHandle(() => SetActiveComponent(playerName, UI.Name));
            roleName.AddOnClickHandle(() => SetActiveComponent(roleName, UI.Role));
            subrole.AddOnClickHandle(() => SetActiveComponent(subrole, UI.Subrole));
            abilityCounter.AddOnClickHandle(() => SetActiveComponent(abilityCounter, UI.Counter));
            abilityCooldown.AddOnClickHandle(() => SetActiveComponent(abilityCooldown, UI.Cooldown));
            misc.AddOnClickHandle(() => SetActiveComponent(misc, UI.Misc));
            defButton.AddOnClickHandle(ResetComponents);

            left.AddOnClickHandle(() => ShiftComponent(ShiftDirection.Left));
            right.AddOnClickHandle(() => ShiftComponent(ShiftDirection.Right));
            up.AddOnClickHandle(() => ShiftComponent(ShiftDirection.Up));
            down.AddOnClickHandle(() => ShiftComponent(ShiftDirection.Down));
            zoomIn.AddOnClickHandle(GrowComponent);
            zoomOut.AddOnClickHandle(ShrinkComponent);
            visibility.AddOnClickHandle(ToggleVisibility);
            addSpace.AddOnClickHandle(AddSpace);
            removeSpace.AddOnClickHandle(RemoveSpace);
        }

        private static void SetActiveComponent(CustomTextButton button, UI component)
        {
            activeComponent = component;
            selectionButtons.Where(b => b != button).Do(b => b.SetState(false));
        }

        private static void ToggleVisibility()
        {
            Name.ToggleVisibility(activeComponent);
            UpdateName();
        }

        private static void AddSpace()
        {
            Name.AddSpace(activeComponent);
            UpdateName();
        }

        private static void RemoveSpace()
        {
            Name.RemoveSpace(activeComponent);
            UpdateName();
        }

        private static void GrowComponent()
        {
            Name.IncrementSize(activeComponent);
            UpdateName();
        }

        private static void ShrinkComponent()
        {
            Name.DecrementSize(activeComponent);
            UpdateName();
        }

        private static void ShiftComponent(ShiftDirection direction)
        {
            Name.ShiftComponent(activeComponent, direction);
            UpdateName();
        }

        private static void ResetComponents()
        {
            Name.ResetToDefault();
            UpdateName();
        }

        private static void HoverComponent(UI component)
        {
            Name.HighlightComponent(component);
            UpdateName(false);
        }
        private static void UnhoverComponent(UI component)
        {
            Name.UnhighlightComponent(component);
            UpdateName(false);
        }
        private static void UpdateName(bool serialize = true) => pane.SetName(Name.PreviewName(serialize));

        private static void ShowNameScreen()
        {
            gsm.gameObject.SetActive(false);
            NamePositionMenu.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Close))]
    public class CustomNameMenuClose
    {
        public static void Prefix() => CloseNameScreen();
    }

    public static void CloseNameScreen(bool switchTab = false)
    {
        try
        {
            gsm.gameObject.SetActive(switchTab);
            NamePositionMenu.SetActive(false);
        }
        catch (Exception e)
        {
            VentLogger.Fatal("Please Evan for the love of god fix this", "CloseNameMenu");
        }
    }

}