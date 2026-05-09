# 服务接口速查

K-Framework 通过 `ServiceLocator` 暴露服务，所有服务遵循 **接口隔离** 原则，可 Mock 测试。

## 服务列表

| 接口 | 实现类 | 主要 API |
|---|---|---|
| `ILogService` | `EnhancedLog` | `Verbose/Debug/Info/Warning/Error/Fatal(tag, msg)` · `SetGlobalLevel(level)` · `SetTagLevel(tag, level)` |
| `IAssetService` | `AssetManager` | `LoadAsset<T>(path)` · `LoadAssetAsync<T>(path)` · `LoadAsset<T>(AssetReference)` · `Instantiate(ref)` · `Release(handle)` |
| `IConfigService` | `ConfigManager` | `GetConfig<T>(name)` |
| `IUIService` | `UIManager` | `PushAsync<T>()` · `CloseAsync()` · `BringToFrontAsync<T>()` · `GetUI<T>()` · `DestroyAsync<T>()` |
| `ISoundService` | `SoundManager` | `PlaySound(clip, category)` · `PlaySound3D(clip, pos)` · `PlayMusic(clip)` · `SetMixerVolume(param, vol)` · `GetMixerVolume(param)` · `TransitionToSnapshot(snap)` · `DuckBGM(duration)` · `UnduckBGM()` · `MusicVolume` · `CanPlaySound(data)` · `GetEffectiveVolume(data, vol)` |
| `ISettingsService` | `SettingsManager` | `CurrentSettings` · `SaveSettings()` · `SetQuality(level)` · `SetResolution(w, h)` |
| `IPersistentDataService` | `PersistentDataManager` | `SaveData(key, data)` · `LoadData<T>(key)` · `DeleteData(key)` · `UpdateData(key, action)` · `GetScenePersistentData(path)` |
| `ISceneService` | `SceneManager` | `LoadScene(name)` · `LoadSceneAsync(name)` · `LoadAdditiveAsync(name)` · `UnloadAdditiveAsync(name)` · `GoBack()` · `OnSceneLoadBegin/Progress/Complete` |
| `IEventBusService` | `EventBus` | `Subscribe<T>(handler)` · `Publish<T>(event)` · `PublishSticky<T>(event)` · `QuerySticky<T>()` · `Unsubscribe<T>(handler)` |
| `IDebugService` | `DebugManager` | `DrawGizmos(action)` · `DrawRectangle(pos, size, color)` · `DrawSphere(pos, radius, color)` |
| `IVfxService` | `VfxManager` | `Get(prefab, pos, rot)` · `Release(vfx)` · `Preload(prefab, count)` |
| `IPoolService` | `PoolManager` | `Get(prefab, pos, rot)` · `Get<T>(prefab, pos, rot)` · `Release(instance)` · `Preload(prefab, count)` · `IsPooled(instance)` · `ClearAll()` |

## 访问模式

### 推荐：Service Locator + 接口

```csharp
var assets = ServiceLocator.Get<IAssetService>();
assets.LoadAsset<T>(path);
```

### 兼容：经典单例

```csharp
AssetManager.Instance.LoadAsset<T>(path);
```

### 安全：不抛异常

```csharp
if (ServiceLocator.TryGet<ISoundService>(out var sound))
    sound.PlayMusic(clip);
```

## 服务生命周期

| 类型 | 生命周期 | 说明 |
|------|----------|------|
| KSingleton | 首次访问创建，Application.Quit 销毁 | 纯 C# 服务 |
| PersistentSingleton | Awake 创建，DontDestroyOnLoad | MonoBehaviour 服务 |
| TModule\<T\> | Awake 自动注册，随 GameMode 销毁 | 可热插拔模块 |

所有 Manager 在构造或 Awake 时自动注册到 `ServiceLocator`。
