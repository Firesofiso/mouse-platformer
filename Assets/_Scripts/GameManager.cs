using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform playerTransform;
    public Transform cursorTransform;

    // Singleton pattern to access the GameManager from other scripts
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // Implement any other game-related functionalities and interactions
}