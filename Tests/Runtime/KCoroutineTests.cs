// Author: K-Framework Tests
// Date: 2026/05/11
// KCoroutine / CoroutineHandler / CoroutineManager 纯 C# 测试集

using System.Collections;
using Framework.Coroutine;
using NUnit.Framework;

namespace KFramework
{
    /// <summary>
    /// KCoroutine（自定义协程系统）的 EditMode 测试集。
    /// 不依赖 Unity MonoBehaviour，手动驱动 Tick。
    /// </summary>
    [TestFixture]
    public class KCoroutineTests
    {
        private CoroutineHandler _handler;

        [SetUp]
        public void Setup()
        {
            _handler = new CoroutineHandler();
        }

        [TearDown]
        public void TearDown()
        {
            _handler.Clear();
        }

        // ─── 基础协程执行 ───

        [Test]
        public void KCoroutine_Basic_RunsToCompletion()
        {
            int step = 0;

            IEnumerator Routine()
            {
                step = 1;
                yield return null;
                step = 2;
            }

            var co = _handler.StartCoroutine(Routine());

            // 第一帧：step = 1，遇到 yield return null
            _handler.Tick(0.016f);
            Assert.AreEqual(2, step, "yield return null 应立刻在下一 Tick 继续");
            Assert.IsTrue(co.IsDone);
        }

        [Test]
        public void KCoroutine_NullRoutine_ReturnsNull()
        {
            var co = _handler.StartCoroutine(null);
            Assert.IsNull(co);
        }

        // ─── WaitSeconds ───

        [Test]
        public void KCoroutine_WaitSeconds_WaitsApproximateTime()
        {
            int step = 0;

            IEnumerator Routine()
            {
                step = 1;
                yield return new WaitSeconds(1.0f);
                step = 2;
            }

            var co = _handler.StartCoroutine(Routine());

            // Tick 0.3s — WaitSeconds 未到
            _handler.Tick(0.3f);
            Assert.AreEqual(1, step);

            // Tick 0.5s — WaitSeconds 仍未到（累计 0.8）
            _handler.Tick(0.5f);
            Assert.AreEqual(1, step);

            // Tick 0.3s — WaitSeconds 已到（累计 1.1）
            _handler.Tick(0.3f);
            Assert.AreEqual(2, step);
            Assert.IsTrue(co.IsDone);
        }

        [Test]
        public void KCoroutine_MultipleWaitSeconds_AccumulatesCorrectly()
        {
            int step = 0;

            IEnumerator Routine()
            {
                step = 1;
                yield return new WaitSeconds(0.5f);
                step = 2;
                yield return new WaitSeconds(0.5f);
                step = 3;
            }

            var co = _handler.StartCoroutine(Routine());

            _handler.Tick(0.6f);
            Assert.AreEqual(2, step);

            _handler.Tick(0.6f);
            Assert.AreEqual(3, step);
            Assert.IsTrue(co.IsDone);
        }

        // ─── WaitForNextFrame ───

        [Test]
        public void KCoroutine_WaitForNextFrame_ResumesNextTick()
        {
            int step = 0;

            IEnumerator Routine()
            {
                step = 1;
                yield return new WaitForNextFrame();
                step = 2;
                yield return new WaitForNextFrame();
                step = 3;
                yield return new WaitForNextFrame();
                step = 4;
            }

            var co = _handler.StartCoroutine(Routine());

            _handler.Tick(0.016f);
            Assert.AreEqual(2, step);

            _handler.Tick(0.016f);
            Assert.AreEqual(3, step);

            _handler.Tick(0.016f);
            Assert.AreEqual(4, step);
            Assert.IsTrue(co.IsDone);
        }

        // ─── WaitUntil ───

        [Test]
        public void KCoroutine_WaitUntil_ResumesWhenConditionTrue()
        {
            bool flag = false;
            int step = 0;

            IEnumerator Routine()
            {
                step = 1;
                yield return new WaitUntil(() => flag);
                step = 2;
            }

            var co = _handler.StartCoroutine(Routine());

            _handler.Tick(0.1f);
            Assert.AreEqual(1, step, "条件不满足，不应继续");

            flag = true;
            _handler.Tick(0.1f);
            Assert.AreEqual(2, step);
            Assert.IsTrue(co.IsDone);
        }

