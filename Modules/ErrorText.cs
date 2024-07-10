using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public class ErrorText : MonoBehaviour
    {
        #region Singleton
        public static ErrorText Instance
        {
            get
            {
                return _instance;
            }
        }
        private static ErrorText _instance;
        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(this);
            }
        }
        #endregion
        public static void Create(TMPro.TextMeshPro baseText)
        {
            var Text = Instantiate(baseText);
            Text.fontSizeMax = Text.fontSizeMin = 2f;
            var instance = Text.gameObject.AddComponent<ErrorText>();
            instance.Text = Text;
            instance.name = "ErrorText";

            Text.enabled = false;
            Text.text = "NO ERROR";
            Text.color = Color.red;
            Text.outlineColor = Color.black;
            Text.alignment = TMPro.TextAlignmentOptions.Top;

            // 背景
            var bgObject = new GameObject("Background") { layer = LayerMask.NameToLayer("UI") };
            var bgRenderer = instance.background = bgObject.AddComponent<SpriteRenderer>();
            var bgTexture = new Texture2D(Screen.width, 150, TextureFormat.ARGB32, false);
            for (var x = 0; x < bgTexture.width; x++)
            {
                for (var y = 0; y < bgTexture.height; y++)
                {
                    bgTexture.SetPixel(x, y, new(0f, 0f, 0f, 0.6f));
                }
            }
            bgTexture.Apply();
            var bgSprite = Sprite.Create(bgTexture, new(0, 0, bgTexture.width, bgTexture.height), new(0.5f, 1f /* 上端の真ん中を中心とする */ ));
            bgRenderer.sprite = bgSprite;
            var bgTransform = bgObject.transform;
            bgTransform.parent = instance.transform;
            bgTransform.localPosition = new(0f, TextOffsetY, 1f);
            bgObject.SetActive(false);
        }

        public TMPro.TextMeshPro Text;
        private SpriteRenderer background;
        public Camera Camera;
        public List<ErrorData> AllErrors = new();
        public Vector3 TextOffset = new(0, TextOffsetY, -1000f);
        public void Update()
        {
            AllErrors.ForEach(err => err.IncreaseTimer());
            var ToRemove = AllErrors.Where(err => err.ErrorLevel <= 1 && 30f < err.Timer);
            if (ToRemove.Any())
            {
                AllErrors.RemoveAll(err => ToRemove.Contains(err));
                UpdateText();
                if (HnSFlag)
                    Destroy(this.gameObject);
            }
        }
        public void LateUpdate()
        {
            if (!Text.enabled) return;

            if (Camera == null)
                Camera = !HudManager.InstanceExists ? Camera.main : HudManager.Instance.PlayerCam.GetComponent<Camera>();
            if (Camera != null)
            {
                transform.position = AspectPosition.ComputeWorldPosition(Camera, AspectPosition.EdgeAlignments.Top, TextOffset);
            }
        }
        public void AddError(ErrorCode code)
        {
            var error = new ErrorData(code);
            if (0 < error.ErrorLevel)
                Logger.Error($"エラー発生: {error}: {error.Message}", "ErrorText");

            if (!AllErrors.Any(e => e.Code == code))
            {
                //まだ出ていないエラー
                AllErrors.Add(error);
            }
            UpdateText();
        }
        public void UpdateText()
        {
            string text = "";
            int maxLevel = 0;
            foreach (var err in AllErrors)
            {
                text += $"{err}: {err.Message}\n";
                if (maxLevel < err.ErrorLevel) maxLevel = err.ErrorLevel;
            }
            if (maxLevel == 0)
            {
                Hide();
            }
            else
            {
                if (!HnSFlag)
                    text += $"{GetString($"ErrorLevel{maxLevel}")}";
                Show();
            }
            if (GameStates.IsInGame && maxLevel != 3)
                text += $"\n{GetString("TerminateCommand")}: Shift+L+Enter";
            Text.text = text;
        }
        public void Clear()
        {
            AllErrors.RemoveAll(err => err.ErrorLevel != 3);
            UpdateText();
        }
        private void Show()
        {
            Text.enabled = true;
            background.gameObject.SetActive(true);
        }
        private void Hide()
        {
            Text.enabled = false;
            background.gameObject.SetActive(false);
        }

        public class ErrorData
        {
            public readonly ErrorCode Code;
            public readonly int ErrorType1;
            public readonly int ErrorType2;
            public readonly int ErrorLevel;
            public float Timer { get; private set; }
            public string Message => GetString(this.ToString());
            public ErrorData(ErrorCode code)
            {
                this.Code = code;
                this.ErrorType1 = (int)code / 10000;
                this.ErrorType2 = (int)code / 10 - ErrorType1 * 1000; // xxxyyy - xxx000
                this.ErrorLevel = (int)code - (int)code / 10 * 10;
                this.Timer = 0f;
            }
            public override string ToString()
            {
                // ERR-xxx-yyy-z
                return $"ERR-{ErrorType1:000}-{ErrorType2:000}-{ErrorLevel:0}";
            }
            public void IncreaseTimer() => Timer += Time.deltaTime;
        }

        public bool HnSFlag;

        const float TextOffsetY = 0.3f;
    }
    public enum ErrorCode
    {
        //xxxyyyz: ERR-xxx-yyy-z
        //  xxx: エラー大まかなの種類 (HUD関連, 追放処理関連など)
        //  yyy: エラーの詳細な種類 (BoutyHunterの処理, SerialKillerの処理など)
        //  z:   深刻度
        //    0: 処置不要 (非表示)
        //    1: 正常に動作しなければ廃村 (一定時間で非表示)
        //    2: 廃村を推奨 (廃村で非表示)
        //    3: ユーザー側では対処不能 (消さない)
        // ==========
        // 001 Main
        Main_DictionaryError = 0010003, // 001-000-3 Main Dictionary Error
        OptionIDDuplicate = 001_010_3, // 001-010-3 オプションIDが重複している(DEBUGビルド時のみ)
        // 002 サポート関連
        UnsupportedVersion = 002_000_1,  // 002-000-1 AmongUsのバージョンが古い

        // 010 参加/退出関連
        OnPlayerLeftPostfixFailedInGame = 010_000_2,  // 010-000-2 OnPlayerLeftPatch.Postfixがゲーム中に失敗
        OnPlayerLeftPostfixFailedInLobby = 010_001_2,  // 010-001-2 OnPlayerLeftPatch.Postfixがロビーで失敗

        // ==========
        // 000 Test
        NoError = 0000000, // 000-000-0 No Error
        TestError0 = 0009000, // 000-900-0 Test Error 0
        TestError1 = 0009101, // 000-910-1 Test Error 1
        TestError2 = 0009202, // 000-920-2 Test Error 2
        TestError3 = 0009303, // 000-930-3 Test Error 3
        HnsUnload = 000_804_1, // 000-804-1 Unloaded By HnS
    }
}