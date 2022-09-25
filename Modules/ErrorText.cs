using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            var instance = Text.gameObject.AddComponent<ErrorText>();
            instance.Text = Text;
            instance.name = "ErrorText";

            Text.enabled = false;
            Text.text = "NO ERROR";
            Text.color = Color.red;
            Text.outlineColor = Color.black;
            Text.alignment = TMPro.TextAlignmentOptions.Top;
        }

        public TMPro.TextMeshPro Text;
        public Camera Camera;
        public List<ErrorData> AllErrors = new();
        public Vector3 TextOffset = new(0, 0.3f, -1000f);
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
            switch (maxLevel)
            {
                case 0:
                    Text.enabled = false;
                    break;
                case 1:
                    text += $"{GetString("ErrorLevel1")}\n";
                    Text.enabled = true;
                    break;
                case 2:
                    text += $"{GetString("ErrorLevel2")}\n";
                    Text.enabled = true;
                    break;
            }
            text += $"{GetString("TerminateCommand")}: Shift+L+Enter";
            Text.text = text;
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
        NoError = 0000000, // 000-000-0 No Error
    }
}