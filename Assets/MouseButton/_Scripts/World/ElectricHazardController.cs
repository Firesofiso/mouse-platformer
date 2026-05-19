using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MouseButton.World
{
    public class ElectricHazardController : MonoBehaviour
    {
        [Serializable]
        public struct TileEntry
        {
            public Tilemap tilemap;
            public Vector3Int cell;
        }

        [Header("Tile Assignments")]
        [SerializeField] List<TileEntry> _platformCells = new();
        [SerializeField] List<TileEntry> _decorationCells = new();

        [Header("VFX")]
        [SerializeField] GameObject _platformVfxPrefab;
        [SerializeField] GameObject _decorationVfxPrefab;

        [Header("Tile Swaps (optional)")]
        [SerializeField] TileBase _platformOnTile;
        [SerializeField] TileBase _platformOffTile;
        [SerializeField] TileBase _panelOnTile;
        [SerializeField] TileBase _panelOffTile;

        [Header("State")]
        [SerializeField] bool _startsActive = true;

        bool _active;
        List<GameObject> _vfxInstances = new();

        public bool Active => _active;

        void Start()
        {
            SpawnVFX();
            _active = _startsActive;
            Apply();
        }

        public void Toggle()
        {
            _active = !_active;
            Apply();
        }

        public void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            Apply();
        }

        void SpawnVFX()
        {
            SpawnForCells(_platformCells, _platformVfxPrefab, addHazardTrigger: true);
            SpawnForCells(_decorationCells, _decorationVfxPrefab, addHazardTrigger: false);
        }

        void SpawnForCells(List<TileEntry> cells, GameObject prefab, bool addHazardTrigger)
        {
            if (prefab == null) return;

            foreach (var entry in cells)
            {
                if (entry.tilemap == null) continue;
                var worldPos = entry.tilemap.GetCellCenterWorld(entry.cell);
                var instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                _vfxInstances.Add(instance);

                if (addHazardTrigger)
                {
                    var box = instance.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    var cellSize = entry.tilemap.cellSize;
                    box.size = new Vector2(cellSize.x, cellSize.y);
                    instance.AddComponent<HazardTrigger>();
                }

                var overlay = instance.transform.Find("PulseOverlay");
                if (overlay == null) continue;
                var sr = overlay.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                var tileSprite = entry.tilemap.GetSprite(entry.cell);
                if (tileSprite != null) sr.sprite = tileSprite;
            }
        }

        void Apply()
        {
            foreach (var entry in _platformCells)
            {
                if (entry.tilemap == null) continue;

                if (_platformOnTile != null && _platformOffTile != null)
                    entry.tilemap.SetTile(entry.cell, _active ? _platformOnTile : _platformOffTile);
            }

            foreach (var vfx in _vfxInstances)
            {
                if (vfx != null) vfx.SetActive(_active);
            }

            foreach (var entry in _decorationCells)
            {
                if (entry.tilemap == null) continue;

                if (_panelOnTile != null && _panelOffTile != null)
                    entry.tilemap.SetTile(entry.cell, _active ? _panelOnTile : _panelOffTile);
            }
        }

        public void AddPlatformCell(Tilemap tilemap, Vector3Int cell)
        {
            if (HasEntry(_platformCells, tilemap, cell)) return;
            _platformCells.Add(new TileEntry { tilemap = tilemap, cell = cell });
        }

        public void AddDecorationCell(Tilemap tilemap, Vector3Int cell)
        {
            if (HasEntry(_decorationCells, tilemap, cell)) return;
            _decorationCells.Add(new TileEntry { tilemap = tilemap, cell = cell });
        }

        public bool RemovePlatformCell(Tilemap tilemap, Vector3Int cell)
        {
            return RemoveEntry(_platformCells, tilemap, cell);
        }

        public bool RemoveDecorationCell(Tilemap tilemap, Vector3Int cell)
        {
            return RemoveEntry(_decorationCells, tilemap, cell);
        }

        public bool HasCell(Tilemap tilemap, Vector3Int cell)
        {
            return HasEntry(_platformCells, tilemap, cell)
                || HasEntry(_decorationCells, tilemap, cell);
        }

        static bool HasEntry(List<TileEntry> list, Tilemap tilemap, Vector3Int cell)
        {
            foreach (var e in list)
                if (e.tilemap == tilemap && e.cell == cell) return true;
            return false;
        }

        static bool RemoveEntry(List<TileEntry> list, Tilemap tilemap, Vector3Int cell)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].tilemap == tilemap && list[i].cell == cell)
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        void OnDrawGizmosSelected()
        {
            DrawCellGizmos(_platformCells, new Color(1f, 0.3f, 0.3f, 0.4f));
            DrawCellGizmos(_decorationCells, new Color(0.3f, 0.6f, 1f, 0.4f));
        }

        void DrawCellGizmos(List<TileEntry> cells, Color color)
        {
            Gizmos.color = color;
            foreach (var entry in cells)
            {
                if (entry.tilemap == null) continue;
                var worldPos = entry.tilemap.GetCellCenterWorld(entry.cell);
                var size = entry.tilemap.cellSize;
                Gizmos.DrawCube(worldPos, new Vector3(size.x, size.y, 0.1f));
            }
        }
    }
}
