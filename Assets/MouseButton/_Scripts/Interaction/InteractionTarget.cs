using UnityEngine;
using UnityEngine.Events;

// Place on any object the player can interact with.
// Wire onInteract to DialoguePrompt.Trigger(), a door, etc.
public class InteractionTarget : MonoBehaviour
{
    [Tooltip("World-space offset from this transform where the cursor icon will rest.")]
    [SerializeField] public Vector2 iconAnchor = Vector2.up;

    [SerializeField] public UnityEvent onInteract;

    public Vector3 IconWorldPosition => transform.position + (Vector3)iconAnchor;

    public void Trigger() => onInteract.Invoke();
}
