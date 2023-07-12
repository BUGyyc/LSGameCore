using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Window;
using UniFramework.Event;
using UniFramework.Machine;
using UniFramework.Singleton;
using YooAsset;

internal class FsmSceneBattle : IStateNode
{
	private BattleRoom _battleRoom;

	void IStateNode.OnCreate(StateMachine machine)
	{	
	}
	void IStateNode.OnEnter()
	{
		UniSingleton.StartCoroutine(Prepare());
	}
	void IStateNode.OnUpdate()
	{
		if(_battleRoom != null)
			_battleRoom.UpdateRoom();
	}
	void IStateNode.OnExit()
	{
		if(_battleRoom != null)
		{
			_battleRoom.DestroyRoom();
			_battleRoom = null;
		}
	}

	private IEnumerator Prepare()
	{
		yield return YooAssets.LoadSceneAsync("scene_battle");

		Debug.Log("Open BattleScene");
		
		//！ 启动战斗场景
		_battleRoom = new BattleRoom();
		yield return _battleRoom.LoadRoom();

		//！ 这里的策略需要慎重一点
		// 释放资源
		var package = YooAssets.GetPackage("FPS");
		package.UnloadUnusedAssets();
	}
}