        // ─── WaitWhile ───

        [Test]
        public void KCoroutine_WaitWhile_ResumesWhenConditionFalse()
        {
            bool flag = true;
            int step = 0;

            IEnumerator Routine()
            {
                step = 1;
                yield return new WaitWhile(() => flag);
                step = 2;
            }

            var co = _handler.StartCoroutine(Routine());

            _handler.Tick(0.1f);
            Assert.AreEqual(1, step, "flag 为 true，应继续等待");

            flag = false;
            _handler.Tick(0.1f);
            Assert.AreEqual(2, step);
            Assert.IsTrue(co.IsDone);
        }

        // ─── 暂停/恢复/停止 ───

        [Test]
        public void KCoroutine_Pause_StopsTicking()
        {
            int step = 0;

            IEnumerator Routine()
            {
                step = 1;
                yield return new WaitSeconds(1.0f);
                step = 2;
            }

            var co = _handler.StartCoroutine(Routine());

            co.Pause();
            Assert.IsTrue(co.IsPaused);

            // 即使时间够了也不应推进
            _handler.Tick(2.0f);
            Assert.AreEqual(1, step);

            co.Resume();
            Assert.IsFalse(co.IsPaused);

            _handler.Tick(2.0f);
            Assert.AreEqual(2, step);
        }

        [Test]
        public void KCoroutine_Stop_MarksAsDoneImmediately()
        {
            int step = 0;

            IEnumerator Routine()
            {
                step = 1;
                yield return new WaitSeconds(10.0f);
                step = 2; // never reached
            }

            var co = _handler.StartCoroutine(Routine());
            _handler.Tick(0.1f);
            Assert.AreEqual(1, step);

            co.Stop();
            Assert.IsTrue(co.IsDone);
            Assert.IsTrue(co.IsStopped);

            // Handler 应自动移除已停止的协程
            _handler.Tick(0.1f);
            Assert.AreEqual(0, _handler.ActiveCoroutineCount);
        }

        [Test]
        public void KCoroutine_IsRunning_ReflectsState()
        {
            IEnumerator Routine()
            {
                yield return new WaitSeconds(100f);
            }

            var co = _handler.StartCoroutine(Routine());
            Assert.IsTrue(co.IsRunning);
            Assert.IsFalse(co.IsPaused);
            Assert.IsFalse(co.IsDone);

            co.Pause();
            Assert.IsTrue(co.IsRunning); // 暂停但未停止

            co.Stop();
            Assert.IsFalse(co.IsRunning);
            Assert.IsTrue(co.IsDone);
        }

        // ─── 嵌套协程 ───

        [Test]
        public void KCoroutine_NestedCoroutine_RunsChildToCompletion()
        {
            int order = 0;

            IEnumerator Child()
            {
                order = 1;
                yield return new WaitSeconds(0.5f);
                order = 2;
            }

            IEnumerator Parent()
            {
                order = 0;
                yield return Child();
                order = 3;
            }

            var co = _handler.StartCoroutine(Parent());

            // 同步执行到 child 的 WaitSeconds
            _handler.Tick(0.1f);
            Assert.AreEqual(1, order, "进入 child，但卡在 WaitSeconds");

            _handler.Tick(1.0f);
            Assert.AreEqual(3, order, "child 完成，parent 继续");
            Assert.IsTrue(co.IsDone);
        }

