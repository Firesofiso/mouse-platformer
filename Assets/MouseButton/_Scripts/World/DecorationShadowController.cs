using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MouseButton.World
{
    public class TilemapShadowController : MonoBehaviour
    {
        [Serializable]
        public struct TileEntry
        {
            public Tilemap tilemap;
            public Vector3Int cell;
        }

        [SerializeField] Tilemap _targetTilemap;
        [SerializeField] List<TileEntry> _shadowCells = new();
        [SerializeField] GameObject _shadowPrefab;
        [SerializeField] bool _selfShadows;
        [SerializeField] float _alphaThreshold = 0.5f;
        [SerializeField] bool _includeTopEdge = true;
        [SerializeField] bool _includeBottomEdge = true;

        static FieldInfo s_shapePathField;
        static FieldInfo s_shapePathHashField;

        static void EnsureReflectionCache()
        {
            if (s_shapePathField != null) return;
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            s_shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", flags);
            s_shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", flags);
        }

        public Tilemap TargetTilemap
        {
            get => _targetTilemap;
            set => _targetTilemap = value;
        }

        public IReadOnlyList<TileEntry> ShadowCells => _shadowCells;

        // ── Editor bake API ──

        public int DeleteShadowObjects()
        {
            int count = 0;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.GetComponent<ShadowCaster2D>() != null)
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                    count++;
                }
            }
            return count;
        }

        public int CreateShadowObjects()
        {
            EnsureReflectionCache();

            var worldRects = new List<Rect>();

            foreach (var entry in _shadowCells)
            {
                if (entry.tilemap == null) continue;

                var sprite = entry.tilemap.GetSprite(entry.cell);
                if (sprite == null) continue;

                var rects = BuildSpriteRects(sprite);
                if (rects == null || rects.Count == 0) continue;

                var worldCenter = entry.tilemap.GetCellCenterWorld(entry.cell);
                float ppu = sprite.pixelsPerUnit;
                var pivot = sprite.pivot;

                foreach (var rect in rects)
                {
                    float x0 = worldCenter.x + (rect.x - pivot.x) / ppu;
                    float y0 = worldCenter.y + (rect.y - pivot.y) / ppu;
                    float x1 = worldCenter.x + (rect.xMax - pivot.x) / ppu;
                    float y1 = worldCenter.y + (rect.yMax - pivot.y) / ppu;
                    worldRects.Add(new Rect(x0, y0, x1 - x0, y1 - y0));
                }
            }

            worldRects = MergeWorldRects(worldRects);

            int count = 0;
            foreach (var wr in worldRects)
            {
                var localShape = new Vector3[]
                {
                    transform.InverseTransformPoint(new Vector3(wr.xMin, wr.yMax, 0)),
                    transform.InverseTransformPoint(new Vector3(wr.xMax, wr.yMax, 0)),
                    transform.InverseTransformPoint(new Vector3(wr.xMax, wr.yMin, 0)),
                    transform.InverseTransformPoint(new Vector3(wr.xMin, wr.yMin, 0)),
                };

                var shadowObj = new GameObject("BakedShadow");
                shadowObj.transform.SetParent(transform, false);
                shadowObj.transform.localPosition = Vector3.zero;

                var caster = shadowObj.AddComponent<ShadowCaster2D>();
                caster.selfShadows = _selfShadows;
                caster.castsShadows = true;

                s_shapePathField.SetValue(caster, localShape);
                s_shapePathHashField.SetValue(caster, UnityEngine.Random.Range(int.MinValue, int.MaxValue));

                caster.enabled = false;
                caster.enabled = true;
                count++;
            }

            return count;
        }

        // ── Sprite rect analysis ──

        List<RectInt> BuildSpriteRects(Sprite sprite)
        {
            var tex = sprite.texture;
            var texRect = sprite.textureRect;

            int w = (int)texRect.width;
            int h = (int)texRect.height;
            int ox = (int)texRect.x;
            int oy = (int)texRect.y;

            if (!tex.isReadable)
                return new List<RectInt> { new(0, 0, w, h) };

            var pixels = tex.GetPixels32(0);
            int texWidth = tex.width;
            byte threshold = (byte)(_alphaThreshold * 255f);

            var spans = new List<(int x0, int x1, int y)>();
            for (int y = 0; y < h; y++)
            {
                int rowBase = (oy + y) * texWidth + ox;
                int runStart = -1;

                for (int x = 0; x < w; x++)
                {
                    bool opaque = pixels[rowBase + x].a >= threshold;

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

            var activeRects = new Dictionary<long, RectInt>();
            var rects = new List<RectInt>();

            for (int y = 0; y < h; y++)
            {
                var rowSpans = new HashSet<long>();
                foreach (var span in spans)
                {
                    if (span.y != y) continue;
                    long key = ((long)span.x0 << 32) | (long)(uint)span.x1;
                    rowSpans.Add(key);

                    if (activeRects.TryGetValue(key, out var existing))
                    {
                        if (existing.yMax == y)
                        {
                            existing.height++;
                            activeRects[key] = existing;
                        }
                        else
                        {
                            rects.Add(existing);
                            activeRects[key] = new RectInt(span.x0, y, span.x1 - span.x0, 1);
                        }
                    }
                    else
                    {
                        activeRects[key] = new RectInt(span.x0, y, span.x1 - span.x0, 1);
                    }
                }

                var toRemove = new List<long>();
                foreach (var kvp in activeRects)
                {
                    if (!rowSpans.Contains(kvp.Key) && kvp.Value.yMax <= y)
                    {
                        rects.Add(kvp.Value);
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (var k in toRemove)
                    activeRects.Remove(k);
            }

            foreach (var kvp in activeRects)
                rects.Add(kvp.Value);

            return SelectShadowRects(rects, w, h, pixels, texWidth, ox, oy, threshold);
        }

        List<RectInt> SelectShadowRects(List<RectInt> candidates, int w, int h,
            Color32[] pixels, int texWidth, int ox, int oy, byte threshold)
        {
            var result = new List<RectInt>();

            bool hasBottomFullWidth = false;
            bool hasTopFullWidth = false;

            foreach (var r in candidates)
            {
                if (r.x != 0 || r.xMax != w) continue;
                if (r.y == 0 && !_includeBottomEdge) continue;
                if (r.yMax == h && !_includeTopEdge) continue;
                result.Add(r);
                if (r.y == 0) hasBottomFullWidth = true;
                if (r.yMax == h) hasTopFullWidth = true;
            }

            if (_includeBottomEdge && !hasBottomFullWidth)
                BuildEdgeSpans(result, 0, h, pixels, texWidth, ox, oy, w, threshold);
            if (_includeTopEdge && !hasTopFullWidth)
                BuildEdgeSpans(result, h - 1, h, pixels, texWidth, ox, oy, w, threshold);

            return result;
        }

        void BuildEdgeSpans(List<RectInt> result, int y, int h, Color32[] pixels,
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
                    result.Add(new RectInt(runStart, y, runEnd - runStart, 1));
                    runStart = -1;
                }
            }
        }

        // ── World-space rect merging ──

        static List<Rect> MergeWorldRects(List<Rect> rects)
        {
            if (rects.Count == 0) return rects;

            const float eps = 0.01f;
            bool changed = true;

            while (changed)
            {
                changed = false;
                for (int i = 0; i < rects.Count; i++)
                {
                    for (int j = i + 1; j < rects.Count; j++)
                    {
                        var a = rects[i];
                        var b = rects[j];

                        if (Mathf.Abs(a.yMin - b.yMin) < eps &&
                            Mathf.Abs(a.yMax - b.yMax) < eps &&
                            (Mathf.Abs(a.xMax - b.xMin) < eps || Mathf.Abs(b.xMax - a.xMin) < eps))
                        {
                            rects[i] = new Rect(
                                Mathf.Min(a.xMin, b.xMin), a.yMin,
                                Mathf.Max(a.xMax, b.xMax) - Mathf.Min(a.xMin, b.xMin), a.height);
                            rects.RemoveAt(j);
                            changed = true;
                            break;
                        }

                        if (Mathf.Abs(a.xMin - b.xMin) < eps &&
                            Mathf.Abs(a.xMax - b.xMax) < eps &&
                            (Mathf.Abs(a.yMax - b.yMin) < eps || Mathf.Abs(b.yMax - a.yMin) < eps))
                        {
                            rects[i] = new Rect(
                                a.xMin, Mathf.Min(a.yMin, b.yMin),
                                a.width, Mathf.Max(a.yMax, b.yMax) - Mathf.Min(a.yMin, b.yMin));
                            rects.RemoveAt(j);
                            changed = true;
                            break;
                        }
                    }
                    if (changed) break;
                }
            }

            return rects;
        }

        // ── Cell management ──

        public void AddCell(Tilemap tilemap, Vector3Int cell)
        {
            if (HasCell(tilemap, cell)) return;
            _shadowCells.Add(new TileEntry { tilemap = tilemap, cell = cell });
        }

        public bool RemoveCell(Tilemap tilemap, Vector3Int cell)
        {
            for (int i = _shadowCells.Count - 1; i >= 0; i--)
            {
                if (_shadowCells[i].tilemap == tilemap && _shadowCells[i].cell == cell)
                {
                    _shadowCells.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public bool HasCell(Tilemap tilemap, Vector3Int cell)
        {
            foreach (var e in _shadowCells)
                if (e.tilemap == tilemap && e.cell == cell) return true;
            return false;
        }
    }
}
