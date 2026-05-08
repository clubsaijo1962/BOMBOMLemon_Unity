using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class RainingLemonItem : MonoBehaviour
    {
        private RectTransform _rt;
        private float _speed;
        private float _topY;
        private float _bottomY;

        // speed: pt/s   screenHeight: canvas reference height
        // startY: initial anchoredPosition.y   xOffset: horizontal offset from center
        // size: width & height in reference pts   rotation: initial Z rotation in degrees
        public void Init(float speed, float screenHeight, float startY, float xOffset, float size, float rotation)
        {
            _rt = GetComponent<RectTransform>();
            _speed  = speed;
            _topY   = screenHeight * 0.5f + size;
            _bottomY = -(screenHeight * 0.5f + size);

            _rt.sizeDelta           = new Vector2(size, size);
            _rt.anchoredPosition    = new Vector2(xOffset, startY);
            _rt.localEulerAngles    = new Vector3(0, 0, rotation);

            var img = GetComponent<Image>();
            if (img) img.preserveAspect = true;
        }

        void Update()
        {
            var pos = _rt.anchoredPosition;
            pos.y -= _speed * Time.deltaTime;
            if (pos.y < _bottomY) pos.y = _topY;
            _rt.anchoredPosition = pos;
        }
    }
}
