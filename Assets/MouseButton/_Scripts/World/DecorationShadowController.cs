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

            int count = 0;

            foreach (var entry in _shadowCells)
            {
                if (entry.tilemap == null) continue;

                var sprite = entry.tilemap.GetSprite(entry.cell);
                if (sprite == null) continue;

                var worldCenter = entry.tilemap.GetCellCenterWorld(entry.cell);
                var shape = BuildSilhouetteShape(sprite, worldCenter);
                if (shape == null || shape.Length < 3) continue;

                var localShape = new Vector3[shape.Length];
                for (int i = 0; i < shape.Length; i++)
                    localShape[i] = transform.InverseTransformPoint(shape[i]);

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

        Vector3[] BuildSilhouetteShape(Sprite sprite, Vector3 worldCenter)
        {
            var tex = sprite.texture;
            var texRect = sprite.textureRect;
            int w = (int)texRect.width;
            int h = (int)texRect.height;
            int ox = (int)texRect.x;
            int oy = (int)texRect.y;
            float ppu = sprite.pixelsPerUnit;
            var pivot = sprite.pivot;
            byte threshold = (byte)(_alphaThreshold * 255f);

            if (!tex.isReadable)
            {
                // Fallback: full tile box
                float bx0 = worldCenter.x + (0 - pivot.x) / ppu;
                float by0 = worldCenter.y + (0 - pivot.y) / ppu;
                float bx1 = worldCenter.x + (w - pivot.x) / ppu;
                float by1 = worldCenter.y + (h - pivot.y) / ppu;
                return new Vector3[]
                {
                    new(bx0, by1, 0), new(bx1, by1, 0),
                    new(bx1, by0, 0), new(bx0, by0, 0),
                };
            }

            var pixels = tex.GetPixels32(0);
            int texWidth = tex.width;

            // Per-column: find topmost and bottommost opaque pixel
            int[] topY = new int[w];
            int[] bottomY = new int[w];
            bool[] hasOpaque = new bool[w];

            for (int x = 0; x < w; x++)
            {
                topY[x] = -1;
                bottomY[x] = -1;
                for (int y = h - 1; y >= 0; y--)
                {
                    if (pixels[(oy + y) * texWidth + ox + x].a >= threshold)
                    {
                        if (topY[x] == -1) topY[x] = y;
                        bottomY[x] = y;
                        hasOpaque[x] = true;
                    }
                }
            }

            // Find leftmost and rightmost opaque columns
            int left = -1, right = -1;
            for (int x = 0; x < w; x++)
            {
                if (!hasOpaque[x]) continue;
                if (left == -1) left = x;
                right = x;
            }

            if (left == -1) return null;

            // Build polygon: bottom edge L→R, then top edge R→L
            var verts = new List<Vector3>();

            // Bottom edge (left to right)
            for (int x = left; x <= right; x++)
            {
                if (!hasOpaque[x]) continue;
                float wx = worldCenter.x + (x - pivot.x) / ppu;
                float wy = worldCenter.y + (bottomY[x] - pivot.y) / ppu;
                verts.Add(new Vector3(wx, wy, 0));
            }
            // Add bottom-right corner (right edge of rightmost pixel)
            {
                float wx = worldCenter.x + (right + 1 - pivot.x) / ppu;
                float wy = worldCenter.y + (bottomY[right] - pivot.y) / ppu;
                verts.Add(new Vector3(wx, wy, 0));
            }

            // Top edge (right to left)
            for (int x = right; x >= left; x--)
            {
                if (!hasOpaque[x]) continue;
                float wx = worldCenter.x + (x + 1 - pivot.x) / ppu;
                float wy = worldCenter.y + (topY[x] + 1 - pivot.y) / ppu;
                verts.Add(new Vector3(wx, wy, 0));
            }
            // Add top-left corner (left edge of leftmost pixel)
            {
                float wx = worldCenter.x + (left - pivot.x) / ppu;
                float wy = worldCenter.y + (topY[left] + 1 - pivot.y) / ppu;
                verts.Add(new Vector3(wx, wy, 0));
            }

            return SimplifyPolygon(verts);
        }

        static Vector3[] SimplifyPolygon(List<Vector3> verts)
        {
            if (verts.Count < 3) return verts.ToArray();

            var result = new List<Vector3>();
            int n = verts.Count;

            for (int i = 0; i < n; i++)
            {
                var prev = verts[(i - 1 + n) % n];
                var curr = verts[i];
                var next = verts[(i + 1) % n];

                // Skip collinear points
                var d1 = (curr - prev).normalized;
                var d2 = (next - curr).normalized;
                if (Vector3.Cross(d1, d2).sqrMagnitude > 0.0001f)
                    result.Add(curr);
            }

            return result.Count >= 3 ? result.ToArray() : verts.ToArray();
        }

        // ── Sprite rect analysis (same logic as TileSpriteShadowCaster) ──

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
                result.Add(r);
                if (r.y == 0) hasBottomFullWidth = true;
                if (r.yMax == h) hasTopFullWidth = true;
            }

            if (!hasBottomFullWidth)
                BuildEdgeSpans(result, 0, h, pixels, texWidth, ox, oy, w, threshold);
            if (!hasTopFullWidth)
                BuildEdgeSpans(result, h - 1, h, pixels, texWidth, ox, oy, w, threshold);

            return result;
        }

        void BuildEdgeSpans(List<RectInt> result, int y, int h, Color32[] pixels,
            int texWidth, int ox, int oy, int w, byte threshold)
        {
            int runStart = -1;

            for (int x = 0; x < w; x++)
            {
                // All-opaque column vote: any transparent pixel disqualifies
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

                        // Same y-range, horizontally adjacent
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

                        // Same x-range, vertically adjacent
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
