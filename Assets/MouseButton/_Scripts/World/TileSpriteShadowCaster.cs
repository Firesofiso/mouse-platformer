using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MouseButton.World
{
    public class TileSpriteShadowCaster : MonoBehaviour
    {
        [SerializeField] float _alphaThreshold = 0.5f;

        Tilemap _tilemap;
        Vector3Int _cell;
        bool _initialized;

        // Cache reflection lookups — same fields for every ShadowCaster2D instance
        static FieldInfo s_shapePathField;
        static FieldInfo s_shapePathHashField;

        static void EnsureReflectionCache()
        {
            if (s_shapePathField != null) return;
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            s_shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", flags);
            s_shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", flags);
        }

        public void Init(Tilemap tilemap, Vector3Int cell)
        {
            _tilemap = tilemap;
            _cell = cell;
            _initialized = true;
        }

        void Start()
        {
            if (!_initialized)
            {
                _tilemap = GetComponentInParent<Tilemap>();
                if (_tilemap != null)
                    _cell = _tilemap.WorldToCell(transform.position);
            }

            if (_tilemap == null) return;

            var sprite = _tilemap.GetSprite(_cell);
            if (sprite == null) return;

            // Remove the prefab's own ShadowCaster2D — we'll use children instead
            var ownCaster = GetComponent<ShadowCaster2D>();
            if (ownCaster != null)
                Destroy(ownCaster);

            EnsureReflectionCache();

            var rects = BuildRects(sprite);
            if (rects == null || rects.Count == 0) return;

            foreach (var rect in rects)
                SpawnShadowChild(rect, sprite.pixelsPerUnit, sprite.pivot);
        }

        void SpawnShadowChild(PixelRect rect, float ppu, Vector2 pivot)
        {
            var child = new GameObject("Shadow");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.zero;

            var caster = child.AddComponent<ShadowCaster2D>();
            caster.selfShadows = false;
            caster.castsShadows = true;

            var shape = new Vector3[]
            {
                new Vector3((rect.x0 - pivot.x) / ppu, (rect.y1 - pivot.y) / ppu, 0),
                new Vector3((rect.x1 - pivot.x) / ppu, (rect.y1 - pivot.y) / ppu, 0),
                new Vector3((rect.x1 - pivot.x) / ppu, (rect.y0 - pivot.y) / ppu, 0),
                new Vector3((rect.x0 - pivot.x) / ppu, (rect.y0 - pivot.y) / ppu, 0),
            };

            s_shapePathField.SetValue(caster, shape);
            s_shapePathHashField.SetValue(caster, Random.Range(int.MinValue, int.MaxValue));

            caster.enabled = false;
            caster.enabled = true;
        }

        struct PixelRect
        {
            public int x0, y0, x1, y1;
        }

        List<PixelRect> BuildRects(Sprite sprite)
        {
            var tex = sprite.texture;
            var texRect = sprite.textureRect;

            int w = (int)texRect.width;
            int h = (int)texRect.height;
            int ox = (int)texRect.x;
            int oy = (int)texRect.y;

            if (!tex.isReadable)
            {
                return new List<PixelRect>
                {
                    new PixelRect { x0 = 0, y0 = 0, x1 = w, y1 = h }
                };
            }

            // Single native call instead of per-pixel GetPixel
            var pixels = tex.GetPixels32(0);
            int texWidth = tex.width;

            // Build horizontal spans of opaque pixels per row
            var spans = new List<(int x0, int x1, int y)>();
            for (int y = 0; y < h; y++)
            {
                int rowBase = (oy + y) * texWidth + ox;
                int runStart = -1;

                for (int x = 0; x < w; x++)
                {
                    bool opaque = pixels[rowBase + x].a >= (byte)(_alphaThreshold * 255f);

                    if (opaque && runStart == -1)
                        runStart = x;

                    if ((!opaque || x == w - 1) && runStart != -1)
                    {
                        int runEnd = opaque ? x + 1 : x;
                        spans.Add((runStart, runEnd, y));
                        runStart = -1;
                    }
                }
            }

            // Merge vertically adjacent spans with same x-range into rectangles.
            // Dictionary keyed on (x0, x1) tracks the currently-growing rect for each column range.
            // O(n) instead of O(n²).
            var activeRects = new Dictionary<long, PixelRect>();
            var rects = new List<PixelRect>();

            for (int y = 0; y < h; y++)
            {
                // Collect spans for this row
                var rowSpans = new HashSet<long>();

                foreach (var span in spans)
                {
                    if (span.y != y) continue;
                    long key = ((long)span.x0 << 32) | (long)(uint)span.x1;
                    rowSpans.Add(key);

                    if (activeRects.TryGetValue(key, out var existing))
                    {
                        if (existing.y1 == y)
                        {
                            // Extend existing rect
                            existing.y1 = y + 1;
                            activeRects[key] = existing;
                        }
                        else
                        {
                            // Gap — flush old, start new
                            if (existing.y0 == 0 || existing.y1 == h)
                                rects.Add(existing);
                            activeRects[key] = new PixelRect { x0 = span.x0, x1 = span.x1, y0 = y, y1 = y + 1 };
                        }
                    }
                    else
                    {
                        activeRects[key] = new PixelRect { x0 = span.x0, x1 = span.x1, y0 = y, y1 = y + 1 };
                    }
                }

                // Flush rects whose key wasn't in this row (span ended)
                var toRemove = new List<long>();
                foreach (var kvp in activeRects)
                {
                    if (!rowSpans.Contains(kvp.Key) && kvp.Value.y1 <= y)
                    {
                        if (kvp.Value.y0 == 0 || kvp.Value.y1 == h)
                            rects.Add(kvp.Value);
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (var k in toRemove)
                    activeRects.Remove(k);
            }

            // Flush remaining active rects
            foreach (var kvp in activeRects)
            {
                if (kvp.Value.y0 == 0 || kvp.Value.y1 == h)
                    rects.Add(kvp.Value);
            }

            return SelectShadowRects(rects, w, h, pixels, texWidth, ox, oy);
        }
        /// <summary>
        /// Prefer the largest full-width rect (x0==0, x1==w) touching top or bottom.
        /// If none are full-width, fall back to 1px edge spans from the top/bottom rows.
        /// </summary>
        List<PixelRect> SelectShadowRects(List<PixelRect> candidates, int w, int h,
            Color32[] pixels, int texWidth, int ox, int oy)
        {
            var result = new List<PixelRect>();
            byte threshold = (byte)(_alphaThreshold * 255f);

            bool hasBottomFullWidth = false;
            bool hasTopFullWidth = false;

            foreach (var r in candidates)
            {
                if (r.x0 != 0 || r.x1 != w) continue;
                result.Add(r);
                if (r.y0 == 0) hasBottomFullWidth = true;
                if (r.y1 == h) hasTopFullWidth = true;
            }

            if (!hasBottomFullWidth)
                BuildEdgeSpans(result, 0, h, pixels, texWidth, ox, oy, w, threshold);
            if (!hasTopFullWidth)
                BuildEdgeSpans(result, h - 1, h, pixels, texWidth, ox, oy, w, threshold);

            return result;
        }

        void BuildEdgeSpans(List<PixelRect> result, int y, int h, Color32[] pixels,
            int texWidth, int ox, int oy, int w, byte threshold)
        {
            int runStart = -1;

            for (int x = 0; x < w; x++)
            {
                bool opaque = true;
                for (int cy = 0; cy < h; cy++)
                {
                    if (pixels[(oy + cy) * texWidth + ox + x].a < threshold)
                    {
                        opaque = false;
                        break;
                    }
                }

                if (opaque && runStart == -1)
                    runStart = x;

                if ((!opaque || x == w - 1) && runStart != -1)
                {
                    int runEnd = opaque ? x + 1 : x;
                    result.Add(new PixelRect { x0 = runStart, x1 = runEnd, y0 = y, y1 = y + 1 });
                    runStart = -1;
                }
            }
        }
    }
}
