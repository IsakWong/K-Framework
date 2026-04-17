# Changelog

All notable changes to KFramework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [1.6.0] - 2026-04-17

### Added
- **SoundCategory** (ScriptableObject) — identity-based audio category with mixer routing, concurrency limits, cooldown, volume decay, random pitch
- **Mixer API** — `SetMixerVolume` / `GetMixerVolume`, Snapshot transitions, BGM Ducking
- **SoundEmitter** implements `IPoolable` — unified with PoolManager instead of separate Unity ObjectPool

### Changed
- **SoundData** slimmed to pure AudioSource parameter template (no clip, no mixerGroup, no concurrency fields)
- **SoundBuilder.Play()** accepts `SoundCategory` for routing and concurrency control
- **AudioConfig** presets changed from `SoundData` to `SoundCategory` references
- **Project structure** reorganized to UPM package format

### Fixed
- PlayMusic memory leak — old AudioSource components were never destroyed
- PlaySoundLimitFrame ignored position parameter
- CrossFade used `Time.deltaTime` instead of `Time.unscaledDeltaTime`
- SoundEmitter.Stop() hardcoded SoundManager.Instance coupling (replaced with callback)
- RandomAudioClip init in FixedUpdate moved to OnEnable
- StopAll() FrequentSoundEmitters crash after Clear()
- PlaySoundLimitFrame lost SoundCategory when deferred to FixedUpdate

## [1.5.0] - 2026-04-16

### Added
- **Generic Object Pool** — PoolManager, GameObjectPool, CSharpPool, IPoolable interface
- **UnitBase** opt-in pool recycling via `CanRecycle` override

## [1.4.0] - 2026-04-15

### Added
- **EnhancedLog** — structured log system with levels (Debug/Info/Warning/Error/Fatal), module tags, local file output
- **ILogService** interface via ServiceLocator

## [1.3.0] - 2026-04-14

### Added
- **Service Locator** pattern — `ServiceLocator.Register<T>()` / `Get<T>()` / `TryGet<T>()`
- All singletons auto-register their service interfaces

## [1.2.0] - 2026-04-13

### Added
- **EventBus** — type-safe publish/subscribe decoupled messaging
- **IEventBusService** interface

## [1.1.0] - 2026-04-12

### Added
- **SceneManager** — async scene loading with progress callbacks, no LoadingUI coupling
- **KVersion** — framework version tracking via VersionConfig ScriptableObject

## [1.0.0] - 2026-04-10

### Added
- Initial release
- Core architecture: KGameCore, KSingleton, PersistentSingleton, TModule
- Config system, FSM, Behavior Tree, Command pattern
- UI system (UIPanel, UIManager, InfiniteScroll)
- Sound system (SoundManager, SoundEmitter, SoundBuilder)
- Persistent data, Coroutine utilities, Addressables integration
- SerializedCollections, JsonConverters
