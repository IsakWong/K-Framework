// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections;
using KFramework.Action;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KFramework.Tests
{
    /// <summary>KTrigger（事件-条件-动作）的 PlayMode 测试集。</summary>
    public class TriggerTests
    {
        private GameObject _hostGo;
        private FlowTestHost _host;

        [SetUp]
        public void Setup()
        {
            _hostGo = new GameObject("[TriggerTestHost]");
            _host = _hostGo.AddComponent<FlowTestHost>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.Destroy(_hostGo);
        }

        // ─── 1. Once：触发一次后自动 Unregister ───
        [UnityTest]
        public IEnumerator Once_AutoUnregisters()
        {
            int count = 0;
            var trigger = KTrigger.Once()
                .OnManual()
                .Do(() => count++)
                .BuildAndRegister(_host);

            trigger.Fire();
            yield return null;   // 给 Action 一帧执行
            Assert.AreEqual(1, count);
            Assert.IsFalse(trigger.IsRegistered);

            trigger.Fire();
            yield return null;
            Assert.AreEqual(1, count, "Once 触发器不应再次执行");
        }

        // ─── 2. Repeating：多次触发都执行 ───
        [UnityTest]
        public IEnumerator Repeating_FiresMultipleTimes()
        {
            int count = 0;
            var trigger = KTrigger.Repeating()
                .OnManual()
                .Do(() => count++)
                .BuildAndRegister(_host);

            trigger.Fire(); yield return null;
            trigger.Fire(); yield return null;
            trigger.Fire(); yield return null;

            Assert.AreEqual(3, count);
            Assert.IsTrue(trigger.IsRegistered);
            trigger.Unregister();
        }

        // ─── 3. Condition false 不执行 Action ───
        [UnityTest]
        public IEnumerator Condition_RejectsFire()
        {
            int count = 0;
            bool gate = false;
            var trigger = KTrigger.Repeating()
                .OnManual()
                .When(_ => gate)
                .Do(() => count++)
                .BuildAndRegister(_host);

            trigger.Fire(); yield return null;
            Assert.AreEqual(0, count, "条件 false 不应触发");

            gate = true;
            trigger.Fire(); yield return null;
            Assert.AreEqual(1, count);
            trigger.Unregister();
        }

        // ─── 4. KSignal 源 ───
        [UnityTest]
        public IEnumerator SignalSource_Fires()
        {
            var sig = new KSignal();
            int count = 0;
            var trigger = KTrigger.Repeating()
                .On(sig)
                .Do(() => count++)
                .BuildAndRegister(_host);

            sig.Invoke(); yield return null;
            sig.Invoke(); yield return null;

            Assert.AreEqual(2, count);
            trigger.Unregister();
        }

        // ─── 5. EventBus 源 ───
        [UnityTest]
        public IEnumerator EventBusSource_Fires()
        {
            int count = 0;
            var trigger = KTrigger.Repeating()
                .On<TriggerTestEvent>()
                .Do(() => count++)
                .BuildAndRegister(_host);

            EventBus.Instance.Publish(new TriggerTestEvent { Score = 10 });
            yield return null;
            EventBus.Instance.Publish(new TriggerTestEvent { Score = 20 });
            yield return null;

            Assert.AreEqual(2, count);
            trigger.Unregister();
        }

        // ─── 6. EventBus filter ───
        [UnityTest]
        public IEnumerator EventBusFilter_RejectsMismatch()
        {
            int count = 0;
            var trigger = KTrigger.Repeating()
                .On<TriggerTestEvent>(e => e.Score >= 100)
                .Do(() => count++)
                .BuildAndRegister(_host);

            EventBus.Instance.Publish(new TriggerTestEvent { Score = 50 });
            yield return null;
            EventBus.Instance.Publish(new TriggerTestEvent { Score = 200 });
            yield return null;

            Assert.AreEqual(1, count);
            trigger.Unregister();
        }

        // ─── 7. Manual Fire ───
        [UnityTest]
        public IEnumerator Manual_FireExecutes()
        {
            int count = 0;
            var trigger = KTrigger.Repeating()
                .OnManual()
                .Do(() => count++)
                .BuildAndRegister(_host);

            trigger.Fire();
            yield return null;
            Assert.AreEqual(1, count);
            trigger.Unregister();
        }

        // ─── 8. Action 是 SubFlow（多步异步） ───
        [UnityTest]
        public IEnumerator ActionAsFlow_RunsToCompletion()
        {
            int step = 0;
            var subFlow = Flow.Create()
                .Do(_ => step = 1)
                .Wait(0.1f)
                .Do(_ => step = 2)
                .Wait(0.1f)
                .Do(_ => step = 3)
                .Build();

            var trigger = KTrigger.Once()
                .OnManual()
                .Do(subFlow)
                .BuildAndRegister(_host);

            trigger.Fire();
            // 等到 Flow 跑完
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(3, step);
            Assert.IsFalse(trigger.IsRegistered, "Once 应在 Action 完成后 Unregister");
        }

        // ─── 测试事件 ───

        private struct TriggerTestEvent : IEvent
        {
            public int Score;
        }
    }
}
