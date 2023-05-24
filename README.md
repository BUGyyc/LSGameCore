
- [DEV\_LIST](#dev_list)
- [Framework Pipeline](#framework-pipeline)
- [Lockstep Tutorial](#lockstep-tutorial)
    - [前言](#前言)
    - [教程大纲](#教程大纲)
      - [阶段零: 帧同步概要](#阶段零-帧同步概要)
      - [阶段一: 基础帧同步(视频重置中...)](#阶段一-基础帧同步视频重置中)
      - [阶段二：预测\&回滚式 (视频重置中...)](#阶段二预测回滚式-视频重置中)
      - [**References：**](#references)



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
#  Lockstep Tutorial

### 前言
	本教程的目标是普及帧同步技术,含基本帧同步，以及预测回滚式帧同步，不含ECS
配套的Blog 
[配套的视频教程][3]


### 教程大纲
#### 阶段零: 帧同步概要

#### 阶段一: 基础帧同步(视频重置中...)
0. 大纲最  
1. 环境搭建
2. 帧同步开发注意事项  
3. 服务器，回放，客户端模式，基础框架，移动  
4. 不同步的检测与定位  
5. 帧同步逻辑编写  
6. 碰撞检测&技能系统  

#### 阶段二：预测&回滚式 (视频重置中...)
7. 帧同步预测回滚框架演示  
8. 预测回滚式框架概要 
9. 多平台,多实例 框架设计 
10. 多平台,多实例 框架实现  
11. "回滚" 基本生命期&数据的备份与还原  
12. "预测" 实现&守望先锋网络方案比对  
13. "预测" 自动伸缩的预测缓冲区  
14. 预测回滚中的不同步的检测  
15. 预测回滚帧同步中网络相关随机bug的重现与定位技巧  

#### **References：** 
- 使用的帧同步库 [https://github.com/JiepengTan/LockstepEngine][1]

---

< https://github.com/JiepengTan/LockstepEngine>
<https://github.com/JiepengTan/LockstepEngine_ARPGDemo>
<https://www.bilibili.com/video/av70422751/>
<https://github.com/JiepengTan/LockstepMath>
<https://github.com/JiepengTan/LockstepCollision>
<https://github.com/JiepengTan/LockstepBehaviorTree>
<https://github.com/JiepengTan/LockstepPathFinding>




