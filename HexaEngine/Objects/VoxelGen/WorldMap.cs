using HexaEngine.Mathematics;
using System;
using System.Numerics;
using Vortice.Mathematics;

namespace HexaEngine.Objects.VoxelGen
{
    public class WorldMap
    {
        public Chunk[,,] Chunks;
        public const byte SHIFT = 5;
        public const int MASK = 0x1f;
        public const int MAP_SIZE_X = 512 * Chunk.CHUNK_SIZE;
        public const int MAP_SIZE_Y = 16 * Chunk.CHUNK_SIZE;
        public const int MAP_SIZE_Z = 512 * Chunk.CHUNK_SIZE;
        public const int CHUNK_AMOUNT_X = 512;
        public const int CHUNK_AMOUNT_Y = 16;
        public const int CHUNK_AMOUNT_Z = 512;
        public string Path { get; protected set; }

        // Returns true if there is no block at this global map position

        public bool IsNoBlock(Vector3 pos)
        {
            return IsNoBlock(pos.X.Round(), pos.Y.Round(), pos.Z.Round());
        }

        public bool IsNoBlock(int x, int y, int z)
        {
            var xglobal = x / Chunk.CHUNK_SIZE;
            var xlocal = x % Chunk.CHUNK_SIZE;
            var yglobal = y / Chunk.CHUNK_SIZE;
            var ylocal = y % Chunk.CHUNK_SIZE;
            var zglobal = z / Chunk.CHUNK_SIZE;
            var zlocal = z % Chunk.CHUNK_SIZE;
            // If it is at the edge of the map, return true
            if (xglobal < 0 || xglobal >= CHUNK_AMOUNT_X ||
                yglobal < 0 || yglobal >= MAP_SIZE_Y ||
                zglobal < 0 || zglobal >= MAP_SIZE_Z)
                return true;
            if (xlocal < 0 || xlocal >= Chunk.CHUNK_SIZE ||
                ylocal < 0 || ylocal >= Chunk.CHUNK_SIZE ||
                zlocal < 0 || zlocal >= Chunk.CHUNK_SIZE)
                return true;

            // Chunk accessed quickly using bitwise shifts
            var c = Chunks[xglobal, yglobal, zglobal];

            // To lower memory usage, a chunk is null if it has no blocks
            if (c == null)
                return true;

            // Chunk data accessed quickly using bit masks
            return c.data[Extensions.MapToIndex(xlocal, ylocal, zlocal, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE)].index == Chunk.EMPTY;
        }

        public void Set(Chunk chunk, Vector3 pos)
        {
            if (pos.X < 0 || pos.X >= CHUNK_AMOUNT_X ||
                         pos.Y < 0 || pos.Y >= CHUNK_AMOUNT_Y ||
                         pos.Z < 0 || pos.Z >= CHUNK_AMOUNT_Z)
                return;
            Chunks[(int)pos.X, (int)pos.Y, (int)pos.Z] = chunk;
        }

        public void Set(Chunk chunk, float x, float y, float z)
        {
            if (x < 0 || x >= CHUNK_AMOUNT_X ||
                   y < 0 || y >= CHUNK_AMOUNT_Y ||
                   z < 0 || z >= MAP_SIZE_Z)
                return;
            Chunks[(int)x, (int)y, (int)z] = chunk;
        }

        public Chunk Get(Vector3 pos)
        {
            if (pos.X < 0 || pos.X >= CHUNK_AMOUNT_X ||
                      pos.Y < 0 || pos.Y >= CHUNK_AMOUNT_Y ||
                      pos.Z < 0 || pos.Z >= CHUNK_AMOUNT_Z)
                return null;
            return Chunks[(int)pos.X, (int)pos.Y, (int)pos.Z];
        }

        public Chunk Get(float x, float y, float z)
        {
            if (x < 0 || x >= CHUNK_AMOUNT_X ||
                   y < 0 || y >= CHUNK_AMOUNT_Y ||
                   z < 0 || z >= CHUNK_AMOUNT_Z)
                return null;
            return Chunks[(int)x, (int)y, (int)z];
        }

        public void Set(ChunkRegion region)
        {
            if (region.Position.X < 0 || region.Position.X >= CHUNK_AMOUNT_X || region.Position.Y < 0 || region.Position.Y >= CHUNK_AMOUNT_Y)
                return;
            for (int i = 0; i < region.Chunks.Length; i++)
            {
                Set(region.Chunks[i], region.Position.X, i, region.Position.Y);
            }
        }

        public ChunkRegion GetRegion(Vector3 pos)
        {
            if (pos.X < 0 || pos.X >= CHUNK_AMOUNT_X || pos.Y < 0 || pos.Y >= CHUNK_AMOUNT_Y || pos.Z < 0 || pos.Z >= CHUNK_AMOUNT_Z) return default;
            return ChunkRegion.CreateFrom(this, pos);
        }

