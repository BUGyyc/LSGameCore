using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Lockstep.Logging;
using Lockstep.Network;
using Lockstep.Util;
using Lockstep.FakeServer.Server;

public class HostClient : MonoBehaviour
{
    Lockstep.FakeServer.Server.Server server;

    public MainScript script;

    public GameObject clientCoreObj;
    OneThreadSynchronizationContext contex;

    // Start is called before the first frame update
    void Start()
    {
        contex = new OneThreadSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(contex);
        //Debug.Log("Main start");
        //Utils.StartServices();
        server = new Lockstep.FakeServer.Server.Server();
        server.Start();

        StartCoroutine(DelayStartClient());
    }

    IEnumerator DelayStartClient()
    {
        yield return new UnityEngine.WaitForSeconds(1);
        clientCoreObj.SetActive(true);
        yield return null;
    }

    // Update is called once per frame
    void Update()
    {
        contex.Update();
        server.Update();
    }
}
