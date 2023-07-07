
namespace YooAsset
{
	internal class BundledRawFileProvider : ProviderBase
	{
		public BundledRawFileProvider(AssetSystemImpl impl, string providerGUID, AssetInfo assetInfo) : base(impl, providerGUID, assetInfo)
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
					OwnerBundleLoader.WaitForAsyncComplete();
				}

				if (OwnerBundleLoader.IsDone() == false)
					return;

				if (OwnerBundleLoader.Status != BundleLoaderBase.EStatus.Succeed)
				{
					Status = EStatus.Failed;
					LastError = OwnerBundleLoader.LastError;
					InvokeCompletion();
					return;
				}

				Status = EStatus.Checking;
			}

			// 2. 检测加载结果
			if (Status == EStatus.Checking)
			{
				RawFilePath = OwnerBundleLoader.FileLoadPath;
				Status = EStatus.Succeed;
				InvokeCompletion();
			}
		}
	}
}