using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // Force layout rebuild when this panel becomes active.
    // Fixes ContentSizeFitter not updating after the panel was inactive at scene start.
    [RequireComponent(typeof(RectTransform))]
    public class SheetPanelActivator : MonoBehaviour
    {
        void OnEnable()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
