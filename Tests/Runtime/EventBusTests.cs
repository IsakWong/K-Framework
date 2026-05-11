// Author: K-Framework Tests
// Date: 2026/05/11
// EventBus PlayMode 测试集 — 全局类型路由事件总线

using System;
using NUnit.Framework;

namespace KFramework
{
    /// <summary>
    /// EventBus（全局事件总线）的纯 C# 测试集。
    /// 不依赖 MonoBehaviour 或 PlayMode。
    /// </summary>
    [TestFixture]
    public class EventBusTests
    {
        // ─── 测试事件定义 ───

        private struct TestEventA : IEvent
        {
            public int Value;
        }

        private struct TestEventB : IEvent
        {
            public string Message;
            public float Amount;
        }

        private struct EmptyEvent : IEvent { }

        [SetUp]
        public void Setup()
        {
            EventBus.Instance.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Instance.Clear();
        }

        // ─── 基础订阅/发布 ───

        [Test]
        public void Subscribe_Publish_DeliversEvent()
        {
            TestEventA received = default;
            EventBus.Instance.Subscribe<TestEventA>(e => received = e);

            EventBus.Instance.Publish(new TestEventA { Value = 42 });

            Assert.AreEqual(42, received.Value);
        }

        [Test]
        public void Subscribe_MultipleSubscribers_AllReceiveEvent()
        {
            int a = 0, b = 0;
            EventBus.Instance.Subscribe<TestEventA>(e => a = e.Value);
            EventBus.Instance.Subscribe<TestEventA>(e => b = e.Value);

            EventBus.Instance.Publish(new TestEventA { Value = 7 });

            Assert.AreEqual(7, a);
            Assert.AreEqual(7, b);
        }

