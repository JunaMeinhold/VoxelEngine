namespace HexaEngine.Scenes.Objects
{
    using System.Numerics;
    using Vortice.Mathematics;

    public struct Frustum
    {
        private readonly Plane[] planes;

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

        public bool CheckSphere(Vector3 center, float radius)
        {
            // Check if the radius of the sphere is inside the view frustum.
            for (int i = 0; i < 6; i++)
            {
                if (Plane.DotCoordinate(planes[i], center) < -radius)
                    return false;
            }
            return true;
        }

        public bool Intersects(BoundingBox box)
        {
            return Contains(box) != ContainmentType.Disjoint;
        }

        public ContainmentType Contains(BoundingBox box)
        {
            Vector3 p, n;
            Plane plane;
            var result = ContainmentType.Contains;
            for (int i = 0; i < 6; i++)
            {
                plane = planes[i];
                GetBoxToPlanePVertexNVertex(box, plane.Normal, out p, out n);
                if (PlaneIntersectsPoint(plane, p) == PlaneIntersectionType.Back)
                    return ContainmentType.Disjoint;

                if (PlaneIntersectsPoint(plane, n) == PlaneIntersectionType.Back)
                    result = ContainmentType.Intersects;
            }
            return result;
        }

        public static PlaneIntersectionType PlaneIntersectsPoint(Plane plane, Vector3 point)
        {
            float distance = Vector3.Dot(plane.Normal, point);
            distance += plane.D;

            if (distance > 0f)
                return PlaneIntersectionType.Front;

            if (distance < 0f)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }

        private void GetBoxToPlanePVertexNVertex(BoundingBox box, Vector3 planeNormal, out Vector3 p, out Vector3 n)
        {
            p = box.Minimum;
            if (planeNormal.X >= 0)
                p.X = box.Maximum.X;
            if (planeNormal.Y >= 0)
                p.Y = box.Maximum.Y;
            if (planeNormal.Z >= 0)
                p.Z = box.Maximum.Z;

            n = box.Maximum;
            if (planeNormal.X >= 0)
                n.X = box.Minimum.X;
            if (planeNormal.Y >= 0)
                n.Y = box.Minimum.Y;
            if (planeNormal.Z >= 0)
                n.Z = box.Minimum.Z;
        }
    }
}