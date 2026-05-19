using UnityEngine;
using UnityEngine.Tilemaps;

namespace MouseButton.World
{
    public class ElectricTileInit : MonoBehaviour
    {
        [SerializeField] float _maxTimeOffset = 3f;

        void Start()
        {
            OffsetParticles();
            CopySpriteFromTilemap();
        }

        void OffsetParticles()
        {
            var offset = Random.Range(0f, _maxTimeOffset);
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                ps.Simulate(offset, false, true);
                ps.Play();
            }
        }

        void CopySpriteFromTilemap()
        {
            var overlay = transform.Find("PulseOverlay");
            if (overlay == null) return;

            var sr = overlay.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            var tilemap = GetComponentInParent<Tilemap>();
            if (tilemap == null) return;

            var cellPos = tilemap.WorldToCell(transform.position);
            var sprite = tilemap.GetSprite(cellPos);
            if (sprite != null)
                sr.sprite = sprite;
        }
    }
}
