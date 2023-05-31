using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Lockstep.Logging;
using Lockstep.Network;
using Lockstep.Util;
using Lockstep.FakeServer.Server;


public class PureServer : MonoBehaviour
{
    Server server;

    // public MainScript script;

    // public GameObject clientCoreObj;
    OneThreadSynchronizationContext contex;
    // Start is called before the first frame update
    void Start()
    {
        contex = new OneThreadSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(contex);
        //Debug.Log("Main start");
        //Utils.StartServices();
        server = new Server();
        server.Start();
    }


    int count = 0;
    // Update is called once per frame
    void Update()
    {
        contex.Update();
        server.Update();

        // if (count == 30)
        // {

           
        // }

        // if (count == 60)
        // {
        //     clientCoreObj.SetActive(true);

        //     //script?.SendConnectServer();
        // }
        // count++;
    }
}
