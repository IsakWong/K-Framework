// Author: K-Framework Tests
// Date: 2026/05/11
// KSignal / Subscriber / KSignal<T> PlayMode 测试集

using System;
using NUnit.Framework;

namespace KFramework
{
    /// <summary>
    /// KSignal / KSignal&lt;T&gt; / Subscriber 的纯 C# 测试（EditMode 兼容）。
    /// 不依赖 MonoBehaviour，可在 Editor 下直接运行。
    /// </summary>
    [TestFixture]
    public class SignalTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        // ─── KSignal（无参）基础 ───

        [Test]
        public void KSignal_Invoke_TriggersAllConnectedCallbacks()
        {
            var sig = new KSignal();
            int a = 0, b = 0, c = 0;

            sig.Connect(() => a = 1);
            sig.Connect(() => b = 2);
            sig.Connect(() => c = 3);

            sig.Invoke();

            Assert.AreEqual(1, a);
            Assert.AreEqual(2, b);
            Assert.AreEqual(3, c);
        }

        [Test]
        public void KSignal_ConnectOnce_FiresOnlyOnce()
        {
            var sig = new KSignal();
            int count = 0;

            sig.ConnectOnce(() => count++);

            sig.Invoke();
            sig.Invoke();
            sig.Invoke();

            Assert.AreEqual(1, count, "ConnectOnce 应只触发一次");
        }

        [Test]
        public void KSignal_DisconnectByHandle_RemovesCallback()
        {
            var sig = new KSignal();
            int count = 0;

            var handle = sig.Connect(() => count++);
            sig.Invoke();
            Assert.AreEqual(1, count);

            sig.Disconnect(handle);
            sig.Invoke();
            Assert.AreEqual(1, count, "Disconnect 后不应再触发");
        }

        [Test]
        public void KSignal_DisconnectByDelegate_RemovesCallback()
        {
            var sig = new KSignal();
            int count = 0;
            Action cb = () => count++;

            sig.Connect(cb);
            sig.Invoke();
            Assert.AreEqual(1, count);

            sig.Disconnect(cb);
            sig.Invoke();
            Assert.AreEqual(1, count, "Disconnect 后不应再触发");
        }

        [Test]
        public void KSignal_Subscriber_DisconnectAll_RemovesAll()
        {
            var sig = new KSignal();
            var sub = new Subscriber();
            int a = 0, b = 0;

            sig.Connect(sub, () => a = 1);
            sig.Connect(sub, () => b = 2);
            sig.Invoke();
            Assert.AreEqual(1, a);
            Assert.AreEqual(2, b);

            sub.DisconnectAll();
            a = b = 0;
            sig.Invoke();
            Assert.AreEqual(0, a);
            Assert.AreEqual(0, b);
        }

        [Test]
        public void KSignal_Subscriber_Dispose_RemovesAll()
        {
            var sig = new KSignal();
            int count = 0;

            using (var sub = new Subscriber())
            {
                sig.Connect(sub, () => count++);
                sig.Invoke();
                Assert.AreEqual(1, count);
            }

            sig.Invoke();
            Assert.AreEqual(1, count, "Dispose 后不应再触发");
        }

        // ─── KSignal<T>（单参） ───

        [Test]
        public void KSignalT_Invoke_PassesArgument()
        {
            var sig = new KSignal<int>();
            int received = 0;

            sig.Connect(val => received = val);
            sig.Invoke(42);

            Assert.AreEqual(42, received);
        }

        [Test]
        public void KSignalT_ConnectOnce_FiresOnlyOnceWithArg()
        {
            var sig = new KSignal<string>();
            string received = null;

            sig.ConnectOnce(val => received = val);
            sig.Invoke("first");
            sig.Invoke("second");

            Assert.AreEqual("first", received);
        }

        [Test]
        public void KSignalT_DisconnectByDelegate_StopsReceiving()
        {
            var sig = new KSignal<float>();
            float received = 0f;
            Action<float> cb = val => received = val;

            sig.Connect(cb);
            sig.Invoke(3.14f);
            Assert.AreEqual(3.14f, received);

            sig.Disconnect(cb);
            received = 0f;
            sig.Invoke(2.71f);
            Assert.AreEqual(0f, received);
        }

        // ─── KSignal<T1,T2>（双参） ───

        [Test]
        public void KSignalTwoParams_Invoke_PassesBothArgs()
        {
            var sig = new KSignal<int, string>();
            int r1 = 0;
            string r2 = null;

            sig.Connect((i, s) => { r1 = i; r2 = s; });
            sig.Invoke(10, "hello");

            Assert.AreEqual(10, r1);
            Assert.AreEqual("hello", r2);
        }

        // ─── KSignal<T1,T2,T3>（三参） ───

        [Test]
        public void KSignalThreeParams_Invoke_PassesAllArgs()
        {
            var sig = new KSignal<int, float, string>();
            int r1 = 0;
            float r2 = 0f;
            string r3 = null;

            sig.Connect((i, f, s) => { r1 = i; r2 = f; r3 = s; });
            sig.Invoke(1, 2.5f, "world");

            Assert.AreEqual(1, r1);
            Assert.AreEqual(2.5f, r2);
            Assert.AreEqual("world", r3);
        }

        // ─── KSignal<T1,T2,T3,T4>（四参） ───

        [Test]
        public void KSignalFourParams_Invoke_PassesAllArgs()
        {
            var sig = new KSignal<int, float, string, bool>();
            int r1 = 0;
            float r2 = 0f;
            string r3 = null;
            bool r4 = false;

            sig.Connect((i, f, s, b) => { r1 = i; r2 = f; r3 = s; r4 = b; });
            sig.Invoke(5, 1.1f, "test", true);

            Assert.AreEqual(5, r1);
            Assert.AreEqual(1.1f, r2);
            Assert.AreEqual("test", r3);
            Assert.IsTrue(r4);
        }

