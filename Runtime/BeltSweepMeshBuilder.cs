using UnityEngine;
using UnityEngine.Rendering;

namespace Tool.BeltPath
{
    /// <summary>
    /// Builds channel and water meshes by sweeping a 2D profile (right, up) along a <see cref="BeltPath"/>.
    /// Every profile segment gets its own vertices, so a sharp profile corner stays hard while a gentle bend is
    /// smoothed. UV.v follows the path length; UV2.x is the vertex height in the profile (0 floor, 1 crest).
    /// </summary>
    public static class BeltSweepMeshBuilder
    {
        private const int VerticesPerProfileSegment = 2;
        private const int IndicesPerQuad = 6;
        private const int MaxUInt16Vertices = 65000;
        private const float Half = 0.5f;

        // Two neighbouring profile segments share one normal when they bend less than this.
        private const float SmoothingAngle = 40f;

        public static Mesh BuildChannel(BeltPath sampler, BeltShapeSettings shape, BeltMeshSettings visual)
        {
            var profile = BeltChannelProfile.Build(
                visual.ChannelWidthCells * Half * shape.CellSize,
                visual.WallHeightCells * shape.CellSize,
                visual.WallThicknessCells * shape.CellSize,
                visual.FloorFilletCells * shape.CellSize,
                visual.WallArcSegments);
            return Sweep(sampler, profile, shape, visual, "BeltChannel");
        }

        public static Mesh BuildWaterSurface(BeltPath sampler, BeltShapeSettings shape, BeltMeshSettings visual)
        {
            var half = visual.ChannelWidthCells * Half * shape.CellSize;
            var level = visual.WaterDepthCells * shape.CellSize;
            var profile = new[] { new Vector2(-half, level), new Vector2(half, level) };
            return Sweep(sampler, profile, shape, visual, "BeltWater");
        }

        /// <summary>Flat strip under the channel, as wide as the outer walls plus the shadow spread.</summary>
        public static Mesh BuildShadow(BeltPath sampler, BeltShapeSettings shape, BeltMeshSettings visual)
        {
            var half = (visual.ChannelWidthCells * Half + visual.WallThicknessCells + visual.ShadowSpreadCells)
                * shape.CellSize;
            var profile = new[] { new Vector2(-half, 0f), new Vector2(half, 0f) };
            return Sweep(sampler, profile, shape, visual, "BeltShadow");
        }

        private static Mesh Sweep(BeltPath sampler, Vector2[] points, BeltShapeSettings shape,
            BeltMeshSettings visual, string meshName)
        {
            var step = visual.SampleStepCells * shape.CellSize;
            var uvLength = ComputeUvLength(sampler, visual.UvLengthCells * shape.CellSize);
            var profile = new ProfileData(points);
            var intervals = Mathf.Max(1, Mathf.RoundToInt(sampler.Length / step));
            var ringCount = intervals + 1;
            var buffers = new MeshBuffers(ringCount * profile.SegmentCount * VerticesPerProfileSegment);

            for (var ring = 0; ring < ringCount; ring++)
            {
                var distance = sampler.Length * ring / intervals;
                FillRing(sampler, profile, ring, distance, distance / uvLength, buffers);
            }

            return CreateMesh(meshName, buffers, BuildTriangles(ringCount, profile.SegmentCount));
        }

        // A closed belt must end on a whole number of UV units so tiled textures meet without a seam.
        private static float ComputeUvLength(BeltPath sampler, float uvLength)
        {
            if (!sampler.IsClosed)
            {
                return uvLength;
            }

            var repeats = Mathf.Max(1, Mathf.RoundToInt(sampler.Length / uvLength));
            return sampler.Length / repeats;
        }

        private static void FillRing(BeltPath sampler, ProfileData profile, int ring, float distance,
            float v, MeshBuffers buffers)
        {
            var center = sampler.Position(distance);
            var right = Vector3.Cross(Vector3.up, sampler.Forward(distance));
            right = right.sqrMagnitude > Mathf.Epsilon ? right.normalized : Vector3.right;

            for (var segment = 0; segment < profile.SegmentCount; segment++)
            {
                var index = (ring * profile.SegmentCount + segment) * VerticesPerProfileSegment;
                for (var end = 0; end < VerticesPerProfileSegment; end++)
                {
                    var point = profile.Points[segment + end];
                    var normal = profile.Normals[segment * VerticesPerProfileSegment + end];
                    buffers.Vertices[index + end] = center + right * point.x + Vector3.up * point.y;
                    buffers.Normals[index + end] = right * normal.x + Vector3.up * normal.y;
                    buffers.Uvs[index + end] = new Vector2(profile.U[segment + end], v);
                    buffers.HeightUvs[index + end] = new Vector2(profile.Heights[segment + end], 0f);
                }
            }
        }

