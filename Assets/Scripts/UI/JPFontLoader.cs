using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    public class JPFontLoader : MonoBehaviour
    {
        private static Font _jpFont;

        void Awake() => Apply(transform);

        public static Font GetFont()
        {
            if (_jpFont != null) return _jpFont;
            _jpFont = Font.CreateDynamicFontFromOSFont(new[]
            {
                "Hiragino Sans", "HiraKakuProN-W3", "HiraKakuProN-W6",
                "NotoSansCJKjp-Regular", "Noto Sans CJK JP", "Noto Sans JP",
                "Arial Unicode MS", "Arial"
            }, 14);
            return _jpFont;
        }

        private static void Apply(Transform t)
        {
            var txt = t.GetComponent<Text>();
            if (txt != null) txt.font = GetFont();
            foreach (Transform child in t) Apply(child);
        }
    }
}
