using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 初始化操作
    /// </summary>
    public abstract class InitializationOperation : AsyncOperationBase
    {
        public string PackageVersion { protected set; get; }
    }

    /// <summary>
    /// 编辑器下模拟模式的初始化操作
    /// </summary>
    internal sealed class EditorSimulateModeInitializationOperation : InitializationOperation
    {
        private enum ESteps
        {
            None,
            LoadEditorManifest,
            Done,
        }

        private readonly EditorSimulateModeImpl _impl;
        private readonly string _simulateManifestFilePath;
        private LoadEditorManifestOperation _loadEditorManifestOp;
        private ESteps _steps = ESteps.None;

        internal EditorSimulateModeInitializationOperation(EditorSimulateModeImpl impl, string simulateManifestFilePath)
        {
            _impl = impl;
            _simulateManifestFilePath = simulateManifestFilePath;
        }
        internal override void Start()
        {
            _steps = ESteps.LoadEditorManifest;
        }
        internal override void Update()
        {
            if (_steps == ESteps.LoadEditorManifest)
            {
                if (_loadEditorManifestOp == null)
                {
                    _loadEditorManifestOp = new LoadEditorManifestOperation(_simulateManifestFilePath);
                    OperationSystem.StartOperation(_loadEditorManifestOp);
                }

                if (_loadEditorManifestOp.IsDone == false)
                    return;

                if (_loadEditorManifestOp.Status == EOperationStatus.Succeed)
                {
                    PackageVersion = _loadEditorManifestOp.Manifest.PackageVersion;
                    _impl.ActiveManifest = _loadEditorManifestOp.Manifest;
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadEditorManifestOp.Error;
                }
            }
        }
    }

    /// <summary>
    /// 离线运行模式的初始化操作
    /// </summary>
    internal sealed class OfflinePlayModeInitializationOperation : InitializationOperation
    {
        private enum ESteps
        {
            None,
            QueryBuildinPackageVersion,
            LoadBuildinManifest,
            PackageCaching,
            Done,
        }

        private readonly OfflinePlayModeImpl _impl;
        private readonly string _packageName;
        private QueryBuildinPackageVersionOperation _queryBuildinPackageVersionOp;
        private LoadBuildinManifestOperation _loadBuildinManifestOp;
        private PackageCachingOperation _cachingOperation;
        private ESteps _steps = ESteps.None;

        internal OfflinePlayModeInitializationOperation(OfflinePlayModeImpl impl, string packageName)
        {
            _impl = impl;
            _packageName = packageName;
        }
        internal override void Start()
        {
            _steps = ESteps.QueryBuildinPackageVersion;
        }
        internal override void Update()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.QueryBuildinPackageVersion)
            {
                if (_queryBuildinPackageVersionOp == null)
                {
                    _queryBuildinPackageVersionOp = new QueryBuildinPackageVersionOperation(_packageName);
                    OperationSystem.StartOperation(_queryBuildinPackageVersionOp);
                }

                if (_queryBuildinPackageVersionOp.IsDone == false)
                    return;

                if (_queryBuildinPackageVersionOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.LoadBuildinManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _queryBuildinPackageVersionOp.Error;
                }
            }

            if (_steps == ESteps.LoadBuildinManifest)
            {
                if (_loadBuildinManifestOp == null)
                {
                    _loadBuildinManifestOp = new LoadBuildinManifestOperation(_packageName, _queryBuildinPackageVersionOp.PackageVersion);
                    OperationSystem.StartOperation(_loadBuildinManifestOp);
                }

                Progress = _loadBuildinManifestOp.Progress;
                if (_loadBuildinManifestOp.IsDone == false)
                    return;

                if (_loadBuildinManifestOp.Status == EOperationStatus.Succeed)
                {
                    PackageVersion = _loadBuildinManifestOp.Manifest.PackageVersion;
                    _impl.ActiveManifest = _loadBuildinManifestOp.Manifest;
                    _steps = ESteps.PackageCaching;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadBuildinManifestOp.Error;
                }
            }

            if (_steps == ESteps.PackageCaching)
            {
                if (_cachingOperation == null)
                {
                    _cachingOperation = new PackageCachingOperation(_packageName);
                    OperationSystem.StartOperation(_cachingOperation);
                }

                Progress = _cachingOperation.Progress;
                if (_cachingOperation.IsDone)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
            }
        }
    }

    /// <summary>
    /// 联机运行模式的初始化操作
    /// 注意：优先从沙盒里加载清单，如果沙盒里不存在就尝试把内置清单拷贝到沙盒并加载该清单。
    /// </summary>
    internal sealed class HostPlayModeInitializationOperation : InitializationOperation
    {
        private enum ESteps
        {
            None,
            /// <summary>
            /// ? 检测版本
            /// </summary>
            CheckAppFootPrint,
            /// <summary>
            /// ? 查询本地的缓冲是否需要更新
            /// </summary>
            QueryCachePackageVersion,
            /// <summary>
            /// 加载本地的 Manifest 清单文件
            /// </summary>
            TryLoadCacheManifest,
            /// <summary>
            /// ？查询AB版本
            /// </summary>
            QueryBuildinPackageVersion,
            /// <summary>
            /// ？解压 清单文件
            /// </summary>
            UnpackBuildinManifest,
            LoadBuildinManifest,
            PackageCaching,
            Done,
        }

        private readonly HostPlayModeImpl _impl;
        private readonly string _packageName;
        private QueryBuildinPackageVersionOperation _queryBuildinPackageVersionOp;
        private QueryCachePackageVersionOperation _queryCachePackageVersionOp;
        private UnpackBuildinManifestOperation _unpackBuildinManifestOp;
        private LoadBuildinManifestOperation _loadBuildinManifestOp;
        private LoadCacheManifestOperation _loadCacheManifestOp;
        private PackageCachingOperation _cachingOperation;
        private ESteps _steps = ESteps.None;

        internal HostPlayModeInitializationOperation(HostPlayModeImpl impl, string packageName)
        {
            LogMaster.Log("[HostMode]"," HostPlayModeInitializationOperation init Op packageName: " + packageName);
            _impl = impl;
            _packageName = packageName;
        }
        internal override void Start()
        {
            _steps = ESteps.CheckAppFootPrint;
        }
        internal override void Update()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckAppFootPrint)
            {
                var appFootPrint = new AppFootPrint();
                appFootPrint.Load();

                bool debugFootPrint = false;

                //! footPrint 用来对比版本，这里起始应该判断大小，防止一些异常情况
                // 如果水印发生变化，则说明覆盖安装后首次打开游戏
                if (appFootPrint.IsDirty() || debugFootPrint)
                {
                    //！删除旧的 Manifest 
                    PersistentTools.DeleteManifestFolder();
                    //！ 覆盖版本
                    appFootPrint.Coverage();
                    LogMaster.S("[CheckAppFootPrint]","删除本地清单文件，并覆盖 FootPrint,   Delete manifest files when application foot print dirty !");
                }else{
                    LogMaster.S("[CheckAppFootPrint]","版本一致，不用变更");
                }
                _steps = ESteps.QueryCachePackageVersion;
            }

            if (_steps == ESteps.QueryCachePackageVersion)
            {
                if (_queryCachePackageVersionOp == null)
                {
                    LogMaster.S("[QueryCachePackage]","查询 Package版本  QueryCachePackageVersion query");
                    /// <summary>
                    /// 查询AB Package 版本
                    /// </summary>
                    /// <returns></returns>
                    _queryCachePackageVersionOp = new QueryCachePackageVersionOperation(_packageName);
                    OperationSystem.StartOperation(_queryCachePackageVersionOp);
                }

                if (_queryCachePackageVersionOp.IsDone == false)
                    return;

                if (_queryCachePackageVersionOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.TryLoadCacheManifest;
                }
                else
                {
                    _steps = ESteps.QueryBuildinPackageVersion;
                }
            }

            if (_steps == ESteps.TryLoadCacheManifest)
            {
                if (_loadCacheManifestOp == null)
                {
                    LogMaster.S("[LoadCacheManifest]"," load cache manifest");
                    _loadCacheManifestOp = new LoadCacheManifestOperation(_packageName, _queryCachePackageVersionOp.PackageVersion);
                    OperationSystem.StartOperation(_loadCacheManifestOp);
                }

                if (_loadCacheManifestOp.IsDone == false)
                    return;

                if (_loadCacheManifestOp.Status == EOperationStatus.Succeed)
                {

                    LogMaster.S("[LoadCacheManifest]"," 加载缓存 清单文件 成功 manifest");

                    PackageVersion = _loadCacheManifestOp.Manifest.PackageVersion;
                    _impl.ActiveManifest = _loadCacheManifestOp.Manifest;
                    _steps = ESteps.PackageCaching;
                }
                else
                {
                    _steps = ESteps.QueryBuildinPackageVersion;
                }
            }

            if (_steps == ESteps.QueryBuildinPackageVersion)
            {
                if (_queryBuildinPackageVersionOp == null)
                {
                    LogMaster.S("[QueryBuildIn]"," query build in");
                    _queryBuildinPackageVersionOp = new QueryBuildinPackageVersionOperation(_packageName);
                    OperationSystem.StartOperation(_queryBuildinPackageVersionOp);
                }

                if (_queryBuildinPackageVersionOp.IsDone == false)
                    return;

                if (_queryBuildinPackageVersionOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.UnpackBuildinManifest;
                }
                else
                {
                    // 注意：为了兼容MOD模式，初始化动态新增的包裹的时候，如果内置清单不存在也不需要报错！
                    _steps = ESteps.PackageCaching;
                    string error = _queryBuildinPackageVersionOp.Error;
                    LogMaster.Log($"Failed to load buildin package version file : {error}");
                }
            }

            if (_steps == ESteps.UnpackBuildinManifest)
            {
                if (_unpackBuildinManifestOp == null)
                {
                    LogMaster.S("[UnpackBuildInManifest]"," unpack buildin manifest");
                    _unpackBuildinManifestOp = new UnpackBuildinManifestOperation(_packageName, _queryBuildinPackageVersionOp.PackageVersion);
                    OperationSystem.StartOperation(_unpackBuildinManifestOp);
                }

                if (_unpackBuildinManifestOp.IsDone == false)
                    return;

                if (_unpackBuildinManifestOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.LoadBuildinManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _unpackBuildinManifestOp.Error;
                }
            }

            if (_steps == ESteps.LoadBuildinManifest)
            {
                if (_loadBuildinManifestOp == null)
                {
                    LogMaster.S("[LoadBuildInManifest]"," load build in manifest");
                    _loadBuildinManifestOp = new LoadBuildinManifestOperation(_packageName, _queryBuildinPackageVersionOp.PackageVersion);
                    OperationSystem.StartOperation(_loadBuildinManifestOp);
                }

                Progress = _loadBuildinManifestOp.Progress;
                if (_loadBuildinManifestOp.IsDone == false)
                    return;

                if (_loadBuildinManifestOp.Status == EOperationStatus.Succeed)
                {
                    PackageVersion = _loadBuildinManifestOp.Manifest.PackageVersion;
                    _impl.ActiveManifest = _loadBuildinManifestOp.Manifest;
                    _steps = ESteps.PackageCaching;


                    // PackageRuntimeData.packageVersion = PackageVersion;

                    LogMaster.Log("[LoadBuildinManifest]"," [PackageRuntimeData] 设定版本号，  succeed version:" + PackageVersion);
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadBuildinManifestOp.Error;
                }
            }

            if (_steps == ESteps.PackageCaching)
            {
                if (_cachingOperation == null)
                {
                    LogMaster.S("[PackageCache] ", " PackageCaching  cache _packageName:"+ _packageName);
                    _cachingOperation = new PackageCachingOperation(_packageName);
                    OperationSystem.StartOperation(_cachingOperation);
                }

                Progress = _cachingOperation.Progress;
                if (_cachingOperation.IsDone)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
            }
        }
    }

    /// <summary>
    /// 应用程序水印
    /// ！存储关键的版本
    /// </summary>
    internal class AppFootPrint
    {
        private string _footPrint;

        /// <summary>
        /// 读取应用程序水印
        /// </summary>
        public void Load()
        {
            string footPrintFilePath = PersistentTools.GetAppFootPrintFilePath();
            if (File.Exists(footPrintFilePath))
            {
                //！取下本地的
                _footPrint = FileUtility.ReadAllText(footPrintFilePath);
                LogMaster.Log("[AppFootPrint] ","local cache value:" + _footPrint + "  from: " + footPrintFilePath);
            }
            else
            {
                //! 用CDN的数据写到本地
                Coverage();
            }
        }

        /// <summary>
        /// 检测水印是否发生变化
        /// </summary>
        public bool IsDirty()
        {
#if UNITY_EDITOR
            LogMaster.S("[FootPrint]    ",$" check IsDirty  ,  Compare local.footPrint:{_footPrint}  version:{Application.version} ");
            return _footPrint != Application.version;
#else
			return _footPrint != Application.buildGUID;
#endif
        }

        /// <summary>
        /// 覆盖掉水印
        /// </summary>
        public void Coverage()
        {
#if UNITY_EDITOR
            _footPrint = Application.version;
#else
			_footPrint = Application.buildGUID;
#endif
            string footPrintFilePath = PersistentTools.GetAppFootPrintFilePath();
            FileUtility.WriteAllText(footPrintFilePath, _footPrint);
            LogMaster.Log("[FootPrint]",$" 本地保存 版本号  Save application foot print : {_footPrint}   path：{footPrintFilePath}");
        }
    }
}