using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using UniFramework.Singleton;
using YooAsset;

/// <summary>
/// 更新资源版本号
/// </summary>
internal class FsmUpdateVersion : IStateNode
{
	private StateMachine _machine;

	void IStateNode.OnCreate(StateMachine machine)
	{
		_machine = machine;
	}
	void IStateNode.OnEnter()
	{
		PatchEventDefine.PatchStatesChange.SendEventMessage("获取最新的资源版本 !");
		UniSingleton.StartCoroutine(GetStaticVersion());
	}
	void IStateNode.OnUpdate()
	{
	}
	void IStateNode.OnExit()
	{
	}

	private IEnumerator GetStaticVersion()
	{
		yield return new WaitForSecondsRealtime(0.5f);

		var package = YooAssets.GetPackage("FPS");
		var operation = package.UpdatePackageVersionAsync();
		yield return operation;

		if (operation.Status == EOperationStatus.Succeed)
		{
			Debug.Log("[FsmUpdateVersion] succeed");

			//! 写入 Remote 的版本
			PatchManager.Instance.PackageVersion = operation.PackageVersion;
			_machine.ChangeState<FsmUpdateManifest>();
		}
		else
		{
			Debug.LogWarning(operation.Error);
			PatchEventDefine.PackageVersionUpdateFailed.SendEventMessage();
		}
	}
}