        // ─── Signal operator +/- ───

        [Test]
        public void Signal_OperatorPlus_AddsCallback()
        {
            var sig = new KSignal();
            int count = 0;
            Action cb = () => count++;

            sig += cb;
            sig.Invoke();
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Signal_OperatorMinus_RemovesCallback()
        {
            var sig = new KSignal();
            int count = 0;
            Action cb = () => count++;

            sig += cb;
            sig.Invoke();
            sig -= cb;
            count = 0;
            sig.Invoke();
            Assert.AreEqual(0, count);
        }

        // ─── Subscriber 多信号管理 ───

        [Test]
        public void Subscriber_MultipleSignals_DisconnectAllClearsAll()
        {
            var sig1 = new KSignal();
            var sig2 = new KSignal<int>();
            var sub = new Subscriber();
            int a = 0, b = 0;

            sig1.Connect(sub, () => a++);
            sig2.Connect(sub, _ => b++);

            sig1.Invoke();
            sig2.Invoke(0);
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);

            sub.DisconnectAll();

            sig1.Invoke();
            sig2.Invoke(0);
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }

        // ─── 边界情况 ───

        [Test]
        public void KSignal_NoSubscribers_InvokeDoesNotThrow()
        {
            var sig = new KSignal();
            Assert.DoesNotThrow(() => sig.Invoke());
        }

        [Test]
        public void KSignal_ConnectOnceMixed_OnceFiredOnlyOnce_RegularStillFires()
        {
            var sig = new KSignal();
            int onceCount = 0, regularCount = 0;

            sig.ConnectOnce(() => onceCount++);
            sig.Connect(() => regularCount++);

            sig.Invoke();
            sig.Invoke();
            sig.Invoke();

            Assert.AreEqual(1, onceCount);
            Assert.AreEqual(3, regularCount);
        }

        [Test]
        public void KSignal_NullSubscriber_ThrowsNullRef()
        {
            var sig = new KSignal();
            Assert.Throws<NullReferenceException>(() => sig.Connect(null as Subscriber, () => { }));
        }

        // ─── DisconnectAll ───

        [Test]
        public void KSignal_DisconnectAll_ClearsAllCallbacks()
        {
            var sig = new KSignal();
            int a = 0, b = 0;

            sig.Connect(() => a++);
            sig.Connect(() => b++);
            sig.Invoke();
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);

            sig.DisconnectAll();
            sig.Invoke();
            Assert.AreEqual(1, a, "DisconnectAll 后不应触发");
            Assert.AreEqual(1, b);
        }

        [Test]
        public void KSignal_DisconnectAll_ClearsOnceCallbacks()
        {
            var sig = new KSignal();
            int count = 0;

            sig.ConnectOnce(() => count++);
            sig.DisconnectAll();
            sig.Invoke();

            Assert.AreEqual(0, count, "DisconnectAll 应清除 Once 回调");
        }

        // ─── Disconnect(int) 返回 bool ───

        [Test]
        public void KSignal_DisconnectByHandle_ReturnsTrue_WhenExists()
        {
            var sig = new KSignal();
            var handle = sig.Connect(() => { });

            Assert.IsTrue(sig.Disconnect(handle));
        }

        [Test]
        public void KSignal_DisconnectByHandle_ReturnsFalse_WhenNotExists()
        {
            var sig = new KSignal();

            Assert.IsFalse(sig.Disconnect(99999));
        }

        [Test]
        public void KSignal_DisconnectByHandle_ReturnsFalse_WhenAlreadyDisconnected()
        {
            var sig = new KSignal();
            var handle = sig.Connect(() => { });

            sig.Disconnect(handle);
            Assert.IsFalse(sig.Disconnect(handle), "重复断开应返回 false");
        }

        // ─── ConnectOnce handle 在 Invoke 后清理 ───

        [Test]
        public void KSignal_ConnectOnce_HandleCleanedUpAfterInvoke()
        {
            var sig = new KSignal();
            var handle = sig.ConnectOnce(() => { });

            sig.Invoke();

            // handle 应已被清理
            Assert.IsFalse(sig.Disconnect(handle), "Once 触发后 handle 应已自动清理");
        }

        [Test]
        public void KSignal_ConnectOnce_CanDisconnectBeforeInvoke()
        {
            var sig = new KSignal();
            int count = 0;
            var handle = sig.ConnectOnce(() => count++);

            Assert.IsTrue(sig.Disconnect(handle));
            sig.Invoke();
            Assert.AreEqual(0, count, "提前断开后不应触发");
        }

        // ─── Disconnect(TDelegate) 清理 Once ───

        [Test]
        public void KSignal_DisconnectByDelegate_ClearsOnceCallback()
        {
            var sig = new KSignal();
            int count = 0;
            Action cb = () => count++;

            sig.ConnectOnce(cb);
            sig.Disconnect(cb);
            sig.Invoke();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void KSignal_DisconnectByDelegate_ClearsHandleMap()
        {
            var sig = new KSignal();
            Action cb = () => { };
            var handle = sig.ConnectOnce(cb);

            sig.Disconnect(cb);

            // handle 应已被清理
            Assert.IsFalse(sig.Disconnect(handle));
        }

        // ─── int handle 不装箱 ───

        [Test]
        public void KSignal_Connect_ReturnsIntHandle()
        {
            var sig = new KSignal();
            var h1 = sig.Connect(() => { });
            var h2 = sig.Connect(() => { });

            Assert.Greater(h2, h1, "handle 应自增");
        }
    }
}
