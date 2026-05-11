// Author: K-Framework Tests
// Date: 2026/05/11
// MathExtension 纯 C# 测试集

using Framework.Foundation;
using NUnit.Framework;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// MathExtension 数学工具函数的 EditMode 测试集。
    /// </summary>
    [TestFixture]
    public class MathExtensionTests
    {
        // ─── NormalizeAngle ───

        [Test]
        public void NormalizeAngle_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, MathExtension.NormalizeAngle(0f));
        }

        [Test]
        public void NormalizeAngle_PositiveOverflow_Modulo360()
        {
            Assert.AreEqual(90f, MathExtension.NormalizeAngle(450f), 0.001f);
            Assert.AreEqual(0f, MathExtension.NormalizeAngle(720f), 0.001f);
        }

        [Test]
        public void NormalizeAngle_Negative_Modulo360()
        {
            Assert.AreEqual(270f, MathExtension.NormalizeAngle(-90f), 0.001f);
            Assert.AreEqual(0f, MathExtension.NormalizeAngle(-360f), 0.001f);
        }

        // ─── GetAngleInXZ ───

        [Test]
        public void GetAngleInXZ_Forward_ReturnsZero()
        {
            Assert.AreEqual(0f, MathExtension.GetAngleInXZ(Vector3.forward), 0.001f);
        }

        [Test]
        public void GetAngleInXZ_Right_Returns90()
        {
            Assert.AreEqual(90f, MathExtension.GetAngleInXZ(Vector3.right), 0.001f);
        }

        [Test]
        public void GetAngleInXZ_Backward_Returns180()
        {
            Assert.AreEqual(180f, MathExtension.GetAngleInXZ(Vector3.back), 0.001f);
        }

        [Test]
        public void GetAngleInXZ_Left_Returns270()
        {
            Assert.AreEqual(270f, MathExtension.GetAngleInXZ(Vector3.left), 0.001f);
        }

        [Test]
        public void GetAngleInXZ_ZeroVector_ReturnsZero()
        {
            Assert.AreEqual(0f, MathExtension.GetAngleInXZ(Vector3.zero), 0.001f);
        }

        [Test]
        public void GetAngleInXZ_IgnoresYComponent()
        {
            var dir = new Vector3(1f, 100f, 0f);
            Assert.AreEqual(90f, MathExtension.GetAngleInXZ(dir), 0.001f);
        }

        // ─── ClampAngle ───

        [Test]
        public void ClampAngle_WithinRange_Unchanged()
        {
            Assert.AreEqual(50f, MathExtension.ClampAngle(50f, 0f, 100f), 0.001f);
        }

        [Test]
        public void ClampAngle_BelowMin_ClampedToClosest()
        {
            Assert.AreEqual(0f, MathExtension.ClampAngle(-10f, 0f, 100f), 0.001f);
        }

        [Test]
        public void ClampAngle_AboveMax_ClampedToClosest()
        {
            Assert.AreEqual(100f, MathExtension.ClampAngle(200f, 0f, 100f), 0.001f);
        }

        [Test]
        public void ClampAngle_WrapAroundRange_HandlesCorrectly()
        {
            // 跨 0 度的范围: [350, 10]
            Assert.AreEqual(350f, MathExtension.ClampAngle(300f, 350f, 10f), 0.001f);
            Assert.AreEqual(5f, MathExtension.ClampAngle(5f, 350f, 10f), 0.001f);
        }

        // ─── QuadraticBezierPoint ───

        [Test]
        public void QuadraticBezierPoint_T0_ReturnsP0()
        {
            var p0 = Vector3.zero;
            var p1 = new Vector3(1f, 2f, 0f);
            var p2 = new Vector3(2f, 0f, 0f);

            var result = MathExtension.QuadraticBezierPoint(0f, p0, p1, p2);
            Assert.AreEqual(p0, result);
        }

        [Test]
        public void QuadraticBezierPoint_T1_ReturnsP2()
        {
            var p0 = Vector3.zero;
            var p1 = new Vector3(1f, 2f, 0f);
            var p2 = new Vector3(2f, 0f, 0f);

            var result = MathExtension.QuadraticBezierPoint(1f, p0, p1, p2);
            Assert.AreEqual(p2, result);
        }

        [Test]
        public void QuadraticBezierPoint_T05_Midpoint()
        {
            var p0 = new Vector3(0f, 0f, 0f);
            var p1 = new Vector3(0f, 4f, 0f);
            var p2 = new Vector3(4f, 4f, 0f);

            // t=0.5: 0.25*p0 + 0.5*p1 + 0.25*p2 = (1, 3, 0)
            var result = MathExtension.QuadraticBezierPoint(0.5f, p0, p1, p2);
            Assert.AreEqual(new Vector3(1f, 3f, 0f), result);
        }

        // ─── Bezier ───

        [Test]
        public void Bezier_T0_ReturnsA()
        {
            var result = MathExtension.Bezier(0f, Vector3.zero, Vector3.one, new Vector3(2f, 2f, 2f));
            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void Bezier_T1_ReturnsC()
        {
            var c = new Vector3(2f, 2f, 2f);
            var result = MathExtension.Bezier(1f, Vector3.zero, Vector3.one, c);
            Assert.AreEqual(c, result);
        }

        // ─── CaclulateAcc / CalculateSpeed ───

        [Test]
        public void CaclulateAcc_ReturnsCorrectValue()
        {
            // acc = 2 * d / t^2
            var acc = MathExtension.CaclulateAcc(10f, 2f);
            Assert.AreEqual(5f, acc, 0.001f); // 2*10/4 = 5
        }

        [Test]
        public void CalculateSpeed_ReturnsCorrectValue()
        {
            // speed = t * acc = t * 2d/t^2 = 2d/t
            var speed = MathExtension.CalculateSpeed(10f, 2f);
            Assert.AreEqual(10f, speed, 0.001f); // 2*10/2 = 10
        }

        // ─── RotateDirectionY ───

        [Test]
        public void RotateDirectionY_90Degrees_TurnsRight()
        {
            var forward = Vector3.forward;
            var result = MathExtension.RotateDirectionY(forward, 90f);

            Assert.AreEqual(0f, result.x, 0.001f);
            Assert.AreEqual(0f, result.y, 0.001f);
            Assert.AreEqual(1f, result.z, 0.001f);
            // Actually forward rotated 90° around Y = right = (1,0,0)
            Assert.AreEqual(1f, result.x, 0.001f);
        }

        [Test]
        public void RotateDirectionY_ReturnsNormalized()
        {
            var result = MathExtension.RotateDirectionY(Vector3.forward, 45f);
            Assert.AreEqual(1f, result.magnitude, 0.001f);
        }

        // ─── IsInSection ───

        [Test]
        public void IsInSection_PointStraightAhead_ReturnsTrue()
        {
            var origin = Vector3.zero;
            var point = new Vector3(0f, 0f, 2f);
            var direction = Vector3.forward;

            Assert.IsTrue(MathExtension.IsInSection(origin, point, direction, 180f, 5f));
        }

        [Test]
        public void IsInSection_OutOfAngle_ReturnsFalse()
        {
            var origin = Vector3.zero;
            var point = new Vector3(5f, 0f, 0f); // 90 degrees from forward
            var direction = Vector3.forward;

            Assert.IsFalse(MathExtension.IsInSection(origin, point, direction, 30f, 10f));
        }

        [Test]
        public void IsInSection_OutOfRadius_ReturnsFalse()
        {
            var origin = Vector3.zero;
            var point = new Vector3(0f, 0f, 10f);
            var direction = Vector3.forward;

            Assert.IsFalse(MathExtension.IsInSection(origin, point, direction, 180f, 5f));
        }

        // ─── projectedOnPlane ───

        [Test]
        public void ProjectedOnPlane_UpNormal_FlattensToXZ()
        {
            var v = new Vector3(1f, 5f, 2f);
            var result = v.projectedOnPlane(Vector3.up);

            Assert.AreEqual(1f, result.x, 0.001f);
            Assert.AreEqual(0f, result.y, 0.001f);
            Assert.AreEqual(2f, result.z, 0.001f);
        }
    }
}
