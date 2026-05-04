using UnityEngine;

namespace TarodevController
{
    public class MouseController : UnitController
    {
        public bool IsInCharacterSelect { get; set; }

        protected override void Update()
        {
            if (IsInCharacterSelect) return;
            base.Update();
        }

        protected override void FixedUpdate()
        {
            if (IsInCharacterSelect) return;
            base.FixedUpdate();
        }

        // Drop-down: only trigger when actually standing on a one-way platform
        protected override void HandleDropDownInput()
        {
            if (IsGroundedOnOneWay())
                _droppingDown = true;
        }

        // Pass through one-way platforms when dropping down
        protected override void HandleDropDown()
        {
            if (!_droppingDown) return;
            foreach (RaycastHit2D surface in _groundHits)
            {
                var platform = surface.collider?.GetComponent<OneWayPlatformBehaviour>();
                platform?.AllowObjectPassThrough(_col);
            }
            _droppingDown = false;
        }

        // Solid ceilings only — one-way platforms overhead don't kill upward velocity
        protected override bool IsCeilingHitSolid()
        {
            for (int i = 0; i < _ceilingHits.Length; i++)
            {
                var hit = _ceilingHits[i];
                if (hit.collider != null && hit.collider.GetComponent<OneWayPlatformBehaviour>() == null)
                    return true;
            }
            return false;
        }

        private bool IsGroundedOnOneWay()
        {
            for (int i = 0; i < _groundHitCount; i++)
                if (_groundHits[i].collider?.GetComponent<OneWayPlatformBehaviour>() != null)
                    return true;
            return false;
        }
    }
}
