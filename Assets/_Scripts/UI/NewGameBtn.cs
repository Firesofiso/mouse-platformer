using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class newGameBtn : MonoBehaviour
{
    [SerializeField] private string nextScene;

    public void NextScene()
    {
        SceneManager.LoadScene(nextScene);
    }
}
