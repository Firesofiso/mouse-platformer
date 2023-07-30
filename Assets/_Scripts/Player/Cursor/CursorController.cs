using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    public float speed = 1f;
    [SerializeField] GameObject target;
    [SerializeField] Vector3 offset;
    [SerializeField] SpriteRenderer _playerRenderer; 
    public bool flipX = false;

    private void Start()
    {
       
    }
    
    private void Update()
    {
        var distance = Vector3.Distance(transform.position, target.transform.position);
        offset.x = flipX ? -0.4f : 0.4f;
        var newPos = Vector3.MoveTowards(transform.position, target.transform.position + offset, speed * Time.deltaTime * distance);
        newPos.z = -1;
        if (newPos.x > target.transform.position.x + offset.x) {
            flipX = true;
        } else if (newPos.x < target.transform.position.x + offset.x) {
            flipX = false;
        }
        transform.position = newPos;
        if (distance < 1.12) flipX = _playerRenderer.flipX;
    }

}
