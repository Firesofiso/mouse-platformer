using UnityEngine;

public class PlayerObject : MonoBehaviour
{
    // The static instance of the singleton
    private static PlayerObject instance;

    // Public property to access the instance
    public static PlayerObject Instance
    {
        get
        {
            // If the instance is null, try to find it in the scene
            if (instance == null)
            {
                // // instance = FindObjectOfType<PlayerObject>();

                // // If it's still null, create a new GameObject and add the script to it
                // if (instance == null)
                // {
                //     GameObject singletonObject = new GameObject(typeof(PlayerObject).Name);
                //     instance = singletonObject.AddComponent<PlayerObject>();
                // }
            }
            return instance;
        }
    }

    // Optional: Add your singleton's functionality here
    public void SomeFunction()
    {
        // Implement your functionality here
    }

    // Ensure the singleton instance isn't destroyed when loading new scenes
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