        [Test]
        public void Publish_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                EventBus.Instance.Publish(new TestEventA { Value = 1 }));
        }

        [Test]
        public void Subscribe_DifferentEventTypes_AreIsolated()
        {
            int aVal = 0;
            string bMsg = null;

            EventBus.Instance.Subscribe<TestEventA>(e => aVal = e.Value);
            EventBus.Instance.Subscribe<TestEventB>(e => bMsg = e.Message);

            EventBus.Instance.Publish(new TestEventA { Value = 99 });

            Assert.AreEqual(99, aVal);
            Assert.IsNull(bMsg, "B 类型的订阅者不应收到 A 事件");
        }

        // ─── 退订 ───

        [Test]
        public void Unsubscribe_StopsReceiving()
        {
            int count = 0;
            Action<TestEventA> handler = e => count++;
            EventBus.Instance.Subscribe(handler);

            EventBus.Instance.Publish(new TestEventA());
            EventBus.Instance.Unsubscribe(handler);
            EventBus.Instance.Publish(new TestEventA());

            Assert.AreEqual(1, count, "退订后不应再收到事件");
        }

        [Test]
        public void UnsubscribeAll_RemovesAllSubscribers()
        {
            int count = 0;
            EventBus.Instance.Subscribe<TestEventA>(e => count++);
            EventBus.Instance.Subscribe<TestEventA>(e => count++);

            EventBus.Instance.UnsubscribeAll<TestEventA>();
            EventBus.Instance.Publish(new TestEventA());

            Assert.AreEqual(0, count);
        }

        // ─── 一次性订阅 ───

        [Test]
        public void SubscribeOnce_FiresOnlyOnce()
        {
            int count = 0;
            EventBus.Instance.SubscribeOnce<TestEventA>(e => count++);

            EventBus.Instance.Publish(new TestEventA());
            EventBus.Instance.Publish(new TestEventA());
            EventBus.Instance.Publish(new TestEventA());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void SubscribeOnce_MixedWithRegular_RegularStillFires()
        {
            int onceCount = 0, regularCount = 0;
            EventBus.Instance.SubscribeOnce<TestEventA>(e => onceCount++);
            EventBus.Instance.Subscribe<TestEventA>(e => regularCount++);

            EventBus.Instance.Publish(new TestEventA());
            EventBus.Instance.Publish(new TestEventA());

            Assert.AreEqual(1, onceCount);
            Assert.AreEqual(2, regularCount);
        }

        // ─── 优先级 ───

        [Test]
        public void Priority_LowerNumberExecutesFirst()
        {
            var order = new System.Collections.Generic.List<int>();
            EventBus.Instance.Subscribe<TestEventA>(e => order.Add(2), priority: 2);
            EventBus.Instance.Subscribe<TestEventA>(e => order.Add(0), priority: 0);
            EventBus.Instance.Subscribe<TestEventA>(e => order.Add(1), priority: 1);

            EventBus.Instance.Publish(new TestEventA());

            Assert.AreEqual(3, order.Count);
            Assert.AreEqual(0, order[0], "priority=0 应先执行");
            Assert.AreEqual(1, order[1], "priority=1 应第二执行");
            Assert.AreEqual(2, order[2], "priority=2 应最后执行");
        }

        [Test]
        public void Priority_DefaultIsZero()
        {
            var order = new System.Collections.Generic.List<int>();
            EventBus.Instance.Subscribe<TestEventA>(e => order.Add(2), priority: 2);
            EventBus.Instance.Subscribe<TestEventA>(e => order.Add(0)); // 默认 priority=0

            EventBus.Instance.Publish(new TestEventA());

            Assert.AreEqual(2, order.Count);
            Assert.AreEqual(0, order[0]);
        }

        // ─── 粘性事件 ───

        [Test]
        public void Sticky_NewSubscriberGetsLastEvent()
        {
            EventBus.Instance.PublishSticky(new TestEventA { Value = 77 });

            int received = 0;
            EventBus.Instance.SubscribeSticky<TestEventA>(e => received = e.Value);

            Assert.AreEqual(77, received, "粘性订阅应收到缓存值");
        }

        [Test]
        public void Sticky_SubscriberAlsoGetsFutureEvents()
        {
            EventBus.Instance.PublishSticky(new TestEventA { Value = 10 });

            int received = 0;
            EventBus.Instance.SubscribeSticky<TestEventA>(e => received = e.Value);
            Assert.AreEqual(10, received);

            EventBus.Instance.Publish(new TestEventA { Value = 20 });
            Assert.AreEqual(20, received);
        }

        [Test]
        public void Sticky_TryGetSticky_ReturnsCached()
        {
            Assert.IsFalse(EventBus.Instance.TryGetSticky<TestEventA>(out _));

            EventBus.Instance.PublishSticky(new TestEventA { Value = 55 });

            Assert.IsTrue(EventBus.Instance.TryGetSticky<TestEventA>(out var evt));
            Assert.AreEqual(55, evt.Value);
        }

        [Test]
        public void Sticky_RemoveSticky_ClearsCache()
        {
            EventBus.Instance.PublishSticky(new TestEventA { Value = 1 });
            Assert.IsTrue(EventBus.Instance.TryGetSticky<TestEventA>(out _));

            EventBus.Instance.RemoveSticky<TestEventA>();
            Assert.IsFalse(EventBus.Instance.TryGetSticky<TestEventA>(out _));
        }

        [Test]
        public void Sticky_OverwriteReplacesCache()
        {
            EventBus.Instance.PublishSticky(new TestEventA { Value = 1 });
            EventBus.Instance.PublishSticky(new TestEventA { Value = 2 });

            int received = 0;
            EventBus.Instance.SubscribeSticky<TestEventA>(e => received = e.Value);
            Assert.AreEqual(2, received, "应返回最后一次粘性值");
        }

        // ─── Subscriber 集成 ───

        [Test]
        public void Subscriber_DisconnectAll_UnsubscribesFromEventBus()
        {
            var sub = new Subscriber();
            int count = 0;

            EventBus.Instance.Subscribe<TestEventA>(sub, e => count++);
            EventBus.Instance.Publish(new TestEventA());
            Assert.AreEqual(1, count);

            sub.DisconnectAll();
            EventBus.Instance.Publish(new TestEventA());
            Assert.AreEqual(1, count, "DisconnectAll 应退订 EventBus");
        }

        [Test]
        public void Subscriber_Sticky_DisconnectAllCleansUp()
        {
            EventBus.Instance.PublishSticky(new TestEventA { Value = 5 });

            var sub = new Subscriber();
            int count = 0;
            EventBus.Instance.SubscribeSticky<TestEventA>(sub, e => count++);
            Assert.AreEqual(1, count);

            sub.DisconnectAll();
            EventBus.Instance.Publish(new TestEventA());
            Assert.AreEqual(1, count);
        }

        // ─── 查询 ───

        [Test]
        public void GetSubscriberCount_ReturnsCorrectCount()
        {
            Assert.AreEqual(0, EventBus.Instance.GetSubscriberCount<TestEventA>());

            EventBus.Instance.Subscribe<TestEventA>(e => { });
            Assert.AreEqual(1, EventBus.Instance.GetSubscriberCount<TestEventA>());

            EventBus.Instance.Subscribe<TestEventA>(e => { });
            Assert.AreEqual(2, EventBus.Instance.GetSubscriberCount<TestEventA>());

            EventBus.Instance.UnsubscribeAll<TestEventA>();
            Assert.AreEqual(0, EventBus.Instance.GetSubscriberCount<TestEventA>());
        }

        [Test]
        public void HasSubscribers_ReturnsCorrectBool()
        {
            Assert.IsFalse(EventBus.Instance.HasSubscribers<TestEventA>());

            EventBus.Instance.Subscribe<TestEventA>(e => { });
            Assert.IsTrue(EventBus.Instance.HasSubscribers<TestEventA>());

            EventBus.Instance.UnsubscribeAll<TestEventA>();
            Assert.IsFalse(EventBus.Instance.HasSubscribers<TestEventA>());
        }

        // ─── Clear ───

        [Test]
        public void Clear_RemovesAllSubscriptionsAndSticky()
        {
            EventBus.Instance.Subscribe<TestEventA>(e => { });
            EventBus.Instance.Subscribe<TestEventB>(e => { });
            EventBus.Instance.PublishSticky(new TestEventA { Value = 1 });

            EventBus.Instance.Clear();

            Assert.AreEqual(0, EventBus.Instance.GetSubscriberCount<TestEventA>());
            Assert.AreEqual(0, EventBus.Instance.GetSubscriberCount<TestEventB>());
            Assert.IsFalse(EventBus.Instance.TryGetSticky<TestEventA>(out _));
        }

        // ─── 边界 ───

        [Test]
        public void Publish_ExceptionInHandler_IsCaught()
        {
            // 测试异常不会中断其他订阅者
            int secondCalled = 0;

            EventBus.Instance.Subscribe<TestEventA>(e => throw new InvalidOperationException("boom"));
            EventBus.Instance.Subscribe<TestEventA>(e => secondCalled = e.Value);

            // EventBus 内部用 try-catch 包裹，不应抛出
            Assert.DoesNotThrow(() =>
                EventBus.Instance.Publish(new TestEventA { Value = 42 }));

            Assert.AreEqual(42, secondCalled, "异常处理器不应影响其他订阅者");
        }

        [Test]
        public void Subscribe_DuplicateHandler_IsCalledMultipleTimes()
        {
            int count = 0;
            Action<TestEventA> handler = e => count++;

            EventBus.Instance.Subscribe(handler);
            EventBus.Instance.Subscribe(handler);

            EventBus.Instance.Publish(new TestEventA());

            // 同一 handler 重复订阅会触发多次（当前实现允许）
            Assert.AreEqual(2, count);
        }
    }
}
