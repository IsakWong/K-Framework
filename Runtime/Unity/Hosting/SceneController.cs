using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// 场景控制器（组合根）。每个场景一个，由 GameFlow 在场景加载后显式调用 <see cref="InitializeAsync"/>。
    /// 负责场景生命周期与场景级 BGM；具体业务由子类与普通协作类承担。
    /// </summary>
    [DefaultExecutionOrder(GameCoreProxy.GameModeOrder)]
    public abstract class SceneController : MonoBehaviour
    {
        [Header("场景音乐")]
        [Tooltip("进入本场景后播放的背景音乐；为空则沿用当前音乐")]
        public AudioClip SceneMusic;

        [Range(0f, 1f)]
        public float SceneMusicVolume = 1f;

        private bool _initialized;
        private bool _ended;

        public bool IsInitialized => _initialized;

        protected virtual void Awake()
        {
            // 确保 DebugGUI 在启动时即被创建，F3 可随时切换显示
            _ = DebugGUI.Instance;
            OnSceneAwake();
        }

        protected virtual void OnDestroy() => OnSceneEnd();

        /// <summary>
        /// 同步初始化钩子：场景 Awake 时调用，早于第一帧渲染。
        /// 需要「第一帧之前就生效」的初始化（相机、全局注册表等）放这里。
        /// </summary>
        public virtual void OnSceneAwake()
        {
        }

        /// <summary>显式初始化入口：由 GameFlow 在场景加载后调用（幂等）。</summary>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            if (_initialized)
                return;

            _initialized = true;

            // 用 MonoBehaviour 原生协程驱动：支持 WaitForFixedUpdate / 嵌套协程等全部 yield 类型。
            await RunInitialize().ToUniTask(this).AttachExternalCancellation(ct);
        }

        private IEnumerator RunInitialize()
        {
            yield return InitializeScene();
            yield return OnSceneStart();
        }

        /// <summary>场景初始化（协程）。子类通过 <see cref="OnSceneInitialize"/> 扩展。</summary>
        protected virtual IEnumerator InitializeScene()
        {
            yield return OnSceneInitialize();
        }

        /// <summary>场景初始化时调用（在 OnSceneStart 之前）。</summary>
        protected virtual IEnumerator OnSceneInitialize()
        {
            yield return null;
        }

        /// <summary>场景启动：播放场景音乐后执行子类逻辑。</summary>
        public virtual IEnumerator OnSceneStart()
        {
            PlaySceneMusic();
            yield return null;
        }

        /// <summary>重启场景。</summary>
        public virtual void OnSceneRestart()
        {
        }

        public void RestartScene() => OnSceneRestart();

        /// <summary>场景卸载时调用（幂等）。</summary>
        public virtual void OnSceneEnd()
        {
            if (_ended)
                return;

            _ended = true;
        }

        protected void PlaySceneMusic()
        {
            if (SceneMusic == null || SoundService.Instance == null)
                return;

            if (SoundService.Instance.CurrentMusicClip == SceneMusic)
                return;

            SoundService.Instance.PlayMusic(SceneMusic, null, SceneMusicVolume);
        }
    }
}
