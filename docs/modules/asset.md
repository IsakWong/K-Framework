# 资源管理

`AssetManager` 封装 Unity Addressables（Runtime）和 AssetDatabase（Editor），提供统一的资源加载接口。禁止直接使用 `Resources.Load()`。

## 基本用法

```csharp
// 同步加载（Editor 中通过 AssetDatabase，Runtime 通过 Addressables）
var prefab = AssetManager.Instance.LoadAsset<GameObject>(path);

// 异步加载
var prefab = await AssetManager.Instance.LoadAssetAsync<GameObject>(path);

// 通过 AssetReference 加载
var prefab = AssetManager.Instance.LoadAsset<GameObject>(assetReference);

// 实例化
var instance = AssetManager.Instance.Instantiate(reference);

// 释放
AssetManager.Instance.Release(handle);
```

## 服务接口

```csharp
// 推荐通过接口访问
var assets = ServiceLocator.Get<IAssetService>();
assets.LoadAssetAsync<T>(path);

// 安全访问
if (ServiceLocator.TryGet<IAssetService>(out var assets))
    await assets.LoadAssetAsync<T>(path);
```

## 路径约定

资源路径相对于 Addressables 配置的 group 根目录。Editor 模式下通过 AssetDatabase 直接查找，无需标记 Addressables。

## 依赖

- **Addressables** 1.19.0 — Runtime 资源加载
- **AssetDatabase** — Editor 模式（自动切换，无需配置）
