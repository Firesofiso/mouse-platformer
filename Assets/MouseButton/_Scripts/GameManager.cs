using Tarodev.Trol;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public Transform playerTransform;
    public Transform cursorTransform;
    public TrolManager trolManager;

    // Singleton pattern to access the GameManager from other scripts
    public static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    private void Awake() {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // Implement any other game-related functionalities and interactions
}
