using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Text.RegularExpressions;
using YooAsset;
using UniFramework.Singleton;

public class Launch : MonoBehaviour
{
    public Button createRoomBtn;
    public Button joinRoomBtn;

    public Button pureServerBtn;

    // Start is called before the first frame update
    void Start()
    {
        createRoomBtn.onClick.AddListener(() =>
        {
            // SceneManager.LoadScene("HostClient");
            UniSingleton.StartCoroutine(LoadScene("HostClient"));
        });

        joinRoomBtn.onClick.AddListener(() =>
        {
            // SceneManager.LoadScene("Client");
            UniSingleton.StartCoroutine(LoadScene("Client"));
        });

#if UNITY_EDITOR
        if (APP.QuickDebugSinglePlayer)
        {
            // SceneManager.LoadScene("HostClient");
            UniSingleton.StartCoroutine(LoadScene("HostClient"));
        }
#endif

        pureServerBtn.onClick.AddListener(() =>
        {
            // SceneManager.LoadScene("PureServer");
            UniSingleton.StartCoroutine(LoadScene("PureServer"));
        });
    }

    private IEnumerator LoadScene(string sceneName)
    {
        yield return YooAssets.LoadSceneAsync(sceneName);

        LogMaster.I("Load Scene  name : " + sceneName);
    }
}
