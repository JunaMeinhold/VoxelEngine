// Code ported from https://0fps.net/2012/06/30/meshing-in-a-minecraft-game/

// Note this implementation does not support different block types or block normals
// The original author describes how to do this here: https://0fps.net/2012/07/07/meshing-minecraft-part-2/
using HexaEngine.Objects;
using System.Numerics;
using System.Threading.Tasks;

namespace HexaEngine.Mathematics
{
    public class GreedyMesh
    {
        private const int CHUNK_WIDTH = 16;
        private const int CHUNK_HEIGHT = 16;

        // These variables store the location of the chunk in the world, e.g. (0,0,0), (32,0,0), (64,0,0)

        public BlockVertex BlockVertex { get; set; } = new();

        public BlockType[,,] Blocks { get; set; }

        public void Generate()
        {
            /*
         * These are just working variables for the algorithm - almost all taken
         * directly from Mikola Lysenko's javascript implementation.
         */
            int i, j, k, l, w, h, u, v, n, side = 0;

            int[] x = new int[] { 0, 0, 0 };
            int[] q = new int[] { 0, 0, 0 };
            int[] du = new int[] { 0, 0, 0 };
            int[] dv = new int[] { 0, 0, 0 };

            /*
             * We create a mask - this will contain the groups of matching voxel faces
             * as we proceed through the chunk in 6 directions - once for each face.
             */
            VoxelFace[] mask = new VoxelFace[CHUNK_WIDTH * CHUNK_HEIGHT];

            /*
             * These are just working variables to hold two faces during comparison.
             */
            VoxelFace voxelFace, voxelFace1;

            /**
             * We start with the lesser-spotted boolean for-loop (also known as the old flippy floppy).
             *
             * The variable backFace will be TRUE on the first iteration and FALSE on the second - this allows
             * us to track which direction the indices should run during creation of the quad.
             *
             * This loop runs twice, and the inner loop 3 times - totally 6 iterations - one for each
             * voxel face.
             */
            for (bool backFace = true, b = false; b != backFace; backFace = backFace && b, b = !b)
            {
                /*
                 * We sweep over the 3 dimensions - most of what follows is well described by Mikola Lysenko
                 * in his post - and is ported from his Javascript implementation.  Where this implementation
                 * diverges, I've added commentary.
                 */
                for (int d = 0; d < 3; d++)
                {
                    u = (d + 1) % 3;
                    v = (d + 2) % 3;

                    x[0] = 0;
                    x[1] = 0;
                    x[2] = 0;

                    q[0] = 0;
                    q[1] = 0;
                    q[2] = 0;
                    q[d] = 1;

                    /*
                     * Here we're keeping track of the side that we're meshing.
                     */
                    if (d == 0) { side = backFace ? VoxelFace.WEST : VoxelFace.EAST; }
                    else if (d == 1) { side = backFace ? VoxelFace.BOTTOM : VoxelFace.TOP; }
                    else if (d == 2) { side = backFace ? VoxelFace.SOUTH : VoxelFace.NORTH; }

                    /*
                     * We move through the dimension from front to back
                     */
                    for (x[d] = -1; x[d] < CHUNK_WIDTH;)
                    {
                        /*
                         * -------------------------------------------------------------------
                         *   We compute the mask
                         * -------------------------------------------------------------------
                         */
                        n = 0;

                        for (x[v] = 0; x[v] < CHUNK_HEIGHT; x[v]++)
                        {
                            for (x[u] = 0; x[u] < CHUNK_WIDTH; x[u]++)
                            {
                                /*
                                 * Here we retrieve two voxel faces for comparison.
                                 */
                                voxelFace = (x[d] >= 0) ? GetVoxelFace(x[0], x[1], x[2], side) : null;
                                voxelFace1 = (x[d] < CHUNK_WIDTH - 1) ? GetVoxelFace(x[0] + q[0], x[1] + q[1], x[2] + q[2], side) : null;

                                /*
                                 * Note that we're using the equals function in the voxel face class here, which lets the faces
                                 * be compared based on any number of attributes.
                                 *
                                 * Also, we choose the face to add to the mask depending on whether we're moving through on a backface or not.
                                 */
                                mask[n++] = ((voxelFace != null && voxelFace1 != null && voxelFace.Equals(voxelFace1)))
                                            ? null
                                            : backFace ? voxelFace1 : voxelFace;
                            }
                        }

                        x[d]++;

                        /*
                         * Now we generate the mesh for the mask
                         */
                        n = 0;

                        for (j = 0; j < CHUNK_HEIGHT; j++)
                        {
                            for (i = 0; i < CHUNK_WIDTH;)
                            {
                                if (mask[n] != null)
                                {
                                    /*
                                     * We compute the width
                                     */
                                    for (w = 0; i + w < CHUNK_WIDTH && mask[n + w] != null && mask[n + w].Equals(mask[n]); w++) { }

                                    /*
                                     * Then we compute height
                                     */
                                    bool done = false;

                                    for (h = 0; j + h < CHUNK_HEIGHT; h++)
                                    {
                                        for (k = 0; k < w; k++)
                                        {
                                            if (mask[n + k + h * CHUNK_WIDTH] == null || !mask[n + k + h * CHUNK_WIDTH].Equals(mask[n])) { done = true; break; }
                                        }

                                        if (done) { break; }
                                    }

                                    /*
                                     * Here we check the "transparent" attribute in the VoxelFace class to ensure that we don't mesh
                                     * any culled faces.
                                     */
                                    if (!mask[n].transparent)
                                    {
                                        /*
                                         * Add quad
                                         */
                                        x[u] = i;
                                        x[v] = j;

                                        du[0] = 0;
                                        du[1] = 0;
                                        du[2] = 0;
                                        du[u] = w;

                                        dv[0] = 0;
                                        dv[1] = 0;
                                        dv[2] = 0;
                                        dv[v] = h;

                                        /*
                                         * And here we call the quad function in order to render a merged quad in the scene.
                                         *
                                         * We pass mask[n] to the function, which is an instance of the VoxelFace class containing
                                         * all the attributes of the face - which allows for variables to be passed to shaders - for
                                         * example lighting values used to create ambient occlusion.
                                         */
                                        BlockVertex.AppendQuad(new Vector3(x[0], x[1], x[2]),
                                             new Vector3(x[0] + du[0], x[1] + du[1], x[2] + du[2]),
                                             new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]),
                                             new Vector3(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]),
                                             w,
                                             h,
                                             mask[n],
                                             backFace);
                                    }

                                    /*
                                     * We zero out the mask
                                     */
                                    for (l = 0; l < h; ++l)
                                    {
                                        for (k = 0; k < w; ++k) { mask[n + k + l * CHUNK_WIDTH] = null; }
                                    }

                                    /*
                                     * And then finally increment the counters and continue
                                     */
                                    i += w;
                                    n += w;
                                }
                                else
                                {
                                    i++;
                                    n++;
                                }
                            }
                        }
                    }
                }
            }
        }

        private VoxelFace GetVoxelFace(int v1, int v2, int v3, int side)
        {
            var block = Blocks[v1, v2, v3];
            if (block.IsEmpty) return null;
            var face = new VoxelFace();
            face.side = side;
            face.type = block;
            face.transparent = block.Transparent;
            return face;
        }
    }
}