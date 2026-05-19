using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MouseButton.World
{
    [RequireComponent(typeof(TilemapCollider2D))]
    public class TilemapShadowBaker : MonoBehaviour
    {
        [SerializeField] bool _selfShadows;

        static FieldInfo s_shapePathField;
        static FieldInfo s_shapePathHashField;

        static void EnsureReflectionCache()
        {
            if (s_shapePathField != null) return;
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            s_shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", flags);
            s_shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", flags);
        }

        void Start()
        {
            Bake();
        }

        [ContextMenu("Bake Shadows")]
        public void Bake()
        {
            EnsureReflectionCache();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "BakedShadow")
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }

            var tilemapCollider = GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null) return;

            var tilemap = GetComponent<Tilemap>();
            if (tilemap == null) return;

            var bounds = tilemap.cellBounds;

            foreach (var cellPos in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cellPos)) continue;

                var worldCenter = tilemap.GetCellCenterWorld(cellPos);
                var cellSize = tilemap.cellSize;

                // Each tile gets a box shadow matching its cell
                var halfX = cellSize.x * 0.5f;
                var halfY = cellSize.y * 0.5f;
                var localCenter = transform.InverseTransformPoint(worldCenter);

                var shapePath = new Vector3[]
                {
                    new Vector3(localCenter.x - halfX, localCenter.y + halfY, 0f),
                    new Vector3(localCenter.x + halfX, localCenter.y + halfY, 0f),
                    new Vector3(localCenter.x + halfX, localCenter.y - halfY, 0f),
                    new Vector3(localCenter.x - halfX, localCenter.y - halfY, 0f),
                };

                var shadowObj = new GameObject("BakedShadow");
                shadowObj.transform.SetParent(transform, false);
                shadowObj.transform.localPosition = Vector3.zero;

                var caster = shadowObj.AddComponent<ShadowCaster2D>();
                caster.selfShadows = _selfShadows;
                caster.castsShadows = true;

                s_shapePathField.SetValue(caster, shapePath);
                s_shapePathHashField.SetValue(caster, Random.Range(int.MinValue, int.MaxValue));

                caster.enabled = false;
                caster.enabled = true;
            }
        }
    }
}
