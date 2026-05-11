// Author: K-Framework Tests
// Date: 2026/05/11
// CSharpPool / ListPool / DictionaryPool / HashSetPool 纯 C# 测试集

using System.Collections.Generic;
using NUnit.Framework;

namespace KFramework
{
    /// <summary>
    /// CSharpPool（纯 C# 对象池）的测试集。
    /// 不依赖 Unity，可在 Editor 下直接运行。
    /// </summary>
    [TestFixture]
    public class ObjectPoolTests
    {
        // ─── 基础 Get/Release ───

        [Test]
        public void CSharpPool_Get_ReturnsNewInstance_WhenPoolEmpty()
        {
            var pool = new CSharpPool<TestPoolable>(() => new TestPoolable());

            var item = pool.Get();

            Assert.IsNotNull(item);
            Assert.IsTrue(item.IsGetCalled, "OnGet 回调应被调用");
        }

        [Test]
        public void CSharpPool_Release_ReturnsToPool_ThenGetReturnsSame()
        {
            var pool = new CSharpPool<TestPoolable>(() => new TestPoolable());
            var item = pool.Get();
            item.IsGetCalled = false;

            pool.Release(item);

            Assert.AreEqual(1, pool.CountInactive);

            var reused = pool.Get();
            Assert.AreSame(item, reused, "应复用已归还的对象");
        }

        [Test]
        public void CSharpPool_ReleaseNull_DoesNotThrow()
        {
            var pool = new CSharpPool<TestPoolable>(() => new TestPoolable());
            Assert.DoesNotThrow(() => pool.Release(null));
            Assert.AreEqual(0, pool.CountInactive);
        }

        // ─── 回调 ───

        [Test]
        public void CSharpPool_OnGet_CalledWhenInstanceTaken()
        {
            var pool = new CSharpPool<TestPoolable>(
                factory: () => new TestPoolable(),
                onGet: obj => obj.OnGetFlag = true);

            var item = pool.Get();
            Assert.IsTrue(item.OnGetFlag);
        }

        [Test]
        public void CSharpPool_OnRelease_CalledWhenInstanceReturned()
        {
            var pool = new CSharpPool<TestPoolable>(
                factory: () => new TestPoolable(),
                onRelease: obj => obj.OnReleaseFlag = true);

            var item = pool.Get();
            pool.Release(item);
            Assert.IsTrue(item.OnReleaseFlag);
        }

        [Test]
        public void CSharpPool_OnDestroy_CalledWhenPoolFull()
        {
            int destroyCalled = 0;
            var pool = new CSharpPool<TestPoolable>(
                factory: () => new TestPoolable(),
                onDestroy: _ => destroyCalled++,
                maxSize: 2);

            // 获取 3 个，归还 3 个 — 第 3 个触发 onDestroy
            var a = pool.Get();
            var b = pool.Get();
            var c = pool.Get();

            pool.Release(a);
            pool.Release(b);
            Assert.AreEqual(0, destroyCalled);

            pool.Release(c); // 池已满（容量 2），c 被销毁
            Assert.AreEqual(1, destroyCalled);
        }

        // ─── 容量 ───

        [Test]
        public void CSharpPool_MaxSize_ExcessIsDestroyed()
        {
            var pool = new CSharpPool<TestPoolable>(
                factory: () => new TestPoolable(),
                maxSize: 3);

            var items = new List<TestPoolable>();
            for (int i = 0; i < 5; i++)
                items.Add(pool.Get());

            foreach (var item in items)
                pool.Release(item);

            // 只有 3 个在池中
            Assert.AreEqual(3, pool.CountInactive);
        }

        [Test]
        public void CSharpPool_Preload_CreatesInstances()
        {
            var pool = new CSharpPool<TestPoolable>(
                factory: () => new TestPoolable(),
                maxSize: 10);

            pool.Preload(5);
            Assert.AreEqual(5, pool.CountInactive);
        }

        [Test]
        public void CSharpPool_Preload_ExceedsMaxSize_IsCapped()
        {
            var pool = new CSharpPool<TestPoolable>(
                factory: () => new TestPoolable(),
                maxSize: 5);

            pool.Preload(10);
            Assert.LessOrEqual(pool.CountInactive, 5);
        }

        // ─── Clear ───

        [Test]
        public void CSharpPool_Clear_EmptiesPoolAndCallsOnDestroy()
        {
            int destroyCalled = 0;
            var pool = new CSharpPool<TestPoolable>(
                factory: () => new TestPoolable(),
                onDestroy: _ => destroyCalled++,
                maxSize: 10);

            pool.Preload(5);
            Assert.AreEqual(5, pool.CountInactive);

            pool.Clear();
            Assert.AreEqual(0, pool.CountInactive);
            Assert.AreEqual(5, destroyCalled);
        }

        [Test]
        public void CSharpPool_Clear_NoOnDestroy_DoesNotThrow()
        {
            var pool = new CSharpPool<TestPoolable>(() => new TestPoolable());
            pool.Preload(3);
            Assert.DoesNotThrow(() => pool.Clear());
            Assert.AreEqual(0, pool.CountInactive);
        }

        // ─── Factory null ───

        [Test]
        public void CSharpPool_NullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CSharpPool<TestPoolable>(null));
        }

        // ─── Static Pools ───

        [Test]
        public void ListPool_Get_ReturnsEmptyList()
        {
            var list = ListPool<int>.Get();
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
            ListPool<int>.Release(list);
        }

        [Test]
        public void ListPool_Release_ClearsAndReuses()
        {
            var list = ListPool<int>.Get();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            ListPool<int>.Release(list);
            Assert.AreEqual(0, list.Count, "Release 后应清空");

            var list2 = ListPool<int>.Get();
            Assert.AreSame(list, list2, "应复用");
            ListPool<int>.Release(list2);
        }

        [Test]
        public void DictionaryPool_Get_ReturnsEmptyDictionary()
        {
            var dict = DictionaryPool<string, int>.Get();
            Assert.IsNotNull(dict);
            Assert.AreEqual(0, dict.Count);
            DictionaryPool<string, int>.Release(dict);
        }

        [Test]
        public void DictionaryPool_Release_ClearsAndReuses()
        {
            var dict = DictionaryPool<string, int>.Get();
            dict["a"] = 1;
            dict["b"] = 2;

            DictionaryPool<string, int>.Release(dict);
            Assert.AreEqual(0, dict.Count);

            var dict2 = DictionaryPool<string, int>.Get();
            Assert.AreSame(dict, dict2);
            DictionaryPool<string, int>.Release(dict2);
        }

        [Test]
        public void HashSetPool_Get_ReturnsEmptyHashSet()
        {
            var set = HashSetPool<int>.Get();
            Assert.IsNotNull(set);
            Assert.AreEqual(0, set.Count);
            HashSetPool<int>.Release(set);
        }

        [Test]
        public void HashSetPool_Release_ClearsAndReuses()
        {
            var set = HashSetPool<int>.Get();
            set.Add(1);
            set.Add(2);

            HashSetPool<int>.Release(set);
            Assert.AreEqual(0, set.Count);

            var set2 = HashSetPool<int>.Get();
            Assert.AreSame(set, set2);
            HashSetPool<int>.Release(set2);
        }

        // ─── 辅助类型 ───

        private class TestPoolable
        {
            public bool IsGetCalled;
            public bool OnGetFlag;
            public bool OnReleaseFlag;
        }
    }
}
