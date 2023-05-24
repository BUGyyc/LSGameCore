using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Launch : MonoBehaviour
{
    public Button createRoomBtn;
    public Button joinRoomBtn;
    // Start is called before the first frame update
    void Start()
    {
        createRoomBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("HostClient");
        });

        joinRoomBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Client");
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
