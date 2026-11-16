using NUnit.Framework;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class BeltCameraFacingTests
    {
        private const float Tolerance = 1e-4f;
        private const float HalfSqrt2 = 0.70710678f;

        [Test]
        public void TryGetRotation_TravelUpTheScreen_LeansUpAxisTowardViewerAndScreenTop()
        {
            var ok = BeltCameraFacing.TryGetRotation(Vector3.forward, 45f, out var rotation);

            Assert.That(ok);
            AssertVector(rotation * Vector3.up, new Vector3(0f, HalfSqrt2, HalfSqrt2));
        }

        [Test]
        public void TryGetRotation_TravelRight_ForwardPointsRightAndUpAxisKeepsTilt()
        {
            BeltCameraFacing.TryGetRotation(Vector3.right, 45f, out var rotation);

            AssertVector(rotation * Vector3.forward, Vector3.right);
            AssertVector(rotation * Vector3.up, new Vector3(0f, HalfSqrt2, HalfSqrt2));
        }

        [Test]
        public void TryGetRotation_PathHeight_IsIgnored()
        {
            BeltCameraFacing.TryGetRotation(new Vector3(0f, 5f, 1f), 45f, out var sloped);
            BeltCameraFacing.TryGetRotation(Vector3.forward, 45f, out var flat);

            Assert.That(Quaternion.Angle(sloped, flat), Is.LessThan(Tolerance));
        }

        [Test]
        public void TryGetRotation_NoHorizontalDirection_ReturnsFalse()
        {
            var ok = BeltCameraFacing.TryGetRotation(Vector3.up, 45f, out var rotation);

            Assert.That(ok, Is.False);
            Assert.That(rotation, Is.EqualTo(Quaternion.identity));
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance),
                $"expected {expected} but was {actual}");
        }
    }
}
