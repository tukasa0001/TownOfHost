using UnityEngine;

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

            Text.enabled = true;
            Text.text = "TestMessage";
            Text.alignment = TMPro.TextAlignmentOptions.Top;
        }

        public TMPro.TextMeshPro Text;
        public Camera Camera;
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
    }
}