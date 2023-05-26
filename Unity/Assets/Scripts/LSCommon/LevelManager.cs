using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public string levelName;
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadScene(levelName, LoadSceneMode.Additive);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
