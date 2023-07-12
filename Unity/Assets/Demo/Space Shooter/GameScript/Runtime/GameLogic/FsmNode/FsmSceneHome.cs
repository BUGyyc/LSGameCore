using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using UniFramework.Window;
using UniFramework.Singleton;
using YooAsset;

internal class FsmSceneHome : IStateNode
{
	private StateMachine _machine;

	void IStateNode.OnCreate(StateMachine machine)
	{
		_machine = machine;
	}
	void IStateNode.OnEnter()
	{
		UniSingleton.StartCoroutine(Prepare());
	}
	void IStateNode.OnUpdate()
	{
	}
	void IStateNode.OnExit()
	{
		UniWindow.CloseWindow<UIHomeWindow>();
	}

	private IEnumerator Prepare()
	{
		//note 同步加载
		yield return YooAssets.LoadSceneAsync("Lobby");	

		Debug.Log("Open HomeScene");

		// yield return UniWindow.OpenWindowAsync<UIHomeWindow>("UIHome");

		// 释放资源
		var package = YooAssets.GetPackage("FPS");
		//！清理未使用的资源
		package.UnloadUnusedAssets();
	}
}