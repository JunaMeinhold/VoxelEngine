namespace HexaEngine.Objects.VoxelGen
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Vortice.Mathematics;

    public struct VoxelHelper
    {
        private Matrix4x4 inverseTransformationMatrix;

        public VoxelHelper(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            TransformationMatrix = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale);
            Matrix4x4.Invert(TransformationMatrix, out inverseTransformationMatrix);
        }

        public VoxelHelper(Matrix4x4 transformationMatrix)
        {
            TransformationMatrix = transformationMatrix;
            Matrix4x4.Invert(TransformationMatrix, out inverseTransformationMatrix);
        }

        public Matrix4x4 TransformationMatrix { get; private set; }

        public Matrix4x4 InverseTransformationMatrix { get => inverseTransformationMatrix; private set => inverseTransformationMatrix = value; }

        public Vector3 WorldToVoxel(Vector3 worldPosition)
        {
            return Vector3.Transform(worldPosition, inverseTransformationMatrix);
        }

        public Vector3 WorldToVoxelCoordinate(Vector3 worldPosition)
        {
            Vector3 voxelPosition = WorldToVoxel(worldPosition);
            return new Vector3(MathF.Round(voxelPosition.X), MathF.Round(voxelPosition.Y), MathF.Round(voxelPosition.Z));
        }

        public Vector3 VoxelToWorld(Vector3 voxelPosition)
        {
            return Vector3.Transform(voxelPosition, TransformationMatrix);
        }

        public IEnumerable<Vector3> Traverse(Ray ray, float distance)
        {
            Vector3 relativeOrigin = ray.Position;
            Vector3 relativeDirection = ray.Direction;

            Vector3 currentVoxel = ray.Position;

            Vector3 step = new(MathF.Sign(relativeDirection.X), MathF.Sign(relativeDirection.Y), MathF.Sign(relativeDirection.Z));

            Vector3 cellBoundary = new(currentVoxel.X + 0.5f * step.X, currentVoxel.Y + 0.5f * step.Y, currentVoxel.Z + 0.5f * step.Z);

            Vector3 tMax = new(relativeDirection.X != 0 ? (cellBoundary.X - relativeOrigin.X) * (1 / relativeDirection.X) : float.PositiveInfinity, relativeDirection.Y != 0 ? (cellBoundary.Y - relativeOrigin.Y) * (1 / relativeDirection.Y) : float.PositiveInfinity, relativeDirection.Z != 0 ? (cellBoundary.Z - relativeOrigin.Z) * (1 / relativeDirection.Z) : float.PositiveInfinity);

            Vector3 tDelta = new(relativeDirection.X != 0 ? 1 / relativeDirection.X * step.X : float.PositiveInfinity, relativeDirection.Y != 0 ? 1 / relativeDirection.Y * step.Y : float.PositiveInfinity, relativeDirection.Z != 0 ? 1 / relativeDirection.Z * step.Z : float.PositiveInfinity);
            float currentDistance = 0;

            while (currentDistance < distance)
            {
                yield return currentVoxel;

                if (tMax.X <= tMax.Y && tMax.X <= tMax.Z)
                {
                    currentVoxel.X += step.X;
                    currentDistance = tMax.X;
                    tMax.X += tDelta.X;
                }
                else if (tMax.Y <= tMax.Z)
                {
                    currentVoxel.Y += step.Y;
                    currentDistance = tMax.Y;
                    tMax.Y += tDelta.Y;
                }
                else
                {
                    currentVoxel.Z += step.Z;
                    currentDistance = tMax.Z;
                    tMax.Z += tDelta.Z;
                }
            }
        }
    }
}