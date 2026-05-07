using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // Applies a Japanese-capable OS font to every Text component in this canvas at Awake.
    // Needed because Font.CreateDynamicFontFromOSFont() cannot be serialised in a scene;
    // assigning it in TitleSceneBuilder results in a null reference after save/reload.
    [DisallowMultipleComponent]
    public class JPFontLoader : MonoBehaviour
    {
        void Awake()
        {
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "Hiragino Sans", "HiraKakuProN-W3", "HiraginoSans-W3",
                        "Noto Sans CJK JP", "NotoSansCJKjp-Regular",
                        "Arial Unicode MS", "Arial" }, 14);

            if (font == null) return;

            foreach (var t in GetComponentsInChildren<Text>(true))
                t.font = font;
        }
    }
}
