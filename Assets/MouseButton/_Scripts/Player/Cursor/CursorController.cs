using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    [SerializeField] GameObject target; // object to seek, default to player
    private SpriteRenderer targetRenderer; // target sprite to initialize on start()
    public float speed = 1f; // speed to seek target
    public int proximityThreshold = 6; // proximity when target reached
    [SerializeField] GameObject cursorVisual; // cursor sprite
    private Vector3 offset = new Vector3(8, 3, 0); // offset positioning relative to target
    private float offsetX = 8; // 8 sits in front of mouse
    public bool flipX = false; // cursor sprite direction

    private void Start()
    {
       targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
    }
    
    private void Update()
    {
        // seek target position
        var targetPosition = target.transform.position;
        // calculate hypotenuse between target
        var distance = Vector3.Distance(transform.position, targetPosition + offset);
        // iterate toward target
        var nextPosition = Vector3.MoveTowards(transform.position, targetPosition + offset, speed * Time.deltaTime * distance);

        // determine direction for cursor to face
        if (distance < proximityThreshold) {
            // within some proximity of target, match target direction
            // if changing direction, nudge the sprite a bit
            if (flipX != targetRenderer.flipX) {
                if (flipX) {
                    nextPosition.x += offsetX / 2; // L to R
                } else {
                    nextPosition.x -= offsetX / 2; // R to L
                }
            }
            flipX = targetRenderer.flipX;
        } else  if (!flipX && nextPosition.x > target.transform.position.x + offset.x) {
            // turn left
            flipX = true;
        } else if (flipX && nextPosition.x < target.transform.position.x + offset.x) {
            // turn right
            flipX = false;
        }

        // iterate toward new horizontal offset
        if (flipX && offset.x > -8) {
            if (offset.x > 0) offset.x = 0;
            offset.x = Mathf.MoveTowards(offset.x, -8, Time.deltaTime * 20);
        } else if (!flipX && offset.x < 8) {
            if (offset.x < 0) offset.x = 0;
            offset.x = Mathf.MoveTowards(offset.x, 8, Time.deltaTime * 20);
        }

        // update cursor position
        transform.position = nextPosition;
        // cursorVisual.transform.localPosition = new Vector3(0, (nextPosition.y - targetPosition.y) - offset.y, 0); // if needed, update cursor visual separately
    }

}