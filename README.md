# K-Framework
K-Framework是在制作 Mini-Game (Project-K0)，以及 Project-K1 时沉淀下的一个简易的游戏框架，旨在快速开发一个游戏Demo（仅仅适用于单机游戏）
## Mini Game 盒中少女
第一个版本的 K-Framework，包含了以下功能
1. Runtime/Editor下的 Asset 管理
2. Command设计模式
3. 模块化的Module
4. KGameCore / KGameCoreProxy
5. EventDispatch
6. 加强的Log功能
## Project-K1 拉克丝大战亚索
第二个版本的 K-Framework，仍在迭代中。包含了以下功能
1. Module 模块化架构
2. Utility工具类
3. Runtime / Editor 资源管理（基于AssetBundle 和 Addressable 的资源管理）
4. 有限状态机 FSM
5. 行为树 BT
6. 多种设计模式（迭代中）
7. 扩展可序列化（可序列化的任意类型 Variant，可序列化字典，可序列化Type）
8. 协程框架 
10. UI框架，列表UI
11. 对象池
12. GameMode 

### 扩展功能

Project-K1 基于 K-Framework 扩展了更多功能
基于模块化设计制作了
1. GameplayModule（核心模块）
2. PlayerModule（玩家模块）
3. CameraModule（摄像机模块）
4. PerformModule（演出模块）

基于GameUnit设计了一套游戏物体框架

GameUnit 

1. EnvUnit（环境单位）
2. FuncUnit（弹幕马甲）
3. VfxUnit （视效单位）
4. CharacterUnit（角色单位）
