
- [DEV\_LIST](#dev_list)
- [Framework Pipeline](#framework-pipeline)
- [References](#references)



---

# DEV_LIST

- [X] [HashCode 验证](/Doc/HashCode%20%E9%AA%8C%E8%AF%81.md)
- [X] 弱网络测试

- [ ] Sync 流程图
- [ ] UDP、TCP 环境验证
- [ ] CodeGenerate 生成
- [ ] AssetBundle
- [ ] HybridCLR


# Framework Pipeline

- 三种模式：
  - Client 模式
  - Host Client 模式
  - Pure Server 模式
- 两个工程：
  - Client 工程
  - Client 工程 + 内嵌战斗服代码（战斗服只是一个网络线程，只做广播协议）
- 工程启动流程：
  - 打开 Launch 场景
    - Host Client 模式：作为房主，点击CreateRoom，创建房间，会得到随机端口号，也可以自主设定端口号；
    - Client 模式：点击 JoinRoom，输入目标端口，进入房间





---
# References


<https://github.com/JiepengTan/LockstepEngine>
<https://github.com/JiepengTan/LockstepEngine_ARPGDemo>
<https://www.bilibili.com/video/av70422751/>
<https://github.com/JiepengTan/LockstepMath>
<https://github.com/JiepengTan/LockstepCollision>
<https://github.com/JiepengTan/LockstepBehaviorTree>
<https://github.com/JiepengTan/LockstepPathFinding>




