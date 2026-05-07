using UnityEngine;

namespace BOMBOMLemon
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        void Awake() => Apply();

        void Apply()
        {
            var rt     = GetComponent<RectTransform>();
            var safe   = Screen.safeArea;
            var full   = new Vector2(Screen.width, Screen.height);

            rt.anchorMin = new Vector2(safe.x / full.x, safe.y / full.y);
            rt.anchorMax = new Vector2((safe.x + safe.width) / full.x,
                                       (safe.y + safe.height) / full.y);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
