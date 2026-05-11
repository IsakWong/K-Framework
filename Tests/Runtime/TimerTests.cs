// Author: K-Framework Tests
// Date: 2026/05/11
// KTimer / KTimerManager 纯 C# 测试集

using NUnit.Framework;

namespace KFramework
{
    /// <summary>
    /// KTimer 和 KTimerManager 的纯 C# 测试集。
    /// 不依赖 Unity Time，手动驱动 tick。
    /// </summary>
    [TestFixture]
    public class TimerTests
    {
        // ─── KTimer 基础 ───

        [Test]
        public void KTimer_Start_SetsIsRunning()
        {
            var timer = new KTimer(1.0f);
            Assert.IsFalse(timer.IsRunning);

            timer.Start();
            Assert.IsTrue(timer.IsRunning);
        }

        [Test]
        public void KTimer_Reset_RestartsTimer()
        {
            var timer = new KTimer(1.0f);
            timer.Start();
            timer.OnLogic(0.5f);

            timer.Reset();
            Assert.IsTrue(timer.IsRunning);
            Assert.IsFalse(timer.IsFinished);
            Assert.AreEqual(0f, timer.ElapsedTime);
        }

        [Test]
        public void KTimer_Pause_StopsTicking()
        {
            var timer = new KTimer(1.0f);
            timer.Start();

            timer.OnLogic(0.3f);
            timer.Pause();
            Assert.IsFalse(timer.IsRunning);

            timer.OnLogic(1.0f); // 应该被忽略
            Assert.AreEqual(0.3f, timer.ElapsedTime, 0.001f, "暂停后不应累积时间");
        }

        [Test]
        public void KTimer_Stop_SetsRemoving()
        {
            var timer = new KTimer(1.0f);
            timer.Start();

            timer.Stop();
            Assert.IsFalse(timer.IsRunning);
            Assert.IsTrue(timer.IsRemoving);
        }

        // ─── 超时回调 ───

        [Test]
        public void KTimer_OnTimeout_FiresAtInterval()
        {
            int fired = 0;
            var timer = new KTimer(0.5f, () => fired++);
            timer.Start();

            timer.OnLogic(0.3f);
            Assert.AreEqual(0, fired, "还未到 interval");

            timer.OnLogic(0.3f); // 累计 0.6 > 0.5
            Assert.AreEqual(1, fired, "应触发一次");
        }

        [Test]
        public void KTimer_OnTimeout_FiresMultipleLoops()
        {
            int fired = 0;
            var timer = new KTimer(0.3f, () => fired++, loops: 3);
            timer.Start();

            // 第一循环
            timer.OnLogic(0.4f);
            Assert.AreEqual(1, fired);

            // 第二循环
            timer.OnLogic(0.4f);
            Assert.AreEqual(2, fired);

            // 第三循环
            timer.OnLogic(0.4f);
            Assert.AreEqual(3, fired);

            // 结束后不再触发
            timer.OnLogic(0.4f);
            Assert.AreEqual(3, fired);
        }

        [Test]
        public void KTimer_InfiniteLoop_NegativeLoops_RunsForever()
        {
            int fired = 0;
            var timer = new KTimer(0.1f, () => fired++, loops: -1);
            timer.Start();

            for (int i = 0; i < 10; i++)
            {
                timer.OnLogic(0.2f);
            }

            Assert.GreaterOrEqual(fired, 10);
        }

        [Test]
        public void KTimer_OnFinish_FiresWhenLoopsComplete()
        {
            int timeoutCount = 0;
            int finishCount = 0;
            var timer = new KTimer(0.1f, () => timeoutCount++, loops: 1);
            timer.OnFinish = () => finishCount++;
            timer.Start();

            timer.OnLogic(0.2f);

            Assert.AreEqual(1, timeoutCount);
            Assert.AreEqual(1, finishCount);
            Assert.IsTrue(timer.IsFinished);
        }

        // ─── 进度和剩余时间 ───

