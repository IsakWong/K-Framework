# 安装

## 方式一：Unity Package Manager（推荐）

1. 打开 Unity → **Window** → **Package Manager**
2. 点击左上角 **+** → **Add package from git URL...**
3. 输入：

```
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

指定版本 tag：

```
https://github.com/isakwong/KFramework.git#v1.7.0
```

## 方式二：手动安装

将仓库克隆到 Unity 项目的 `Packages/` 目录下：

```bash
cd YourProject/Packages
git clone https://github.com/isakwong/KFramework.git com.isakwong.kframework
```

## 额外依赖

以下依赖需要单独安装（非 UPM 包）：

| 依赖 | 说明 | 安装方式 |
|------|------|----------|
| DOTween | 动画缓动 | Asset Store |
| Newtonsoft.Json | JSON 序列化 | UPM: `com.unity.nuget.newtonsoft-json` |
| Odin Inspector | Inspector 增强 | Asset Store（可选） |
| MoreMountains Feel | 反馈系统 | Asset Store（可选） |

## 强制依赖

这些依赖通过 UPM 自动引入：

| 依赖 | 用途 |
|------|------|
| UniTask 2.5.10+ | 异步（UI、Asset 加载） |
| Unity InputSystem 1.4.0 | 输入 |
| Unity Addressables 1.19.0 | 资源加载 |
| Unity TextMeshPro 3.0.0 | 文本渲染 |
