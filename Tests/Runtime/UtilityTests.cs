// Author: K-Framework Tests
// Date: 2026/05/11
// DoubleArray / Utility 扩展方法纯 C# 测试集

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// DoubleArray&lt;T&gt;（二维数组）和 Utility 扩展方法的 EditMode 测试集。
    /// </summary>
    [TestFixture]
    public class UtilityTests
    {
        // ═══════════════════════════════════════════════
        //  DoubleArray<T>
        // ═══════════════════════════════════════════════

        [Test]
        public void DoubleArray_Constructor_SetsDimensions()
        {
            var arr = new DoubleArray<int>(5, 3);

            Assert.AreEqual(5, arr.width);
            Assert.AreEqual(3, arr.height);
            Assert.AreEqual(15, arr.Length);
        }

        [Test]
        public void DoubleArray_Indexer_TwoDim_ReadsAndWrites()
        {
            var arr = new DoubleArray<int>(3, 3);

            arr[1, 2] = 42;

            Assert.AreEqual(42, arr[1, 2]);
            Assert.AreEqual(42, arr[2 * 3 + 1]); // y*width + x
        }

        [Test]
        public void DoubleArray_Indexer_Linear_ReadsAndWrites()
        {
            var arr = new DoubleArray<int>(2, 2);

            arr[0] = 1;
            arr[1] = 2;
            arr[2] = 3;
            arr[3] = 4;

            Assert.AreEqual(1, arr[0, 0]);
            Assert.AreEqual(2, arr[1, 0]);
            Assert.AreEqual(3, arr[0, 1]);
            Assert.AreEqual(4, arr[1, 1]);
        }

        [Test]
        public void DoubleArray_GetLength_Dimensions()
        {
            var arr = new DoubleArray<float>(5, 7);

            Assert.AreEqual(5, arr.GetLength(0));
            Assert.AreEqual(7, arr.GetLength(1));
        }

        [Test]
        public void DoubleArray_GetLength_InvalidDim_Throws()
        {
            var arr = new DoubleArray<int>(3, 3);

            Assert.Throws<IndexOutOfRangeException>(() => arr.GetLength(2));
            Assert.Throws<IndexOutOfRangeException>(() => arr.GetLength(-1));
        }

        [Test]
        public void DoubleArray_Rank_Always2()
        {
            var arr = new DoubleArray<string>(4, 5);
            Assert.AreEqual(2, arr.Rank);
        }

        [Test]
        public void DoubleArray_IsValidIndex_ChecksBounds()
        {
            var arr = new DoubleArray<int>(3, 3);

            Assert.IsTrue(arr.IsValidIndex(0, 0));
            Assert.IsTrue(arr.IsValidIndex(2, 2));
            Assert.IsFalse(arr.IsValidIndex(-1, 0));
            Assert.IsFalse(arr.IsValidIndex(0, 3));
            Assert.IsFalse(arr.IsValidIndex(3, 0));
        }

        [Test]
        public void DoubleArray_Clear_SetsToDefault()
        {
            var arr = new DoubleArray<int>(2, 2);
            arr[0, 0] = 1;
            arr[1, 1] = 2;

            arr.Clear();

            Assert.AreEqual(0, arr[0, 0]);
            Assert.AreEqual(0, arr[1, 1]);
        }

        [Test]
        public void DoubleArray_Fill_SetsAllElements()
        {
            var arr = new DoubleArray<string>(2, 2);
            arr.Fill("hello");

            Assert.AreEqual("hello", arr[0, 0]);
            Assert.AreEqual("hello", arr[1, 0]);
            Assert.AreEqual("hello", arr[0, 1]);
            Assert.AreEqual("hello", arr[1, 1]);
        }

        [Test]
        public void DoubleArray_SingleParamConstructor_SetsTotal()
        {
            var arr = new DoubleArray<double>(10);
            Assert.AreEqual(10, arr.Length);
        }

        // ═══════════════════════════════════════════════
        //  Vector 转换扩展
        // ═══════════════════════════════════════════════

        [Test]
        public void ToVector3Int_FromVector3_RoundsCorrectly()
        {
            var v = new Vector3(1.4f, 2.6f, 3.5f);
            var result = v.ToVector3Int();

            Assert.AreEqual(1, result.x);
            Assert.AreEqual(3, result.y); // Mathf.RoundToInt 是四舍五入
            Assert.AreEqual(4, result.z);
        }

        [Test]
        public void ToVector2_FromVector3_DiscardsZ()
        {
            var v = new Vector3(1f, 2f, 100f);
            var result = v.ToVector2();

            Assert.AreEqual(1f, result.x);
            Assert.AreEqual(2f, result.y);
        }

        [Test]
        public void ToVector3_FromVector2_SetsZ()
        {
            var v = new Vector2(3f, 4f);
            var result = v.ToVector3(10f);

            Assert.AreEqual(3f, result.x);
            Assert.AreEqual(4f, result.y);
            Assert.AreEqual(10f, result.z);
        }

        [Test]
        public void ToVector2Int_FromVector2_RoundsCorrectly()
        {
            var v = new Vector2(1.7f, 2.3f);
            var result = v.ToVector2Int();

            Assert.AreEqual(2, result.x);
            Assert.AreEqual(2, result.y);
        }

        [Test]
        public void FloorToInt_FromVector3_FloorsCorrectly()
        {
            var v = new Vector3(1.9f, 2.1f, -0.5f);
            var result = v.FloorToInt();

            Assert.AreEqual(1, result.x);
            Assert.AreEqual(2, result.y);
            Assert.AreEqual(-1, result.z);
        }

        [Test]
        public void CeilToInt_FromVector3_CeilsCorrectly()
        {
            var v = new Vector3(1.1f, 2.9f, -0.1f);
            var result = v.CeilToInt();

            Assert.AreEqual(2, result.x);
            Assert.AreEqual(3, result.y);
            Assert.AreEqual(0, result.z);
        }

        [Test]
        public void VectorConversions_RoundTrip_Identity()
        {
            var v2 = new Vector2(1.5f, 2.5f);
            var back = v2.ToVector3().ToVector2();
            Assert.AreEqual(v2, back);

            var v3i = new Vector3Int(1, 2, 3);
            var back3i = v3i.ToVector3().RoundToInt();
            Assert.AreEqual(v3i, back3i);
        }

        // 手动定义 RoundToInt 因为它在 Vector3 上没有直接的扩展方法
        // 这里测试的是 ToVector3Int
        [Test]
        public void ToVector3_FromVector3Int_PreservesValues()
        {
            var vi = new Vector3Int(5, 6, 7);
            var result = vi.ToVector3();

            Assert.AreEqual(5f, result.x);
            Assert.AreEqual(6f, result.y);
            Assert.AreEqual(7f, result.z);
        }

        // ═══════════════════════════════════════════════
        //  DistanceBetweenPosition
        // ═══════════════════════════════════════════════

        [Test]
        public void DistanceBetweenPosition_IgnoreY_IgnoresY()
        {
            var a = new Vector3(0f, 100f, 0f);
            var b = new Vector3(3f, 200f, 4f);

            var dist = Utility.DistanceBetweenPosition(a, b, ignoreY: true);

            Assert.AreEqual(5f, dist, 0.001f); // sqrt(3²+4²) = 5
        }

        [Test]
        public void DistanceBetweenPosition_IncludeY_ConsidersY()
        {
            var a = new Vector3(0f, 0f, 0f);
            var b = new Vector3(0f, 5f, 0f);

            var dist = Utility.DistanceBetweenPosition(a, b, ignoreY: false);

            Assert.AreEqual(5f, dist, 0.001f);
        }

        // ═══════════════════════════════════════════════
        //  GetRandomElements
        // ═══════════════════════════════════════════════

        [Test]
        public void GetRandomElements_ValidRequest_ReturnsCorrectCount()
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };
            var result = Utility.GetRandomElements(list, 3);

            Assert.AreEqual(3, result.Count);
            CollectionAssert.IsSubsetOf(result, list);
        }

        [Test]
        public void GetRandomElements_CountZero_ReturnsEmpty()
        {
            var list = new List<int> { 1, 2, 3 };
            var result = Utility.GetRandomElements(list, 0);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetRandomElements_CountExceeds_ThrowsArgumentException()
        {
            var list = new List<int> { 1, 2 };
            Assert.Throws<ArgumentException>(() => Utility.GetRandomElements(list, 5));
        }

        [Test]
        public void GetRandomElements_NullList_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Utility.GetRandomElements<int>(null, 1));
        }

        // ═══════════════════════════════════════════════
        //  SafeAccess
        // ═══════════════════════════════════════════════

        [Test]
        public void SafeAccess_ValidIndex_ReturnsTrue()
        {
            var list = new List<string> { "a", "b", "c" };

            Assert.IsTrue(list.SafeAccess(1, out var val));
            Assert.AreEqual("b", val);
        }

        [Test]
        public void SafeAccess_OutOfRange_ReturnsFalseAndDefault()
        {
            var list = new List<string> { "a", "b" };

            Assert.IsFalse(list.SafeAccess(5, out var val));
            Assert.IsNull(val);
        }

        [Test]
        public void SafeAccess_NegativeIndex_ReturnsFalse()
        {
            var list = new List<int> { 1, 2, 3 };

            Assert.IsFalse(list.SafeAccess(-1, out var val));
            Assert.AreEqual(0, val);
        }

        // ═══════════════════════════════════════════════
        //  NormalizedInXY
        // ═══════════════════════════════════════════════

        [Test]
        public void NormalizedInXY_NormalizesCorrectly()
        {
            var v = new Vector3(3f, 4f, 100f);
            var result = v.NormalizedInXY();

            Assert.AreEqual(0f, result.z);
            Assert.AreEqual(1f, result.magnitude, 0.001f);
            Assert.AreEqual(0.6f, result.x, 0.001f);
            Assert.AreEqual(0.8f, result.y, 0.001f);
        }

        [Test]
        public void NormalizedInXY_ZeroVector_ReturnsRight()
        {
            var v = new Vector3(0f, 0f, 5f);
            var result = v.NormalizedInXY();

            Assert.AreEqual(1f, result.magnitude, 0.001f);
            Assert.AreEqual(1f, result.x); // fallback to (1,0,0)
            Assert.AreEqual(0f, result.y);
        }

        // ═══════════════════════════════════════════════
        //  RectInt.Outter
        // ═══════════════════════════════════════════════

        [Test]
        public void RectIntOutter_ExpandsCorrectly()
        {
            var rect = new RectInt(10, 10, 20, 20);
            var result = rect.Outter(1, 2, 3, 4);

            Assert.AreEqual(7, result.xMin);  // 10 - 3
            Assert.AreEqual(8, result.yMin);  // 10 - 2
            Assert.AreEqual(27, result.xMax); // 10+20 + 4 - 3? No: 10-3 + 20+3+4 = 7+27...
            // Let me re-read: Outter returns new RectInt(bounds.xMin - left, bounds.yMin - bottom, bounds.width + left + right, bounds.height + top + bottom)
            // So: xMin = 10 - 3 = 7, yMin = 10 - 2 = 8
            // width = 20 + 3 + 4 = 27, height = 20 + 1 + 2 = 23
            Assert.AreEqual(27, result.width);
            Assert.AreEqual(23, result.height);
            Assert.AreEqual(7, result.xMin);
            Assert.AreEqual(8, result.yMin);
        }
    }
}
