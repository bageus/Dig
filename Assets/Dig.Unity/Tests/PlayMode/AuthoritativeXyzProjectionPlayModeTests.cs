using System.Collections;
using Dig.Presentation.Navigation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
    public sealed class AuthoritativeXyzProjectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator Route_renderer_keeps_same_xy_cells_distinct_across_depth()
        {
            GameObject host = new GameObject("Authoritative XYZ route host");
            DigNavigationRouteRenderer renderer =
                host.AddComponent<DigNavigationRouteRenderer>();
            renderer.Render(new[]
            {
                new RouteViewModel(
                    "job.xyz",
                    "resident.xyz",
                    workX: 2,
                    workY: 3,
                    workZ: 3,
                    succeeded: true,
                    detail: string.Empty,
                    totalCost: 1,
                    navigationVersion: 1,
                    cells: new[]
                    {
                        new RouteCellViewModel(2, 3, 0),
                        new RouteCellViewModel(2, 3, 3),
                    }),
            });
            yield return null;

            LineRenderer line = host.GetComponentInChildren<LineRenderer>()
                ?? throw new AssertionException("Route line was not rendered.");
            Assert.AreEqual(2, line.positionCount);
            Vector3 front = line.GetPosition(0);
            Vector3 back = line.GetPosition(1);
            Assert.AreEqual(front.x, back.x, 0.001f);
            Assert.AreEqual(front.y, back.y, 0.001f);
            Assert.AreNotEqual(front.z, back.z);

            Object.DestroyImmediate(host);
        }
    }
}
