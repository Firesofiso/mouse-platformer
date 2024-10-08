using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Tarodev;
using UnityEngine;

namespace Tarodev.Trol {
    internal class TrolSpear : MonoBehaviour {
        [SerializeField]
        PolygonCollider2D _spearTip;

        [SerializeField]
        CapsuleCollider2D _spearShaft;

        [SerializeField]
        CapsuleCollider2D _spearDetection;

        [SerializeField]
        internal Rigidbody2D _rb;

        // [SerializeField] Rigidbody2D _rb;
        public Sprite spear;
        public Sprite spear45deg;
        public GameObject visual;
        public new SpriteRenderer renderer;
        private Collider2D anchorCollider = null;
        private List<Collider2D> currentlyIgnored = new List<Collider2D>(); // keep track of what's being ignored

        public IEnumerator TemporarilyIgnoreColliders(List<Collider2D> other, bool ignore = true)
        // when spear is instantiated, ignore the thrower's colliders
        // recursively disable after some time
        {
            foreach (Collider2D c in other) {
                Physics2D.IgnoreCollision(_spearTip, c, ignore);
                Physics2D.IgnoreCollision(_spearShaft, c, ignore);
            }
            yield return new WaitForSeconds(0.25f);
            if (ignore && isActiveAndEnabled) {
                currentlyIgnored.Concat(other); // add to currently ignored
                StartCoroutine(TemporarilyIgnoreColliders(other, false));
            }
        }

        void OnEnable() {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            disableCollisions(false);
        }

        void OnDisable() {
            if (currentlyIgnored.Count > 0) {
                TemporarilyIgnoreColliders(currentlyIgnored, false); // when spear is picked up, make sure we re-enable all collisions
            }
        }

        void OnDestroy() {
            GameManager.instance.trolManager.activeSpears.Remove(this);
        }

        // Update is called once per frame
        void Update() {
            if (_rb.bodyType == RigidbodyType2D.Dynamic && Mathf.Abs(_rb.velocity.y) > 15)
                transform.up = Vector2.MoveTowards(transform.up, _rb.velocity, Time.deltaTime);

            // if (Mathf.Abs(transform.rotation.z % 90) > 22.5 && Mathf.Abs(transform.rotation.z % 90) < 67.5 && renderer.sprite == spear) {
            //     renderer.sprite = spear45deg;
            //     visual.transform.Translate(0, -0.5f, 0);
            //     visual.transform.Rotate(0,0,45);
            // } else if (renderer.sprite != spear) {
            //     renderer.sprite = spear;
            //     visual.transform.Translate(0, 0.5f, 0);
            //     visual.transform.Rotate(0,0,-45);
            // }
            // if (_spearTip)
            // _rb.velocity = new Vector2 (_rb.velocity.x, Mathf.MoveTowards(_rb.velocity.y, -60, 110 * Time.fixedDeltaTime));
        }

        //todo spear velocity vector falls below some threshold : disable collisions

        private void OnCollisionEnter2D(Collision2D coll) {
            if (coll.otherCollider == _spearTip) {
                if (
                    coll.gameObject.layer.IsInLayerMask(
                        LayerMask.GetMask("Ground", "one-way", "climbable")
                    )
                ) {
                    _rb.bodyType = RigidbodyType2D.Static;
                    transform.SetParent(coll.transform);
                    transform.position = new Vector3(
                        Mathf.RoundToInt(transform.position.x),
                        Mathf.RoundToInt(transform.position.y),
                        0
                    );
                    disableCollisions(true);
                    anchorCollider = coll.collider;
                } else if (coll.gameObject.layer.IsInLayerMask(LayerMask.GetMask("Entities"))) {
                    Debug.Log("hit!");
                }
            }
        }

        private void disableCollisions(bool _disabled) {
            _spearShaft.enabled = !_disabled;
            _spearTip.enabled = !_disabled;
        }

        private void OnCollisionExit2D(Collision2D other) {
            // todo custom function if spear should be released from parent object
            // if (!_spearTip.IsTouching(anchorCollider))
            // {
            //     _rb.bodyType = RigidbodyType2D.Dynamic;
            //     Physics2D.IgnoreLayerCollision(0, 32, false);
            //     transform.SetParent(null);
            //     anchorCollider = null;
            // }
        }
    }
}