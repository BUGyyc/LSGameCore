using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
	internal sealed class BundledAssetProvider : ProviderBase
	{
		private AssetBundleRequest _cacheRequest;

		public BundledAssetProvider(AssetSystemImpl impl, string providerGUID, AssetInfo assetInfo) : base(impl, providerGUID, assetInfo)
		{
		}
		public override void Update()
		{
			DebugBeginRecording();

			if (IsDone)
				return;

			if (Status == EStatus.None)
			{
				Status = EStatus.CheckBundle;
			}

			// 1. 检测资源包
			if (Status == EStatus.CheckBundle)
			{
				if (IsWaitForAsyncComplete)
				{
					DependBundleGroup.WaitForAsyncComplete();
					OwnerBundleLoader.WaitForAsyncComplete();
				}

				if (DependBundleGroup.IsDone() == false)
					return;
				if (OwnerBundleLoader.IsDone() == false)
					return;

				if (DependBundleGroup.IsSucceed() == false)
				{
					//！依赖包加载失败
					Status = EStatus.Failed;
					LastError = DependBundleGroup.GetLastError();
					InvokeCompletion();
					return;
				}

				if (OwnerBundleLoader.Status != BundleLoaderBase.EStatus.Succeed)
				{
					//！指定资源加载失败
					Status = EStatus.Failed;
					LastError = OwnerBundleLoader.LastError;
					InvokeCompletion();
					return;
				}

				if (OwnerBundleLoader.CacheBundle == null)
				{
					if (OwnerBundleLoader.IsDestroyed)
						throw new System.Exception("Should never get here !");
					Status = EStatus.Failed;
					LastError = $"The bundle {OwnerBundleLoader.MainBundleInfo.Bundle.BundleName} has been destroyed by unity bugs !";
					YooLogger.Error(LastError);
					InvokeCompletion();
					return;
				}

				Status = EStatus.Loading;
			}

			// 2. 加载资源对象
			if (Status == EStatus.Loading)
			{
				if (IsWaitForAsyncComplete)
				{
					if (MainAssetInfo.AssetType == null)
						AssetObject = OwnerBundleLoader.CacheBundle.LoadAsset(MainAssetInfo.AssetPath);
					else
						AssetObject = OwnerBundleLoader.CacheBundle.LoadAsset(MainAssetInfo.AssetPath, MainAssetInfo.AssetType);
				}
				else
				{
					if (MainAssetInfo.AssetType == null)
						_cacheRequest = OwnerBundleLoader.CacheBundle.LoadAssetAsync(MainAssetInfo.AssetPath);
					else
						_cacheRequest = OwnerBundleLoader.CacheBundle.LoadAssetAsync(MainAssetInfo.AssetPath, MainAssetInfo.AssetType);
				}
				Status = EStatus.Checking;
			}

			// 3. 检测加载结果
			if (Status == EStatus.Checking)
			{
				if (_cacheRequest != null)
				{
					if (IsWaitForAsyncComplete)
					{
						// 强制挂起主线程（注意：该操作会很耗时）
						YooLogger.Warning("Suspend the main thread to load unity asset.");
						AssetObject = _cacheRequest.asset;
					}
					else
					{
						Progress = _cacheRequest.progress;
						if (_cacheRequest.isDone == false)
							return;
						AssetObject = _cacheRequest.asset;
					}
				}

				Status = AssetObject == null ? EStatus.Failed : EStatus.Succeed;
				if (Status == EStatus.Failed)
				{
					if (MainAssetInfo.AssetType == null)
						LastError = $"Failed to load asset : {MainAssetInfo.AssetPath} AssetType : null AssetBundle : {OwnerBundleLoader.MainBundleInfo.Bundle.BundleName}";
					else
						LastError = $"Failed to load asset : {MainAssetInfo.AssetPath} AssetType : {MainAssetInfo.AssetType} AssetBundle : {OwnerBundleLoader.MainBundleInfo.Bundle.BundleName}";
					YooLogger.Error(LastError);
				}
				InvokeCompletion();
			}
		}
	}
}