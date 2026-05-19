using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MouseButton.World
{
    [RequireComponent(typeof(Light2D))]
    public class ElectricLightPulse : MonoBehaviour
    {
        [SerializeField] float _pulseSpeed = 3f;
        [SerializeField] float _pulseMin = 0.2f;
        [SerializeField] float _pulseMax = 0.8f;
        [SerializeField] float _flickerSpeed = 15f;
        [SerializeField] float _flickerAmount = 0.15f;

        Light2D _light;
        float _baseIntensity;

        void Awake()
        {
            _light = GetComponent<Light2D>();
            _baseIntensity = _light.intensity;
        }

        void Update()
        {
            float pulse = Mathf.Lerp(_pulseMin, _pulseMax,
                Mathf.Sin(Time.time * _pulseSpeed) * 0.5f + 0.5f);

            float flicker = 1f - _flickerAmount *
                (Mathf.Floor(Time.time * _flickerSpeed) % 13f < 1f ? 1f : 0f);

            _light.intensity = _baseIntensity * pulse * flicker;
        }
    }
}
