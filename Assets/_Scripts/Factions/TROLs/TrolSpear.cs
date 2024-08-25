using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrolSpear : MonoBehaviour
{
    [SerializeField] PolygonCollider2D _spearTip;
    [SerializeField] CapsuleCollider2D _spearShaft;
    [SerializeField] CapsuleCollider2D _spearDetection;
    [SerializeField] Rigidbody2D _rb;
    // [SerializeField] Rigidbody2D _rb;
    public Sprite spear;
    public Sprite spear45deg;
    public GameObject visual;
    public SpriteRenderer renderer;
    private Collider2D anchorCollider = null;

    public IEnumerator TemporarilyIgnoreColliders(Collider2D[] other, bool ignore = true)
    // when spear is instantiated, ignore the thrower's colliders
    // recursively disable after some time
    {
        foreach (Collider2D c in other)
        {
            Debug.Log(c);
            Physics2D.IgnoreCollision(_spearTip, c, ignore);
            Physics2D.IgnoreCollision(_spearShaft, c, ignore); 
        }
        if (!ignore)
        {
            yield return new WaitForSeconds(0.25f);
            StartCoroutine(TemporarilyIgnoreColliders(other, false));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_rb.bodyType == RigidbodyType2D.Dynamic && Mathf.Abs(_rb.velocity.y) > 15) transform.up = Vector2.MoveTowards(transform.up, _rb.velocity, Time.deltaTime);

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

    //todo spear tip collides with ground layer & spear velocity vector is within some threshold of inverse contact normal : stick into ground
    //todo spear velocity vector falls below some threshold : disable collisions

    private void OnCollisionEnter2D(Collision2D other) 
    { 
        if (_spearTip.IsTouchingLayers(LayerMask.GetMask("Ground", "one-way", "climbable")))
        { 
            _rb.bodyType = RigidbodyType2D.Static;
            transform.SetParent(other.transform);
            transform.position = new Vector3(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), 0);
            disableCollisions(true);
            anchorCollider = other.collider;
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
