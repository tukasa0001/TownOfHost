using UnityEngine;

namespace TownOfHost
{
    public class ErrorText : DestroyableSingleton<ErrorText>
    {
        public static void Create(TMPro.TextMeshPro baseText)
        {
            var Text = Instantiate(baseText);
            var instance = Text.gameObject.AddComponent<ErrorText>();
            instance.Text = Text;
            DontDestroyOnLoad(instance.gameObject);
            instance.DontDestroy = true;

            Text.enabled = false;
            Text.text = "";
            Text.alignment = TMPro.TextAlignmentOptions.Top;
        }

        public TMPro.TextMeshPro Text;
    }
}