        // Quad corners: a/b are one ring's segment ends, c/d the next ring's. Winding gives normals toward
        // the profile's left side, so the outline faces the channel interior and the outside.
        private static int[] BuildTriangles(int ringCount, int segmentCount)
        {
            var triangles = new int[(ringCount - 1) * segmentCount * IndicesPerQuad];
            var write = 0;
            for (var ring = 0; ring < ringCount - 1; ring++)
            {
                for (var segment = 0; segment < segmentCount; segment++)
                {
                    var a = (ring * segmentCount + segment) * VerticesPerProfileSegment;
                    var c = ((ring + 1) * segmentCount + segment) * VerticesPerProfileSegment;
                    triangles[write++] = a;
                    triangles[write++] = c;
                    triangles[write++] = a + 1;
                    triangles[write++] = a + 1;
                    triangles[write++] = c;
                    triangles[write++] = c + 1;
                }
            }

            return triangles;
        }

        private static Mesh CreateMesh(string meshName, MeshBuffers buffers, int[] triangles)
        {
            var mesh = new Mesh
            {
                name = meshName,
                indexFormat = buffers.Vertices.Length > MaxUInt16Vertices ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.vertices = buffers.Vertices;
            mesh.normals = buffers.Normals;
            mesh.uv = buffers.Uvs;
            mesh.uv2 = buffers.HeightUvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Per-point data of a profile, derived once per sweep.</summary>
        private sealed class ProfileData
        {
            public ProfileData(Vector2[] points)
            {
                Points = points;
                SegmentCount = points.Length - 1;
                U = ComputeU(points);
                Heights = ComputeHeights(points);
                Normals = ComputeNormals(points);
            }

            public Vector2[] Points { get; }
            public int SegmentCount { get; }

            /// <summary>Position across the profile, 0..1 by outline length.</summary>
            public float[] U { get; }

            /// <summary>Height of each point as a fraction of the highest point, clamped to 0..1.</summary>
            public float[] Heights { get; }

            /// <summary>Two entries per segment: the normal at its start and at its end.</summary>
            public Vector2[] Normals { get; }

            private static float[] ComputeU(Vector2[] points)
            {
                var u = new float[points.Length];
                for (var i = 1; i < points.Length; i++)
                {
                    u[i] = u[i - 1] + Vector2.Distance(points[i - 1], points[i]);
                }

                var total = u[u.Length - 1];
                for (var i = 0; i < u.Length && total > Mathf.Epsilon; i++)
                {
                    u[i] /= total;
                }

                return u;
            }

            private static float[] ComputeHeights(Vector2[] points)
            {
                var top = points[0].y;
                foreach (var point in points)
                {
                    top = Mathf.Max(top, point.y);
                }

                var heights = new float[points.Length];
                for (var i = 0; i < points.Length && top > Mathf.Epsilon; i++)
                {
                    heights[i] = Mathf.Clamp01(points[i].y / top);
                }

                return heights;
            }

            private static Vector2[] ComputeNormals(Vector2[] points)
            {
                var segmentCount = points.Length - 1;
                var face = new Vector2[segmentCount];
                for (var i = 0; i < segmentCount; i++)
                {
                    var direction = points[i + 1] - points[i];
                    face[i] = new Vector2(-direction.y, direction.x).normalized;
                }

                var normals = new Vector2[segmentCount * VerticesPerProfileSegment];
                for (var i = 0; i < segmentCount; i++)
                {
                    normals[i * VerticesPerProfileSegment] = Smooth(face[i], i > 0 ? face[i - 1] : face[i]);
                    normals[i * VerticesPerProfileSegment + 1] =
                        Smooth(face[i], i < segmentCount - 1 ? face[i + 1] : face[i]);
                }

                return normals;
            }

            private static Vector2 Smooth(Vector2 own, Vector2 neighbour)
            {
                return Vector2.Angle(own, neighbour) < SmoothingAngle ? (own + neighbour).normalized : own;
            }
        }

        /// <summary>Vertex streams of the mesh being built.</summary>
        private sealed class MeshBuffers
        {
            public MeshBuffers(int vertexCount)
            {
                Vertices = new Vector3[vertexCount];
                Normals = new Vector3[vertexCount];
                Uvs = new Vector2[vertexCount];
                HeightUvs = new Vector2[vertexCount];
            }

            public Vector3[] Vertices { get; }
            public Vector3[] Normals { get; }
            public Vector2[] Uvs { get; }
            public Vector2[] HeightUvs { get; }
        }
    }
}
