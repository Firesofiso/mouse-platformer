using UnityEngine;

namespace MouseButton.Debug
{
    public class ParticleLightDebug : MonoBehaviour
    {
        ParticleSystem _ps;
        ParticleSystem.Particle[] _particles;
        Camera _cam;
        GUIStyle _style;

        void Start()
        {
            _ps = GetComponent<ParticleSystem>();
            _cam = Camera.main;
        }

        void OnGUI()
        {
            if (_ps == null || _cam == null) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.yellow },
                    alignment = TextAnchor.MiddleCenter
                };
            }

            int count = _ps.particleCount;
            if (_particles == null || _particles.Length < count)
                _particles = new ParticleSystem.Particle[count];
            _ps.GetParticles(_particles);

            var lightTex = Shader.GetGlobalTexture("_ShapeLightTexture0");
            Texture2D readable = null;

            if (lightTex != null && lightTex is RenderTexture rt)
            {
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                readable = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                readable.Apply();
                RenderTexture.active = prev;
            }

            for (int i = 0; i < count; i++)
            {
                var worldPos = _particles[i].position;
                if (_ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                    worldPos = _ps.transform.TransformPoint(worldPos);

                var screenPos = _cam.WorldToScreenPoint(worldPos);
                if (screenPos.z < 0) continue;

                float guiY = Screen.height - screenPos.y;
                string label;

                if (readable != null)
                {
                    float u = screenPos.x / Screen.width;
                    float v = screenPos.y / Screen.height;
                    int px = Mathf.Clamp((int)(u * readable.width), 0, readable.width - 1);
                    int py = Mathf.Clamp((int)(v * readable.height), 0, readable.height - 1);
                    var c = readable.GetPixel(px, py);
                    float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                    label = lum.ToString("F3");
                }
                else
                {
                    label = "no tex";
                }

                GUI.Label(new Rect(screenPos.x - 20, guiY - 8, 40, 16), label, _style);
            }

            if (readable != null)
                Destroy(readable);
        }
    }
}