        public ChunkRegion GetRegion(float x, float z)
        {
            if (x < 0 || x >= CHUNK_AMOUNT_X || z < 0 || z >= CHUNK_AMOUNT_Z) return default;
            return ChunkRegion.CreateFrom(this, x, z);
        }

        #region Raycast

        public void RayMarch(Ray ray, in double max, ref bool hit, ref Axis axis)
        {
            RayMarch(ray.Position, ray.Direction, max, ref hit, ref axis);
        }

        // Voxel ray marching from http://www.cse.chalmers.se/edu/year/2010/course/TDA361/grid.pdf
        // Optimised by keeping block lookups within the current chunk, which minimises bitshifts, masks and multiplication operations
        public void RayMarch(in Vector3 start, Vector3 direction, in double max, ref bool hit, ref Axis axis)
        {
            int x = (int)start.X;
            int y = (int)start.Y;
            int z = (int)start.Z;

            if (y < 0 || y >= CHUNK_AMOUNT_Y || x < 0 || x >= CHUNK_AMOUNT_X || z < 0 || z >= CHUNK_AMOUNT_Z)
            {
                hit = false;
                return;
            }

            // 2^5 = 32 (chunkSize)
            int chunkIndexX = x >> SHIFT;
            int chunkIndexY = y >> SHIFT;
            int chunkIndexZ = z >> SHIFT;

            var c = Chunks[chunkIndexX, chunkIndexY, chunkIndexZ];

            // Determine the chunk-relative position of the ray using a bit-mask
            int i = x & MASK;
            int j = y & MASK;
            int k = z & MASK;

            // Calculate the index of this block in the chunk data[] array
            int access = j + i * Chunk.CHUNK_SIZE + k * Chunk.CHUNK_SIZE_SQUARED;

            // Calculate the end position of the ray
            var end = start + direction;

            // If the start and end positions of the ray both lie on the same coordinate on the voxel grid
            if (x == (int)end.X && y == (int)end.Y && z == (int)end.Z)
            {
                // The chunk is null if it contains no blocks
                if (c == null)
                {
                    hit = false;
                }

                // If the block is empty
                else if (c.data[access].index == 0)
                {
                    hit = false;
                }

                // Else the ray begins and ends within the same non-empty block
                else
                {
                    hit = true;
                }

                return;
            }

            // These variables are used to determine whether the ray has left the current working chunk.
            //  For example when travelling in the negative Y direction,
            //  if j == -1 then we have left the current working chunk
            int iComparison, jComparison, kComparison;

            // When leaving the current working chunk, the chunk-relative position must be reset.
            //  For example when travelling in the negative Y direction,
            //  j should be reset to CHUNK_SIZE - 1 when entering the new current working chunk
            int iReset, jReset, kReset;

            // When leaving the current working chunk, the access variable must also be updated.
            //  These values store how much to add or subtract from the access, depending on
            //  the direction of the ray:
            int xAccessReset, yAccessReset, zAccessReset;

            // The amount to increase i, j and k in each axis (either 1 or -1)
            int iStep, jStep, kStep;

            // When incrementing j, the chunk access is simply increased by 1
            // When incrementing i, the chunk access is increased by 32 (CHUNK_SIZE)
            // When incrementing k, the chunk access is increased by 1024 (CHUNK_SIZE_SQUARED)
            // These variables store whether to increase or decrease by the above amounts
            int xAccessIncrement, zAccessIncrement;

            // The distance to the closest voxel boundary in map units
            double xDist, yDist, zDist;

            if (direction.X > 0)
            {
                iStep = 1;
                iComparison = Chunk.CHUNK_SIZE;
                iReset = 0;
                xAccessReset = -Chunk.CHUNK_SIZE_SQUARED;
                xAccessIncrement = Chunk.CHUNK_SIZE;
                xDist = x - start.X + 1;
            }
            else
            {
                iStep = -1;
                iComparison = -1;
                iReset = Chunk.CHUNK_SIZE - 1;
                xAccessReset = Chunk.CHUNK_SIZE_SQUARED;
                xAccessIncrement = -Chunk.CHUNK_SIZE;
                xDist = start.X - x;
            }

            if (direction.Y > 0)
            {
                jStep = 1;
                jComparison = Chunk.CHUNK_SIZE;
                jReset = 0;
                yAccessReset = -Chunk.CHUNK_SIZE;
                yDist = y - start.Y + 1;
            }
            else
            {
                jStep = -1;
                jComparison = -1;
                jReset = Chunk.CHUNK_SIZE - 1;
                yAccessReset = Chunk.CHUNK_SIZE;
                yDist = start.Y - y;
            }

            if (direction.Z > 0)
            {
                kStep = 1;
                kComparison = Chunk.CHUNK_SIZE;
                kReset = 0;
                zAccessIncrement = Chunk.CHUNK_SIZE_SQUARED;
                zAccessReset = -Chunk.CHUNK_SIZE_CUBED;
                zDist = z - start.Z + 1;
            }
            else
            {
                kStep = -1;
                kComparison = -1;
                kReset = Chunk.CHUNK_SIZE - 1;
                zAccessIncrement = -Chunk.CHUNK_SIZE_SQUARED;
                zAccessReset = Chunk.CHUNK_SIZE_CUBED;
                zDist = start.Z - z;
            }

            // This variable is used to track the current progress throughout the ray march
            double t = 0.0;

            direction = Vector3.Normalize(direction);
            double xInverted = Math.Abs(1 / direction.X);
            double yInverted = Math.Abs(1 / direction.Y);
            double zInverted = Math.Abs(1 / direction.Z);

            // Determine the distance to the closest voxel boundary in units of t
            //  - These values indicate how far we have to travel along the ray to reach the next voxel
            //  - If any component of the direction is perpendicular to an axis, the distance is double.PositiveInfinity
            double xDistance = direction.X == 0 ? double.PositiveInfinity : xInverted * xDist;
            double yDistance = direction.Y == 0 ? double.PositiveInfinity : yInverted * yDist;
            double zDistance = direction.Z == 0 ? double.PositiveInfinity : zInverted * zDist;

            while (t <= max)
            {
                // Exit check
                if (c != null && c.data[access].index != 0)
                {
                    hit = true;
                    return;
                }

                // Determine the closest voxel boundary
                if (yDistance < xDistance)
                {
                    if (yDistance < zDistance)
                    {
                        // Advance to the closest voxel boundary in the Y direction

                        // Increment the chunk-relative position and the block access position
                        j += jStep;
                        access += jStep;

                        // Check if we have exited the current working chunk.
                        // This means that j is either -1 or 32
                        if (j == jComparison)
                        {
                            // If moving in the positive direction, reset j to 0.
                            // If moving in the negative Y direction, reset j to 31
                            j = jReset;

                            // Reset the chunk access
                            access += yAccessReset;

                            // Calculate the new chunk index
                            chunkIndexY += jStep;

                            // If the new chunk is outside the map, exit
                            if (chunkIndexY < 0 || chunkIndexY >= MAP_SIZE_Y)
                            {
                                hit = false;
                                return;
                            }

                            // Get a reference to the new working chunk
                            c = Chunks[chunkIndexX, chunkIndexY, chunkIndexZ];
                        }

                        // Update our progress in the ray
                        t = yDistance;

                        // Set the new distance to the next voxel Y boundary
                        yDistance += yInverted;

                        // For collision purposes we also store the last axis that the ray collided with
                        // This allows us to reflect particle velocity on the correct axis
                        axis = Axis.Y;
                    }
                    else
                    {
                        k += kStep;
                        access += zAccessIncrement;

                        if (k == kComparison)
                        {
                            k = kReset;
                            access += zAccessReset;

                            chunkIndexZ += kStep;

                            if (chunkIndexZ < 0 || chunkIndexZ >= CHUNK_AMOUNT_Z)
                            {
                                hit = false;
                                return;
                            }

                            c = Chunks[chunkIndexX, chunkIndexY, chunkIndexZ];
                        }

                        t = zDistance;
                        zDistance += zInverted;
                        axis = Axis.Z;
                    }
                }
                else if (xDistance < zDistance)
                {
                    i += iStep;
                    access += xAccessIncrement;

                    if (i == iComparison)
                    {
                        i = iReset;
                        access += xAccessReset;

                        chunkIndexX += iStep;

                        if (chunkIndexX < 0 || chunkIndexX >= CHUNK_AMOUNT_X)
                        {
                            hit = false;
                            return;
                        }

                        c = Chunks[chunkIndexX, chunkIndexY, chunkIndexZ];
                    }

                    t = xDistance;
                    xDistance += xInverted;
                    axis = Axis.X;
                }
                else
                {
                    k += kStep;
                    access += zAccessIncrement;

                    if (k == kComparison)
                    {
                        k = kReset;
                        access += zAccessReset;

                        chunkIndexZ += kStep;

                        if (chunkIndexZ < 0 || chunkIndexZ >= CHUNK_AMOUNT_Z)
                        {
                            hit = false;
                            return;
                        }

                        c = Chunks[chunkIndexX, chunkIndexY, chunkIndexZ];
                    }

                    t = zDistance;
                    zDistance += zInverted;
                    axis = Axis.Z;
                }
            }

            hit = false;
        }

        #endregion Raycast
    }
}