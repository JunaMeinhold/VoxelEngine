namespace VoxelEngine.Mathematics
{
    using System.Numerics;
    using Silk.NET.Maths;
    using Vortice.Mathematics;
    using BoundingBox = BepuUtilities.BoundingBox;
    using ContainmentType = BepuUtilities.ContainmentType;
    using Plane = System.Numerics.Plane;

    public struct Frustum
    {
        private readonly Plane[] planes;

        public Frustum(Matrix4x4 viewProjection)
        {
            planes = new Plane[]
            {
                Plane.Normalize(new Plane(-viewProjection.M13, -viewProjection.M23, -viewProjection.M33, -viewProjection.M43)),
                Plane.Normalize(new Plane(viewProjection.M13 - viewProjection.M14, viewProjection.M23 - viewProjection.M24, viewProjection.M33 - viewProjection.M34, viewProjection.M43 - viewProjection.M44)),
                Plane.Normalize(new Plane(-viewProjection.M14 - viewProjection.M11, -viewProjection.M24 - viewProjection.M21, -viewProjection.M34 - viewProjection.M31, -viewProjection.M44 - viewProjection.M41)),
                Plane.Normalize(new Plane(viewProjection.M11 - viewProjection.M14, viewProjection.M21 - viewProjection.M24, viewProjection.M31 - viewProjection.M34, viewProjection.M41 - viewProjection.M44)),
                Plane.Normalize(new Plane(viewProjection.M12 - viewProjection.M14, viewProjection.M22 - viewProjection.M24, viewProjection.M32 - viewProjection.M34, viewProjection.M42 - viewProjection.M44)),
                Plane.Normalize(new Plane(-viewProjection.M14 - viewProjection.M12, -viewProjection.M24 - viewProjection.M22, -viewProjection.M34 - viewProjection.M32, -viewProjection.M44 - viewProjection.M42)),
            };
        }

        public Frustum(Plane[] planes)
        {
            this.planes = planes;
        }

        public Frustum(float screenDepth, Matrix4x4 projection, Matrix4x4 view)
        {
            planes = new Plane[6];
            // Calculate the minimum Z distance in the frustum.
            float zMinimum = -projection.M43 / projection.M33;
            float r = screenDepth / (screenDepth - zMinimum);
            projection.M33 = r;
            projection.M43 = -r * zMinimum;

            // Create the frustum matrix from the view matrix and updated projection matrix.
            Matrix4x4 matrix = view * projection;

            // Calculate near plane of frustum.
            planes[0] = new Plane(matrix.M14 + matrix.M13, matrix.M24 + matrix.M23, matrix.M34 + matrix.M33, matrix.M44 + matrix.M43);
            planes[0] = Plane.Normalize(planes[0]);

            // Calculate far plane of frustum.
            planes[1] = new Plane(matrix.M14 - matrix.M13, matrix.M24 - matrix.M23, matrix.M34 - matrix.M33, matrix.M44 - matrix.M43);
            planes[1] = Plane.Normalize(planes[1]);

            // Calculate left plane of frustum.
            planes[2] = new Plane(matrix.M14 + matrix.M11, matrix.M24 + matrix.M21, matrix.M34 + matrix.M31, matrix.M44 + matrix.M41);
            planes[2] = Plane.Normalize(planes[2]);

            // Calculate right plane of frustum.
            planes[3] = new Plane(matrix.M14 - matrix.M11, matrix.M24 - matrix.M21, matrix.M34 - matrix.M31, matrix.M44 - matrix.M41);
            planes[3] = Plane.Normalize(planes[3]);

            // Calculate top plane of frustum.
            planes[4] = new Plane(matrix.M14 - matrix.M12, matrix.M24 - matrix.M22, matrix.M34 - matrix.M32, matrix.M44 - matrix.M42);
            planes[4] = Plane.Normalize(planes[4]);

            // Calculate bottom plane of frustum.
            planes[5] = new Plane(matrix.M14 + matrix.M12, matrix.M24 + matrix.M22, matrix.M34 + matrix.M32, matrix.M44 + matrix.M42);
            planes[5] = Plane.Normalize(planes[5]);
        }

        public Plane[] Planes => planes;

        public bool CheckSphere(Vector3 center, float radius)
        {
            // Check if the radius of the sphere is inside the view frustum.
            for (int i = 0; i < 6; i++)
            {
                if (Plane.DotCoordinate(planes[i], center) < -radius)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the current <see cref="BoundingFrustum"/> intersects with a specified <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The <see cref="BoundingBox"/> to check for intersection with the current <see cref="BoundingFrustum"/>.</param>
        /// <returns>True if intersects, false otherwise.</returns>
        public bool Intersects(BoundingBox box)
        {
            Vortice.Mathematics.BoundingBox box1 = new(box.Min, box.Max);
            for (int i = 0; i < planes.Length; i++)
            {
                Plane plane = planes[i];
                PlaneIntersectionType intersection = box1.Intersects(in plane);

                if (intersection == PlaneIntersectionType.Front)
                {
                    return false;
                }
            }

            return true;
        }

        public ContainmentType Contains(BoundingBox box)
        {
            Plane plane;
            ContainmentType result = ContainmentType.Contains;
            for (int i = 0; i < 6; i++)
            {
                plane = planes[i];
                GetBoxToPlanePVertexNVertex(box, plane.Normal, out Vector3 p, out Vector3 n);
                if (PlaneIntersectsPoint(plane, p) == PlaneIntersectionType.Back)
                {
                    return ContainmentType.Disjoint;
                }

                if (PlaneIntersectsPoint(plane, n) == PlaneIntersectionType.Back)
                {
                    result = ContainmentType.Intersects;
                }
            }
            return result;
        }

        public static PlaneIntersectionType PlaneIntersectsPoint(Plane plane, Vector3 point)
        {
            float distance = Vector3.Dot(plane.Normal, point);
            distance += plane.D;

            if (distance > 0f)
            {
                return PlaneIntersectionType.Front;
            }

            if (distance < 0f)
            {
                return PlaneIntersectionType.Back;
            }

            return PlaneIntersectionType.Intersecting;
        }

        private static void GetBoxToPlanePVertexNVertex(BoundingBox box, Vector3 planeNormal, out Vector3 p, out Vector3 n)
        {
            p = box.Min;
            if (planeNormal.X >= 0)
            {
                p.X = box.Max.X;
            }

            if (planeNormal.Y >= 0)
            {
                p.Y = box.Max.Y;
            }

            if (planeNormal.Z >= 0)
            {
                p.Z = box.Max.Z;
            }

            n = box.Max;
            if (planeNormal.X >= 0)
            {
                n.X = box.Min.X;
            }

            if (planeNormal.Y >= 0)
            {
                n.Y = box.Min.Y;
            }

            if (planeNormal.Z >= 0)
            {
                n.Z = box.Min.Z;
            }
        }
    }
}