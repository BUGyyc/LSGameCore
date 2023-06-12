using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Text.RegularExpressions;

public class Launch : MonoBehaviour
{
    public virtual void FuncName(int arg1, object arg2, int arg3)
    {        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"arg1: {arg1} arg3: {arg3} ");


 LogMaster.L($"arg1: {arg1} arg3: {arg3} ");

 //LogMaster.L($"arg1: {arg1}  arg3 : { arg3 } ");

 //LogMaster.L($"arg1: \{arg1\}  arg3 : \{ arg3 \} ");

 //LogMaster.L($"arg1: {arg1\}  arg3 : \{ arg3 \} ");

 //LogMaster.L($"{arg1: \{arg1\}  arg3 : \{ arg3 \} }");



        int c = 1;
        int b = arg1 + c;
    }

    public void Test(int a)
    {        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"a: {a} ");


 LogMaster.L($"a: {a} ");

 //LogMaster.L($"a : {a } ");

 //LogMaster.L($"a : \{a \} ");

 //LogMaster.L($"a : \{a \} ");

 //LogMaster.L($"{a : \{a \} }");

        int c = a++;
        int b = a + c;
    }

    public Button createRoomBtn;
    public Button joinRoomBtn;

    public Button pureServerBtn;

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

#if UNITY_EDITOR
        if (APP.QuickDebugSinglePlayer)
        {
            SceneManager.LoadScene("HostClient");
        }
#endif

        pureServerBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("PureServer");
        });
    }
}
