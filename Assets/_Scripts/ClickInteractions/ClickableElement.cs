using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickableElement : MonoBehaviour
{
    public enum Interactions
    {
        Drag
    }

    [SerializeField]
    private Interactions interactionType;

    public Transform objectTransform;

    [SerializeField]
    CapsuleCollider2D _thisCollider;

    public float moveSpeed = 5f;

    private bool isBeingClicked = false;
    private Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var isTouchingCursor = _thisCollider.IsTouchingLayers(LayerMask.GetMask("cursor"));
        if (Input.GetKeyDown(KeyCode.M)) {
            if (isTouchingCursor) {
                // Click
                OnClicked();
            }
        } else if (isBeingClicked) {
            if (Input.GetKey(KeyCode.M) && isTouchingCursor) {
                // Click & hold
                WhileClicked();
            } else {
                // No longer held
                OnClickReleased();
            }
        }
    }

    void OnClicked() {
        isBeingClicked = true;
        Debug.Log("clicked!");


        // Calculate the offset between the object and the cursor only once
        Transform cursorTransform = GameManager.Instance.cursorTransform;
        offset = transform.position - cursorTransform.position;
    }

    void WhileClicked() {
        Debug.Log("holding!");
        switch (interactionType)
        {
            case Interactions.Drag:
                // Move the object along with the cursor
                Transform cursorTransform = GameManager.Instance.cursorTransform;
                transform.position = cursorTransform.position + offset;
                break;
            default:
                break;
        }
    }

    void OnClickReleased() {
        isBeingClicked = false;
        Debug.Log("released!");
        offset = Vector3.zero;
    }
}
