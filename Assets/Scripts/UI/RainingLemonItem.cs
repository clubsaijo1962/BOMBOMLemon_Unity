using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    [RequireComponent(typeof(RectTransform))]
    public class RainingLemonItem : MonoBehaviour
    {
        private RectTransform _rt;
        private float _speed;
        private float _halfH;
        private float _halfW;

        // horizontal wave
        private float _baseX;
        private float _waveAmp;
        private float _waveFreq;
        private float _wavePhase;

        // rotation
        private float _rotSpeed;
        private float _rot;

        // squish (puni-puni)
        private float _squishFreq;
        private float _squishPhase;
        private float _squishAmt;

        private float _t;

        public void Init(float speed, float screenWidth, float screenHeight,
                         float startY, float xOffset, float size)
        {
            _rt      = GetComponent<RectTransform>();
            _speed   = speed;
            _halfH   = screenHeight * 0.5f;
            _halfW   = screenWidth  * 0.5f;
            _baseX   = xOffset;
            _t       = Random.Range(0f, 100f);

            _rt.sizeDelta        = new Vector2(size, size);
            _rt.anchoredPosition = new Vector2(xOffset, startY);

            // randomize rotation start
            _rot      = Random.Range(0f, 360f);
            _rotSpeed = Random.Range(15f, 45f) * (Random.value > 0.5f ? 1f : -1f);

            // horizontal sine drift (gentle)
            _waveAmp   = Random.Range(10f, 28f);
            _waveFreq  = Random.Range(0.15f, 0.4f);
            _wavePhase = Random.Range(0f, Mathf.PI * 2f);

            // squish/stretch (subtle puni-puni)
            _squishFreq  = Random.Range(0.8f, 1.8f);
            _squishPhase = Random.Range(0f, Mathf.PI * 2f);
            _squishAmt   = Random.Range(0.03f, 0.07f);
        }

        void Update()
        {
            _t += Time.deltaTime;

            // fall
            var pos  = _rt.anchoredPosition;
            pos.y   -= _speed * Time.deltaTime;
            pos.x    = _baseX + Mathf.Sin(_t * _waveFreq * Mathf.PI * 2f + _wavePhase) * _waveAmp;
            _rt.anchoredPosition = pos;

            // rotation
            _rot += _rotSpeed * Time.deltaTime;
            _rt.localRotation = Quaternion.Euler(0f, 0f, _rot);

            // squish (x stretches when y compresses and vice versa)
            float s  = Mathf.Sin(_t * _squishFreq * Mathf.PI * 2f + _squishPhase) * _squishAmt;
            _rt.localScale = new Vector3(1f + s, 1f - s * 0.7f, 1f);

            if (pos.y < -_halfH - 80f)
                Respawn();
        }

        private void Respawn()
        {
            _baseX = Random.Range(-_halfW * 0.9f, _halfW * 0.9f);
            var pos = _rt.anchoredPosition;
            pos.y = _halfH + 80f;
            pos.x = _baseX;
            _rt.anchoredPosition = pos;
            _wavePhase  = Random.Range(0f, Mathf.PI * 2f);
            _squishPhase = Random.Range(0f, Mathf.PI * 2f);
        }
    }
}