        [Test]
        public void KCoroutine_DeeplyNested_RunsAllLayers()
        {
            var log = new System.Collections.Generic.List<int>();

            IEnumerator Level3()
            {
                yield return new WaitSeconds(0.1f);
                log.Add(3);
            }

            IEnumerator Level2()
            {
                yield return new WaitSeconds(0.1f);
                log.Add(2);
                yield return Level3();
            }

            IEnumerator Level1()
            {
                yield return new WaitSeconds(0.1f);
                log.Add(1);
                yield return Level2();
                log.Add(0);
            }

            _handler.StartCoroutine(Level1());

            // 推进足够时间
            for (int i = 0; i < 20; i++)
                _handler.Tick(0.1f);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 0 }, log.ToArray());
        }

        // ─── yield break ───

        [Test]
        public void KCoroutine_YieldBreak_EarlyExit()
        {
            int reachable = 0;

            IEnumerator Routine()
            {
                reachable = 1;
                yield break;
#pragma warning disable CS0162 // Unreachable code detected
                reachable = 999;
#pragma warning restore CS0162
            }

            var co = _handler.StartCoroutine(Routine());
            _handler.Tick(0.1f);

            Assert.AreEqual(1, reachable);
            Assert.IsTrue(co.IsDone);
        }

        // ─── CoroutineHandler 管理 ───

        [Test]
        public void CoroutineHandler_StopAllCoroutines_StopsEverything()
        {
            int a = 0, b = 0;

            _handler.StartCoroutine(Counter(() => a++));
            _handler.StartCoroutine(Counter(() => b++));

            _handler.Tick(0.1f);
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);

            _handler.StopAllCoroutines();

            _handler.Tick(1.0f);
            Assert.AreEqual(1, a, "应已停止");
            Assert.AreEqual(1, b, "应已停止");
            Assert.AreEqual(0, _handler.ActiveCoroutineCount);
        }

        [Test]
        public void CoroutineHandler_Clear_RemovesAll()
        {
            _handler.StartCoroutine(Counter(null));
            _handler.StartCoroutine(Counter(null));
            Assert.AreEqual(2, _handler.ActiveCoroutineCount);

            _handler.Clear();
            Assert.AreEqual(0, _handler.ActiveCoroutineCount);
        }

        [Test]
        public void CoroutineHandler_GetActiveCoroutines_ReturnsReadOnly()
        {
            _handler.StartCoroutine(Counter(null));
            var list = _handler.GetActiveCoroutines();

            Assert.AreEqual(1, list.Count);
        }

        // ─── CoroutineManager ───

        [Test]
        public void CoroutineManager_DefaultTiming_IsLogic()
        {
            var mgr = new CoroutineManager();
            int step = 0;

            mgr.StartCoroutine(Counter(() => step++));
            Assert.AreEqual(1, mgr.GetActiveCount(CoroutineManager.TickTiming.Logic));

            mgr.TickFixedUpdate(0.1f);
            Assert.AreEqual(1, step);

            mgr.Clear();
        }

        [Test]
        public void CoroutineManager_MultipleTimings_AreIndependent()
        {
            var mgr = new CoroutineManager();
            int updateCount = 0, fixedCount = 0;

            mgr.StartCoroutine(Counter(() => updateCount++), CoroutineManager.TickTiming.Update);
            mgr.StartCoroutine(Counter(() => fixedCount++), CoroutineManager.TickTiming.Logic);

            mgr.TickUpdate(0.1f);
            Assert.AreEqual(1, updateCount);
            Assert.AreEqual(0, fixedCount);

            mgr.TickFixedUpdate(0.1f);
            Assert.AreEqual(1, updateCount);
            Assert.AreEqual(1, fixedCount);

            mgr.Clear();
        }

        [Test]
        public void CoroutineManager_Clear_RemovesAllTimings()
        {
            var mgr = new CoroutineManager();
            mgr.StartCoroutine(Counter(null), CoroutineManager.TickTiming.Update);
            mgr.StartCoroutine(Counter(null), CoroutineManager.TickTiming.Logic);
            mgr.StartCoroutine(Counter(null), CoroutineManager.TickTiming.LateUpdate);

            mgr.Clear();

            Assert.AreEqual(0, mgr.GetActiveCount(CoroutineManager.TickTiming.Update));
            Assert.AreEqual(0, mgr.GetActiveCount(CoroutineManager.TickTiming.Logic));
            Assert.AreEqual(0, mgr.GetActiveCount(CoroutineManager.TickTiming.LateUpdate));
        }

        // ─── 辅助 ───

        private static IEnumerator Counter(System.Action onEach)
        {
            while (true)
            {
                onEach?.Invoke();
                yield return new WaitSeconds(1.0f);
            }
        }
    }
}
