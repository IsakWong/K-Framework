// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;
using System.Collections.Generic;
using KFramework.Action;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KFramework.Tests
{
    /// <summary>Flow 系统的 PlayMode 测试集。</summary>
    public class FlowTests
    {
        private GameObject _hostGo;
        private FlowTestHost _host;

        [SetUp]
        public void Setup()
        {
            _hostGo = new GameObject("[FlowTestHost]");
            _host = _hostGo.AddComponent<FlowTestHost>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.Destroy(_hostGo);
        }

        // ─── 1. 顺序执行 ───
        [UnityTest]
        public IEnumerator Sequence_RunsInOrder()
        {
            var log = new List<string>();
            var handle = Flow.Create()
                .Do(_ => log.Add("A"))
                .Do(_ => log.Add("B"))
                .Do(_ => log.Add("C"))
                .Build()
                .Run(_host);

            yield return handle.Wait();
            Assert.IsTrue(handle.IsCompleted);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, log);
        }

        // ─── 2. Wait ───
        [UnityTest]
        public IEnumerator Wait_BlocksForApproximateDuration()
        {
            float start = Time.time;
            var handle = Flow.Create().Wait(0.5f).Build().Run(_host);
            yield return handle.Wait();
            float elapsed = Time.time - start;
            Assert.IsTrue(handle.IsCompleted);
            Assert.GreaterOrEqual(elapsed, 0.45f, $"elapsed={elapsed}");
        }

        // ─── 3. If/Then/Else ───
        [UnityTest]
        public IEnumerator If_RoutesToCorrectBranch()
        {
            string thenResult = null, elseResult = null;
            var trueFlow = Flow.Create()
                .If(_ => true).Then(b => b.Do(_ => thenResult = "then"))
                                .Else(b => b.Do(_ => elseResult = "else"))
                .Build();
            yield return trueFlow.Run(_host).Wait();
            Assert.AreEqual("then", thenResult);
            Assert.IsNull(elseResult);

            thenResult = elseResult = null;
            var falseFlow = Flow.Create()
                .If(_ => false).Then(b => b.Do(_ => thenResult = "then"))
                                 .Else(b => b.Do(_ => elseResult = "else"))
                .Build();
            yield return falseFlow.Run(_host).Wait();
            Assert.IsNull(thenResult);
            Assert.AreEqual("else", elseResult);
        }

        // ─── 4. Repeat(N) ───
        [UnityTest]
        public IEnumerator Repeat_ExecutesExactCount()
        {
            int count = 0;
            yield return Flow.Create()
                .Repeat(3, b => b.Do(_ => count++))
                .Build()
                .Run(_host).Wait();
            Assert.AreEqual(3, count);
        }

        // ─── 5. While 直到条件达标 ───
        [UnityTest]
        public IEnumerator While_StopsWhenConditionFalse()
        {
            int count = 0;
            yield return Flow.Create()
                .While(_ => count < 5, b => b.Do(_ => count++))
                .Build()
                .Run(_host).Wait();
            Assert.AreEqual(5, count);
        }

        // ─── 6. ParallelAll: 总耗时 ≈ max(子分支) ───
        [UnityTest]
        public IEnumerator ParallelAll_WaitsForLongestBranch()
        {
            float start = Time.time;
            yield return Flow.Create()
                .ParallelAll(
                    b => b.Wait(0.3f),
                    b => b.Wait(0.5f))
                .Build()
                .Run(_host).Wait();
            float elapsed = Time.time - start;
            Assert.GreaterOrEqual(elapsed, 0.45f);
            Assert.Less(elapsed, 0.85f, $"too slow: {elapsed}");
        }

        // ─── 7. ParallelAny: 总耗时 ≈ min(子分支) ───
        [UnityTest]
        public IEnumerator ParallelAny_FinishesAtFirstBranch()
        {
            float start = Time.time;
            yield return Flow.Create()
                .ParallelAny(
                    b => b.Wait(0.3f),
                    b => b.Wait(1.0f))
                .Build()
                .Run(_host).Wait();
            float elapsed = Time.time - start;
            Assert.GreaterOrEqual(elapsed, 0.25f);
            Assert.Less(elapsed, 0.6f, $"should finish near 0.3s, was {elapsed}");
        }

        // ─── 8. WaitFor(KSignal) ───
        [UnityTest]
        public IEnumerator WaitForSignal_ResumesOnInvoke()
        {
            var sig = new KSignal();
            bool reached = false;
            var handle = Flow.Create()
                .WaitFor(sig)
                .Do(_ => reached = true)
                .Build()
                .Run(_host);

            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(reached);

            sig.Invoke();
            yield return handle.Wait();
            Assert.IsTrue(reached);
        }

        // ─── 9. WaitForEvent<T> ───
        [UnityTest]
        public IEnumerator WaitForEvent_ResumesOnPublish()
        {
            bool reached = false;
            var handle = Flow.Create()
                .WaitForEvent<TestEvent>()
                .Do(_ => reached = true)
                .Build()
                .Run(_host);

            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(reached);

            EventBus.Instance.Publish(new TestEvent { Value = 42 });
            yield return handle.Wait();
            Assert.IsTrue(reached);
        }

        // ─── 10. Cancel ───
        [UnityTest]
        public IEnumerator Cancel_StopsExecution()
        {
            bool reachedAfterCancel = false;
            var handle = Flow.Create()
                .Wait(0.5f)
                .Do(_ => reachedAfterCancel = true)
                .Build()
                .Run(_host);

            yield return new WaitForSeconds(0.1f);
            handle.Cancel();
            yield return handle.Wait();

            Assert.IsTrue(handle.IsCancelled);
            Assert.IsFalse(reachedAfterCancel);
        }

        // ─── 11. 异常处理 ───
        [UnityTest]
        public IEnumerator Exception_MarksHandleFailed()
        {
            // 抑制 Unity 控制台中的预期异常报错
            LogAssert.ignoreFailingMessages = true;

            var handle = Flow.Create()
                .Do(_ => throw new InvalidOperationException("boom"))
                .Build()
                .Run(_host);

            yield return handle.Wait();
            Assert.IsTrue(handle.IsFailed);
            Assert.IsNotNull(handle.Error);
            Assert.AreEqual("boom", handle.Error.Message);

            LogAssert.ignoreFailingMessages = false;
        }

        // ─── 12. SubFlow ───
        [UnityTest]
        public IEnumerator SubFlow_ExecutesNestedFlow()
        {
            int count = 0;
            var inner = Flow.Create()
                .Do(_ => count++)
                .Do(_ => count++)
                .Build();

            yield return Flow.Create()
                .Do(_ => count++)
                .SubFlow(inner)
                .Do(_ => count++)
                .Build()
                .Run(_host).Wait();

            Assert.AreEqual(4, count);
        }

        // ─── 13. FlowContext 类型化 Get/Set ───
        [UnityTest]
        public IEnumerator FlowContext_PassesValuesBetweenNodes()
        {
            string captured = null;
            yield return Flow.Create()
                .Do(ctx => ctx.Set("greeting", "hello"))
                .Do(ctx => ctx.Set(new TestPayload { Name = "world" }))
                .Do(ctx => captured = $"{ctx.Get<string>("greeting")} {ctx.Get<TestPayload>().Name}")
                .Build()
                .Run(_host).Wait();
            Assert.AreEqual("hello world", captured);
        }

        // ─── 测试辅助 ───

        private struct TestEvent : IEvent
        {
            public int Value;
        }

        private class TestPayload
        {
            public string Name;
        }
    }

    /// <summary>给测试用的空 MonoBehaviour 宿主。</summary>
    internal sealed class FlowTestHost : MonoBehaviour { }
}
