using NUnit.Framework;
using UnityEngine;

namespace Tool.BeltPath.Tests
{
    public class BeltCameraFitMathTests
    {
        [Test]
        public void Project_UsesXAndZ_IgnoresHeight()
        {
            var bounds = new Bounds(new Vector3(0f, 2f, 0f), new Vector3(2f, 4f, 2f));

            var rect = BeltCameraFitMath.ProjectToScreenPlane(bounds);

            Assert.That(rect.xMin, Is.EqualTo(-1f).Within(1e-5f));
            Assert.That(rect.xMax, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(rect.yMin, Is.EqualTo(-1f).Within(1e-5f));
            Assert.That(rect.yMax, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Solve_WideContent_SizesByWidthAndAspect()
        {
            var fit = BeltCameraFitMath.Solve(new Rect(-10f, -1f, 20f, 2f), 2f, 0f, 1f);

            Assert.That(fit.OrthoSize, Is.EqualTo(5f).Within(1e-5f));
        }

        [Test]
        public void Solve_TallContent_SizesByHeight()
        {
            var fit = BeltCameraFitMath.Solve(new Rect(-1f, -10f, 2f, 20f), 2f, 0f, 1f);

            Assert.That(fit.OrthoSize, Is.EqualTo(10f).Within(1e-5f));
        }

        [Test]
        public void Solve_SmallContent_ClampsToMinSize()
        {
            var fit = BeltCameraFitMath.Solve(new Rect(0f, 0f, 1f, 1f), 1f, 0f, 3f);

            Assert.That(fit.OrthoSize, Is.EqualTo(3f));
        }

        [Test]
        public void Solve_AddsPaddingOnEverySide_AndCentersOnContent()
        {
            var fit = BeltCameraFitMath.Solve(new Rect(2f, 4f, 4f, 4f), 1f, 1f, 0.1f);

            Assert.That(fit.OrthoSize, Is.EqualTo(3f).Within(1e-5f));
            Assert.That(fit.Center, Is.EqualTo(new Vector2(4f, 6f)));
        }
    }
}
