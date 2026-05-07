using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    [RequireComponent(typeof(RectTransform))]
    public class RainingLemonItem : MonoBehaviour
    {
        private float _speed;
        private float _screenHeight;
        private RectTransform _rect;
        private float _rotation;

        public void Init(float speed, float screenHeight, float startY, float xOffset, float size, float rotation)
        {
            _rect = GetComponent<RectTransform>();
            _speed = speed;
            _screenHeight = screenHeight;
            _rotation = rotation;

            _rect.sizeDelta = new Vector2(size, size);
            _rect.anchoredPosition = new Vector2(xOffset, startY);
            _rect.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        void Update()
        {
            _rect.anchoredPosition += Vector2.down * _speed * Time.deltaTime;

            if (_rect.anchoredPosition.y < -_screenHeight * 0.5f - 80f)
                ResetPosition();
        }

        private void ResetPosition()
        {
            var pos = _rect.anchoredPosition;
            pos.y = _screenHeight * 0.5f + 60f;
            _rect.anchoredPosition = pos;
        }
    }
}
