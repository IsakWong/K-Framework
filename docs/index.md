---
layout: home

hero:
  name: K-Framework
  text: 轻量级模块化 Unity 游戏框架
  tagline: 面向 Unity 2D/3D 游戏，MIT 许可证，Unity 2021.3+
  actions:
    - theme: brand
      text: 快速开始
      link: /guide/installation
    - theme: alt
      text: GitHub
      link: https://github.com/IsakWong/K-Framework

features:
  - icon: 🧩
    title: 分层架构
    details: Foundation → Core → Module → FrameworkExt 四层职责分明，依赖递减
  - icon: 🔌
    title: 接口隔离
    details: Service Locator + 接口抽象，支持 Mock 测试和运行时服务替换
  - icon: 📡
    title: 双轨通信
    details: KSignal（点对点信号）+ EventBus（全局事件广播），解耦模块通信
  - icon: 🎮
    title: Unit 生命周期
    details: 完整实体生命周期：Spawning → Alive → Dying → Dead → Deleting
  - icon: 🖼️
    title: UI 栈管理
    details: Fullscreen 栈式独占 + Overlay 叠加层 + 异步 API + 可扩展动画
  - icon: 🔊
    title: 音频系统
    details: SoundCategory 双层架构 + Mixer API + BGM Ducking + 3D 音效
---

## 版本

当前版本 **1.7.0**，兼容 Unity 2021.3+。

## 安装

```bash
# Unity Package Manager（推荐）
# Window → Package Manager → + → Add package from git URL...
https://github.com/isakwong/KFramework.git
```

或在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.isakwong.kframework": "https://github.com/isakwong/KFramework.git"
  }
}
```

详见 [安装指南](/guide/installation)。
