using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneLoader : MonoBehaviour
{
    [SerializeField] string _sceneToLoad;
    [SerializeField] string _sceneToUnload;
    [SerializeField] bool _triggerOnPlayerEnter = true;

    private bool _loaded;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (_triggerOnPlayerEnter && !col.CompareTag("Player")) return;
        if (!_loaded) StartCoroutine(Load());
        if (!string.IsNullOrEmpty(_sceneToUnload)) StartCoroutine(Unload(_sceneToUnload));
    }

    private IEnumerator Load()
    {
        _loaded = true;
        var op = SceneManager.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Additive);
        yield return op;
    }

    private IEnumerator Unload(string scene)
    {
        if (SceneManager.GetSceneByName(scene).isLoaded)
            yield return SceneManager.UnloadSceneAsync(scene);
    }
}