        [Test]
        public void KTimer_Progress_ReturnsCorrectRatio()
        {
            var timer = new KTimer(1.0f);
            timer.Start();

            timer.OnLogic(0.25f);
            Assert.AreEqual(0.25f, timer.Progress, 0.01f);

            timer.OnLogic(0.25f);
            Assert.AreEqual(0.5f, timer.Progress, 0.01f);

            timer.OnLogic(0.5f);
            Assert.AreEqual(1.0f, timer.Progress, 0.01f);
        }

        [Test]
        public void KTimer_RemainingTime_ReturnsCorrectValue()
        {
            var timer = new KTimer(2.0f);
            timer.Start();

            timer.OnLogic(0.5f);
            Assert.AreEqual(1.5f, timer.RemainingTime, 0.01f);

            timer.OnLogic(1.0f);
            Assert.AreEqual(0.5f, timer.RemainingTime, 0.01f);
        }

        [Test]
        public void KTimer_Progress_ClampedToZeroOne()
        {
            var timer = new KTimer(1.0f);
            timer.Start();
            timer.OnLogic(5.0f); // 远超 interval

            Assert.AreEqual(1.0f, timer.Progress);
        }

        [Test]
        public void KTimer_IsLooping_ReturnsCorrectValue()
        {
            var loopTimer = new KTimer(1.0f, loops: 3);
            loopTimer.Start();
            Assert.IsTrue(loopTimer.IsLooping);

            var singleTimer = new KTimer(1.0f, loops: 1);
            singleTimer.Start();
            singleTimer.OnLogic(2.0f);
            Assert.IsFalse(singleTimer.IsLooping);
        }

        // ─── Interval 属性 ───

        [Test]
        public void KTimer_Interval_CanBeChangedAtRuntime()
        {
            var timer = new KTimer(1.0f);
            timer.Start();

            timer.Interval = 0.5f;
            // 已累积时间不变，但新 interval 影响后续
            Assert.AreEqual(0.5f, timer.Interval);
        }

        // ─── KTimerManager ───

        [Test]
        public void KTimerManager_AddTimer_StartsAndFires()
        {
            var mgr = new KTimerManager();
            int fired = 0;

            mgr.AddTimer(0.3f, () => fired++);
            mgr.OnLogic(0.2f);
            Assert.AreEqual(0, fired);

            mgr.OnLogic(0.2f);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void KTimerManager_RemoveTimer_StopsFiring()
        {
            var mgr = new KTimerManager();
            int fired = 0;

            var timer = mgr.AddTimer(0.1f, () => fired++);
            mgr.RemoveTimer(timer);

            mgr.OnLogic(0.5f);
            Assert.AreEqual(0, fired, "已移除的 timer 不应触发");
        }

        [Test]
        public void KTimerManager_StopAllTimer_StopsEverything()
        {
            var mgr = new KTimerManager();
            int a = 0, b = 0;

            mgr.AddTimer(0.1f, () => a++);
            mgr.AddTimer(0.1f, () => b++);

            mgr.StopAllTimer();
            mgr.OnLogic(0.5f);

            Assert.AreEqual(0, a);
            Assert.AreEqual(0, b);
        }

        [Test]
        public void KTimerManager_MultipleTimers_AllTick()
        {
            var mgr = new KTimerManager();
            int a = 0, b = 0, c = 0;

            mgr.AddTimer(0.2f, () => a++);
            mgr.AddTimer(0.3f, () => b++);
            mgr.AddTimer(0.5f, () => c++);

            mgr.OnLogic(0.4f);
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
            Assert.AreEqual(0, c);
        }

        [Test]
        public void KTimerManager_FinishedTimer_IsAutoRemoved()
        {
            var mgr = new KTimerManager();
            int fired = 0;

            mgr.AddTimer(0.1f, () => fired++, loops: 2);

            // 两循环
            mgr.OnLogic(0.2f);
            mgr.OnLogic(0.2f);
            Assert.AreEqual(2, fired);

            // 不应再有触发
            int before = fired;
            mgr.OnLogic(1.0f);
            Assert.AreEqual(before, fired, "已完成的不应再触发");
        }
    }
}
