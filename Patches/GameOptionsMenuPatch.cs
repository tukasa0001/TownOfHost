using System;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Modules.OptionItems;
using TownOfHost.Modules.OptionItems.Interfaces;
using UnityEngine;
using static TownOfHost.Translator;
using Object = UnityEngine.Object;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameSettingMenu))]
    public static class GameSettingMenuPatch
    {
        private static GameOptionsMenu tohSettingsTab;
        private static PassiveButton tohSettingsButton;
        public static CategoryHeaderMasked MainCategoryHeader { get; private set; }
        public static CategoryHeaderMasked ImpostorRoleCategoryHeader { get; private set; }
        public static CategoryHeaderMasked CrewmateRoleCategoryHeader { get; private set; }
        public static CategoryHeaderMasked NeutralRoleCategoryHeader { get; private set; }
        public static CategoryHeaderMasked AddOnCategoryHeader { get; private set; }

        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPostfix]
        public static void StartPostfix(GameSettingMenu __instance)
        {
            tohSettingsTab = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
            tohSettingsTab.name = TOHMenuName;
            var vanillaOptions = tohSettingsTab.GetComponentsInChildren<OptionBehaviour>();
            foreach (var vanillaOption in vanillaOptions)
            {
                Object.Destroy(vanillaOption.gameObject);
            }

            // TOH設定ボタンのスペースを作るため，左側の要素を上に詰める
            var gameSettingsLabel = __instance.transform.Find("GameSettingsLabel");
            if (gameSettingsLabel)
            {
                gameSettingsLabel.localPosition += Vector3.up * 0.2f;
            }
            __instance.MenuDescriptionText.transform.parent.localPosition += Vector3.up * 0.4f;
            __instance.GamePresetsButton.transform.parent.localPosition += Vector3.up * 0.5f;

            // TOH設定ボタン
            tohSettingsButton = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
            tohSettingsButton.name = "TOHSettingsButton";
            tohSettingsButton.transform.localPosition = __instance.RoleSettingsButton.transform.localPosition + (__instance.RoleSettingsButton.transform.localPosition - __instance.GameSettingsButton.transform.localPosition);
            tohSettingsButton.buttonText.DestroyTranslator();
            tohSettingsButton.buttonText.text = GetString("TOHSettingsButtonLabel");
            var activeSprite = tohSettingsButton.activeSprites.GetComponent<SpriteRenderer>();
            var selectedSprite = tohSettingsButton.selectedSprites.GetComponent<SpriteRenderer>();
            activeSprite.color = selectedSprite.color = Main.UnityModColor;
            tohSettingsButton.OnClick.AddListener((Action)(() =>
            {
                __instance.ChangeTab(-1, false);  // バニラタブを閉じる
                tohSettingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.text = GetString("TOHSettingsDescription");
                tohSettingsButton.SelectButton(true);
            }));

            // 各カテゴリの見出しを作成
            MainCategoryHeader = CreateCategoryHeader(__instance, tohSettingsTab, "TabGroup.MainSettings");
            ImpostorRoleCategoryHeader = CreateCategoryHeader(__instance, tohSettingsTab, "TabGroup.ImpostorRoles");
            CrewmateRoleCategoryHeader = CreateCategoryHeader(__instance, tohSettingsTab, "TabGroup.CrewmateRoles");
            NeutralRoleCategoryHeader = CreateCategoryHeader(__instance, tohSettingsTab, "TabGroup.NeutralRoles");
            AddOnCategoryHeader = CreateCategoryHeader(__instance, tohSettingsTab, "TabGroup.Addons");

            // 各設定スイッチを作成
            var template = __instance.GameSettingsTab.stringOptionOrigin;
            var scOptions = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
            foreach (var option in OptionItem.AllOptions)
            {
                if (option.OptionBehaviour == null)
                {
                    var stringOption = Object.Instantiate(template, tohSettingsTab.settingsContainer);
                    scOptions.Add(stringOption);
                    stringOption.SetClickMask(__instance.GameSettingsButton.ClickMask);
                    stringOption.SetUpFromData(stringOption.data, GameOptionsMenu.MASK_LAYER);
                    stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => { });
                    stringOption.TitleText.text = option.Name;
                    stringOption.Value = stringOption.oldValue = option.CurrentValue;
                    stringOption.ValueText.text = option.GetString();
                    stringOption.name = option.Name;

                    // タイトルの枠をデカくする
                    var indent = 0f;  // 親オプションがある場合枠の左を削ってインデントに見せる
                    var parent = option.Parent;
                    while (parent != null)
                    {
                        indent += 0.15f;
                        parent = parent.Parent;
                    }
                    stringOption.LabelBackground.size += new Vector2(2f - indent * 2, 0f);
                    stringOption.LabelBackground.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);
                    stringOption.TitleText.rectTransform.sizeDelta += new Vector2(2f - indent * 2, 0f);
                    stringOption.TitleText.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);

                    option.OptionBehaviour = stringOption;
                }
                option.OptionBehaviour.gameObject.SetActive(true);
            }
            tohSettingsTab.Children = scOptions;
            tohSettingsTab.gameObject.SetActive(false);

            // 各カテゴリまでスクロールするボタンを作成
            var jumpButtonY = -0.6f;
            var jumpToMainButton = CreateJumpToCategoryButton(__instance, tohSettingsTab, "TownOfHost.Resources.TabIcon_MainSettings.png", ref jumpButtonY, MainCategoryHeader);
            var jumpToImpButton = CreateJumpToCategoryButton(__instance, tohSettingsTab, "TownOfHost.Resources.TabIcon_ImpostorRoles.png", ref jumpButtonY, ImpostorRoleCategoryHeader);
            var jumpToCrewButton = CreateJumpToCategoryButton(__instance, tohSettingsTab, "TownOfHost.Resources.TabIcon_CrewmateRoles.png", ref jumpButtonY, CrewmateRoleCategoryHeader);
            var jumpToNeutralButton = CreateJumpToCategoryButton(__instance, tohSettingsTab, "TownOfHost.Resources.TabIcon_NeutralRoles.png", ref jumpButtonY, NeutralRoleCategoryHeader);
            var jumpToAddOnButton = CreateJumpToCategoryButton(__instance, tohSettingsTab, "TownOfHost.Resources.TabIcon_Addons.png", ref jumpButtonY, AddOnCategoryHeader);
        }
        private static MapSelectButton CreateJumpToCategoryButton(GameSettingMenu __instance, GameOptionsMenu tohTab, string resourcePath, ref float localY, CategoryHeaderMasked jumpTo)
        {
            var image = Utils.LoadSprite(resourcePath, 100f);
            var button = Object.Instantiate(__instance.GameSettingsTab.MapPicker.MapButtonOrigin, Vector3.zero, Quaternion.identity, tohTab.transform);
            button.SetImage(image, GameOptionsMenu.MASK_LAYER);
            button.transform.localPosition = new(7.1f, localY, -10f);
            button.Button.ClickMask = tohTab.ButtonClickMask;
            button.Button.OnClick.AddListener((Action)(() =>
            {
                tohTab.scrollBar.velocity = Vector2.zero;  // ドラッグの慣性によるスクロールを止める
                var relativePosition = tohTab.scrollBar.transform.InverseTransformPoint(jumpTo.transform.position);  // Scrollerのローカル空間における座標に変換
                var scrollAmount = CategoryJumpY - relativePosition.y;
                tohTab.scrollBar.Inner.localPosition = tohTab.scrollBar.Inner.localPosition + Vector3.up * scrollAmount;  // 強制スクロール
                tohTab.scrollBar.ScrollRelative(Vector2.zero);  // スクロール範囲内に収め，スクロールバーを更新する
            }));
            button.Button.activeSprites.transform.GetChild(0).gameObject.SetActive(false);  // チェックボックスを消す
            localY -= JumpButtonSpacing;
            return button;
        }
        private const float JumpButtonSpacing = 0.6f;
        // ジャンプしたカテゴリヘッダのScrollerとの相対Y座標がこの値になる
        private const float CategoryJumpY = 2f;
        private static CategoryHeaderMasked CreateCategoryHeader(GameSettingMenu __instance, GameOptionsMenu tohTab, string translationKey)
        {
            var categoryHeader = Object.Instantiate(__instance.GameSettingsTab.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, tohTab.settingsContainer);
            categoryHeader.name = translationKey;
            categoryHeader.Title.text = GetString(translationKey);
            var maskLayer = GameOptionsMenu.MASK_LAYER;
            categoryHeader.Background.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            if (categoryHeader.Divider != null)
            {
                categoryHeader.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            }
            categoryHeader.Title.fontMaterial.SetFloat("_StencilComp", 3f);
            categoryHeader.Title.fontMaterial.SetFloat("_Stencil", (float)maskLayer);
            categoryHeader.transform.localScale = Vector3.one * GameOptionsMenu.HEADER_SCALE;
            return categoryHeader;
        }

        // 初めてロール設定を表示したときに発生する例外(バニラバグ)の影響を回避するためPrefix
        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
        public static void ChangeTabPrefix(bool previewOnly)
        {
            if (!previewOnly)
            {
                if (tohSettingsTab)
                {
                    tohSettingsTab.gameObject.SetActive(false);
                }
                if (tohSettingsButton)
                {
                    tohSettingsButton.SelectButton(false);
                }
            }
        }

        public const string TOHMenuName = "TownOfHostTab";
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Initialize))]
    public static class GameOptionsMenuInitializePatch
    {
        public static void Postfix(GameOptionsMenu __instance)
        {
            foreach (var ob in __instance.Children)
            {
                switch (ob.Title)
                {
                    case StringNames.GameShortTasks:
                    case StringNames.GameLongTasks:
                    case StringNames.GameCommonTasks:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                        break;
                    case StringNames.GameKillCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    case StringNames.GameNumImpostors:
                        if (DebugModeManager.IsDebugMode)
                        {
                            ob.Cast<NumberOption>().ValidRange.min = 0;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;

        public static void Postfix(GameOptionsMenu __instance)
        {
            if (__instance.name != GameSettingMenuPatch.TOHMenuName) return;

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            var offset = 2.7f;
            var isOdd = true;

            UpdateCategoryHeader(GameSettingMenuPatch.MainCategoryHeader, ref offset);
            foreach (var option in OptionItem.MainOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.ImpostorRoleCategoryHeader, ref offset);
            foreach (var option in OptionItem.ImpostorRoleOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.CrewmateRoleCategoryHeader, ref offset);
            foreach (var option in OptionItem.CrewmateRoleOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.NeutralRoleCategoryHeader, ref offset);
            foreach (var option in OptionItem.NeutralRoleOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.AddOnCategoryHeader, ref offset);
            foreach (var option in OptionItem.AddOnOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }

            __instance.scrollBar.ContentYBounds.max = (-offset) - 1.5f;
        }
        private static void UpdateCategoryHeader(CategoryHeaderMasked categoryHeader, ref float offset)
        {
            offset -= GameOptionsMenu.HEADER_HEIGHT;
            categoryHeader.transform.localPosition = new(GameOptionsMenu.HEADER_X, offset, -2f);
        }
        private static void UpdateOption(ref bool isOdd, OptionItem item, ref float offset)
        {
            if (item?.OptionBehaviour == null || item.OptionBehaviour.gameObject == null) return;

            var enabled = true;
            var parent = item.Parent;

            // 親オプションの値を見て表示するか決める
            enabled = AmongUsClient.Instance.AmHost && !item.IsHiddenOn(Options.CurrentGameMode);
            var stringOption = item.OptionBehaviour;
            while (parent != null && enabled)
            {
                enabled = parent.GetBool();
                parent = parent.Parent;
            }

            item.OptionBehaviour.gameObject.SetActive(enabled);
            if (enabled)
            {
                // 見やすさのため交互に色を変える
                stringOption.LabelBackground.color = item is IRoleOptionItem roleOption ? roleOption.RoleColor : (isOdd ? Color.cyan : Color.white);

                offset -= GameOptionsMenu.SPACING_Y;
                if (item.IsHeader)
                {
                    // IsHeaderなら隙間を広くする
                    offset -= HeaderSpacingY;
                }
                item.OptionBehaviour.transform.localPosition = new Vector3(
                    GameOptionsMenu.START_POS_X,
                    offset,
                    -2f);

                isOdd = !isOdd;
            }
        }

        private const float HeaderSpacingY = 0.2f;
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
    public class StringOptionInitializePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = option.GetName(option is RoleSpawnChanceOptionItem);
            __instance.Value = __instance.oldValue = option.CurrentValue;
            __instance.ValueText.text = option.GetString();

            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            OptionItem.SyncAllOptions();
        }
    }
    [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.InitialSetup))]
    public static class RolesSettingsMenuPatch
    {
        public static void Postfix(RolesSettingsMenu __instance)
        {
            foreach (var ob in __instance.advancedSettingChildren)
            {
                switch (ob.Title)
                {
                    case StringNames.EngineerCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    case StringNames.ShapeshifterCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    default:
                        break;
                }
            }
        }
    }
    [HarmonyPatch(typeof(NormalGameOptionsV08), nameof(NormalGameOptionsV08.SetRecommendations), [typeof(int), typeof(bool), typeof(RulesPresets)])]
    public static class SetRecommendationsPatch
    {
        public static bool Prefix(NormalGameOptionsV08 __instance, int numPlayers, bool isOnline, RulesPresets rulesPresets)
        {
            switch (rulesPresets)
            {
                case RulesPresets.Standard: SetStandardRecommendations(__instance, numPlayers, isOnline); return false;
                // スタンダード以外のプリセットは一旦そのままにしておく
                default: return true;
            }
        }
        private static void SetStandardRecommendations(NormalGameOptionsV08 __instance, int numPlayers, bool isOnline)
        {
            numPlayers = Mathf.Clamp(numPlayers, 4, 15);
            __instance.PlayerSpeedMod = __instance.MapId == 4 ? 1.25f : 1f; //AirShipなら1.25、それ以外は1
            __instance.CrewLightMod = 0.5f;
            __instance.ImpostorLightMod = 1.75f;
            __instance.KillCooldown = 25f;
            __instance.NumCommonTasks = 2;
            __instance.NumLongTasks = 4;
            __instance.NumShortTasks = 6;
            __instance.NumEmergencyMeetings = 1;
            if (!isOnline)
                __instance.NumImpostors = NormalGameOptionsV08.RecommendedImpostors[numPlayers];
            __instance.KillDistance = 0;
            __instance.DiscussionTime = 0;
            __instance.VotingTime = 150;
            __instance.IsDefaults = true;
            __instance.ConfirmImpostor = false;
            __instance.VisualTasks = false;

            __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Shapeshifter);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Phantom);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Scientist);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.GuardianAngel);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Engineer);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Noisemaker);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Tracker);

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) //HideAndSeek
            {
                __instance.PlayerSpeedMod = 1.75f;
                __instance.CrewLightMod = 5f;
                __instance.ImpostorLightMod = 0.25f;
                __instance.NumImpostors = 1;
                __instance.NumCommonTasks = 0;
                __instance.NumLongTasks = 0;
                __instance.NumShortTasks = 10;
                __instance.KillCooldown = 10f;
            }
            if (Options.IsStandardHAS) //StandardHAS
            {
                __instance.PlayerSpeedMod = 1.75f;
                __instance.CrewLightMod = 5f;
                __instance.ImpostorLightMod = 0.25f;
                __instance.NumImpostors = 1;
                __instance.NumCommonTasks = 0;
                __instance.NumLongTasks = 0;
                __instance.NumShortTasks = 10;
                __instance.KillCooldown = 10f;
            }
        }
    }
}
