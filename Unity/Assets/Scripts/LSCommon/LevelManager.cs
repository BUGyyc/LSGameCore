using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniFramework.Singleton;
using YooAsset;

public class LevelManager : MonoBehaviour
{
    public string levelName;

    // Start is called before the first frame update
    void Start()
    {
        // SceneManager.LoadScene(levelName, LoadSceneMode.Additive);

        UniSingleton.StartCoroutine(LoadScene(levelName, LoadSceneMode.Additive));
    }

    // Update is called once per frame
    void Update() { }

    private IEnumerator LoadScene(string sceneName, LoadSceneMode mode)
    {
        yield return YooAssets.LoadSceneAsync(sceneName, mode);

        LogMaster.I("Load Scene  name : " + sceneName);
    }
}
