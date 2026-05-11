// Author: K-Framework Tests
// Date: 2026/05/11
// ServiceLocator 纯 C# 测试集

using System;
using NUnit.Framework;

namespace KFramework
{
    /// <summary>
    /// ServiceLocator（服务定位器）的纯 C# 测试集。
    /// 测试注册、获取、安全获取、注销、重置等。
    /// </summary>
    [TestFixture]
    public class ServiceLocatorTests
    {
        // ─── 测试接口和实现 ───

        private interface ITestService
        {
            string Name { get; }
        }

        private class TestServiceImpl : ITestService
        {
            public string Name { get; set; } = "default";
        }

        private interface IAnotherService
        {
            int Value { get; }
        }

        private class AnotherServiceImpl : IAnotherService
        {
            public int Value { get; set; } = 42;
        }

        [SetUp]
        public void Setup()
        {
            ServiceLocator.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Reset();
        }

        // ─── 注册 ───

        [Test]
        public void Register_Get_ReturnsSameInstance()
        {
            var svc = new TestServiceImpl { Name = "hello" };
            ServiceLocator.Register<ITestService>(svc);

            var resolved = ServiceLocator.Get<ITestService>();

            Assert.AreSame(svc, resolved);
            Assert.AreEqual("hello", resolved.Name);
        }

        [Test]
        public void Register_GenericAndTypeRegistration_BothWork()
        {
            var svc1 = new TestServiceImpl();
            var svc2 = new AnotherServiceImpl();

            ServiceLocator.Register<ITestService>(svc1);
            ServiceLocator.Register(typeof(IAnotherService), svc2);

            Assert.AreSame(svc1, ServiceLocator.Get<ITestService>());
            Assert.AreSame(svc2, ServiceLocator.Get<IAnotherService>());
        }

        [Test]
        public void Register_NullService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ServiceLocator.Register<ITestService>(null));
        }

        [Test]
        public void Register_Overwrite_ReplacesExisting()
        {
            var svc1 = new TestServiceImpl { Name = "first" };
            var svc2 = new TestServiceImpl { Name = "second" };

            ServiceLocator.Register<ITestService>(svc1);
            // 覆盖会输出警告，但不抛异常
            ServiceLocator.Register<ITestService>(svc2);

            Assert.AreSame(svc2, ServiceLocator.Get<ITestService>());
        }

        // ─── 获取 ───

        [Test]
        public void Get_NotRegistered_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ServiceLocator.Get<ITestService>());
        }

        [Test]
        public void TryGet_NotRegistered_ReturnsFalse()
        {
            Assert.IsFalse(ServiceLocator.TryGet<ITestService>(out var svc));
            Assert.IsNull(svc);
        }

        [Test]
        public void TryGet_Registered_ReturnsTrueAndService()
        {
            var svc = new TestServiceImpl();
            ServiceLocator.Register<ITestService>(svc);

            Assert.IsTrue(ServiceLocator.TryGet<ITestService>(out var resolved));
            Assert.AreSame(svc, resolved);
        }

        [Test]
        public void GetOrDefault_NotRegistered_ReturnsNull()
        {
            Assert.IsNull(ServiceLocator.GetOrDefault<ITestService>());
        }

        [Test]
        public void GetOrDefault_Registered_ReturnsService()
        {
            var svc = new TestServiceImpl();
            ServiceLocator.Register<ITestService>(svc);

            Assert.AreSame(svc, ServiceLocator.GetOrDefault<ITestService>());
        }

        // ─── 查询 ───

        [Test]
        public void IsRegistered_ReturnsCorrectStatus()
        {
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());

            ServiceLocator.Register<ITestService>(new TestServiceImpl());
            Assert.IsTrue(ServiceLocator.IsRegistered<ITestService>());

            Assert.IsFalse(ServiceLocator.IsRegistered(typeof(IAnotherService)));
        }

        [Test]
        public void Count_ReflectsRegisteredServices()
        {
            Assert.AreEqual(0, ServiceLocator.Count);

            ServiceLocator.Register<ITestService>(new TestServiceImpl());
            Assert.AreEqual(1, ServiceLocator.Count);

            ServiceLocator.Register<IAnotherService>(new AnotherServiceImpl());
            Assert.AreEqual(2, ServiceLocator.Count);
        }

        // ─── 注销 ───

        [Test]
        public void Unregister_RemovesService()
        {
            ServiceLocator.Register<ITestService>(new TestServiceImpl());
            Assert.IsTrue(ServiceLocator.IsRegistered<ITestService>());

            Assert.IsTrue(ServiceLocator.Unregister<ITestService>());
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
        }

        [Test]
        public void Unregister_NotRegistered_ReturnsFalse()
        {
            Assert.IsFalse(ServiceLocator.Unregister<ITestService>());
        }

        [Test]
        public void Unregister_ByType_RemovesService()
        {
            ServiceLocator.Register<IAnotherService>(new AnotherServiceImpl());

            Assert.IsTrue(ServiceLocator.Unregister(typeof(IAnotherService)));
            Assert.IsFalse(ServiceLocator.IsRegistered(typeof(IAnotherService)));
        }

        // ─── Reset ───

        [Test]
        public void Reset_ClearsAllServices()
        {
            ServiceLocator.Register<ITestService>(new TestServiceImpl());
            ServiceLocator.Register<IAnotherService>(new AnotherServiceImpl());
            Assert.AreEqual(2, ServiceLocator.Count);

            ServiceLocator.Reset();
            Assert.AreEqual(0, ServiceLocator.Count);
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
            Assert.IsFalse(ServiceLocator.IsRegistered<IAnotherService>());
        }


    }
}
