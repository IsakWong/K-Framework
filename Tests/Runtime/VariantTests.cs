// Author: K-Framework Tests
// Date: 2026/05/11
// Variant 类型系统纯 C# 测试集

using NUnit.Framework;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// Variant（通用数据类型容器）的纯 C# 测试集。
    /// 测试构造、Set/Get、隐式转换、类型转换等。
    /// </summary>
    [TestFixture]
    public class VariantTests
    {
        // ─── 构造 ───

        [Test]
        public void Variant_Default_IsNone()
        {
            var v = new Variant();
            Assert.AreEqual(VariantType.None, v.mType);
        }

        [Test]
        public void Variant_IntConstructor_SetsTypeAndValue()
        {
            var v = new Variant(42);
            Assert.AreEqual(VariantType.Int, v.mType);
            Assert.AreEqual(42, v.Get<int>());
        }

        [Test]
        public void Variant_FloatConstructor_SetsTypeAndValue()
        {
            var v = new Variant(3.14f);
            Assert.AreEqual(VariantType.Float, v.mType);
            Assert.AreEqual(3.14f, v.Get<float>(), 0.001f);
        }

        [Test]
        public void Variant_BoolConstructor_SetsTypeAndValue()
        {
            var v = new Variant(true);
            Assert.AreEqual(VariantType.Bool, v.mType);
            Assert.IsTrue(v.Get<bool>());
        }

        [Test]
        public void Variant_StringConstructor_SetsTypeAndValue()
        {
            var v = new Variant("hello world");
            Assert.AreEqual(VariantType.String, v.mType);
            Assert.AreEqual("hello world", v.Get<string>());
        }

        [Test]
        public void Variant_DoubleConstructor_SetsTypeAndValue()
        {
            var v = new Variant(2.718);
            Assert.AreEqual(VariantType.Double, v.mType);
            Assert.AreEqual(2.718, v.Get<double>(), 0.001);
        }

        [Test]
        public void Variant_Vector2Constructor_SetsTypeAndValue()
        {
            var v = new Variant(new Vector2(1f, 2f));
            Assert.AreEqual(VariantType.Vector2, v.mType);
            Assert.AreEqual(new Vector2(1f, 2f), v.Get<Vector2>());
        }

        [Test]
        public void Variant_Vector3Constructor_SetsTypeAndValue()
        {
            var v = new Variant(new Vector3(1f, 2f, 3f));
            Assert.AreEqual(VariantType.Vector3, v.mType);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), v.Get<Vector3>());
        }

        [Test]
        public void Variant_Vector4Constructor_SetsTypeAndValue()
        {
            var v = new Variant(new Vector4(1f, 2f, 3f, 4f));
            Assert.AreEqual(VariantType.Vector4, v.mType);
            Assert.AreEqual(new Vector4(1f, 2f, 3f, 4f), v.Get<Vector4>());
        }

        // ─── Set / Get ───

        [Test]
        public void Variant_Set_UpdatesTypeAndValue()
        {
            var v = new Variant();
            v.Set(10);
            Assert.AreEqual(VariantType.Int, v.mType);
            Assert.AreEqual(10, v.Get<int>());

            v.Set("changed");
            Assert.AreEqual(VariantType.String, v.mType);
            Assert.AreEqual("changed", v.Get<string>());
        }

        [Test]
        public void Variant_Set_Vector3()
        {
            var v = new Variant();
            v.Set(new Vector3(1f, 2f, 3f));
            var result = v.Get<Vector3>();
            Assert.AreEqual(1f, result.x);
            Assert.AreEqual(2f, result.y);
            Assert.AreEqual(3f, result.z);
        }

        [Test]
        public void Variant_Set_Vector2()
        {
            var v = new Variant();
            v.Set(new Vector2(4f, 5f));
            Assert.AreEqual(new Vector2(4f, 5f), v.Get<Vector2>());
        }

        [Test]
        public void Variant_Get_WrongType_ReturnsDefault()
        {
            var v = new Variant("string");
            Assert.AreEqual(0, v.Get<int>());
            Assert.AreEqual(0f, v.Get<float>());
            Assert.AreEqual(default(Vector3), v.Get<Vector3>());
        }

        [Test]
        public void Variant_Get_WithDefault_ReturnsDefaultOnMismatch()
        {
            var v = new Variant(42);
            Assert.AreEqual("fallback", v.Get<string>("fallback"));
        }

        [Test]
        public void Variant_GetRaw_ReturnsCorrectValue()
        {
            var v = new Variant(99);
            Assert.AreEqual(99, v.GetRaw());
        }

        // ─── Copy 构造 ───

        [Test]
        public void Variant_CopyConstructor_CopiesCorrectly()
        {
            var original = new Variant(42);
            var copy = new Variant(original);

            Assert.AreEqual(VariantType.Int, copy.mType);
            Assert.AreEqual(42, copy.Get<int>());
        }

        [Test]
        public void Variant_SetVariant_CopiesFromOther()
        {
            var v1 = new Variant("test");
            var v2 = new Variant();

            v2.SetVariant(v1);
            Assert.AreEqual("test", v2.Get<string>());
        }

        // ─── VariantTypeHelper ───

        [Test]
        public void ConvertToVariantType_Int_ReturnsInt()
        {
            Assert.AreEqual(VariantType.Int, typeof(int).ConvertToVariantType());
        }

        [Test]
        public void ConvertToVariantType_Float_ReturnsFloat()
        {
            Assert.AreEqual(VariantType.Float, typeof(float).ConvertToVariantType());
        }

        [Test]
        public void ConvertToVariantType_String_ReturnsString()
        {
            Assert.AreEqual(VariantType.String, typeof(string).ConvertToVariantType());
        }

        [Test]
        public void ConvertToVariantType_Vector3_ReturnsVector3()
        {
            Assert.AreEqual(VariantType.Vector3, typeof(Vector3).ConvertToVariantType());
        }

        [Test]
        public void ConvertToVariantType_Unknown_ReturnsNone()
        {
            Assert.AreEqual(VariantType.None, typeof(System.Guid).ConvertToVariantType());
        }

        // ─── VariantRef ───

        [Test]
        public void VariantRef_SetAndGet_Works()
        {
            var vref = new VariantRef<int>(42);
            Assert.AreEqual(42, vref.As());

            vref.varaint.Set(100);
            Assert.AreEqual(100, vref.As());
        }

        [Test]
        public void VariantRef_ImplicitOperator_Works()
        {
            var vref = new VariantRef<float>(3.14f);
            float value = vref;
            Assert.AreEqual(3.14f, value, 0.001f);
        }

        [Test]
        public void VariantRef_GetVariant_ReturnsVariant()
        {
            var vref = new VariantRef<string>("hello");
            var v = vref.GetVariant();
            Assert.AreEqual("hello", v.Get<string>());
        }
    }
}
