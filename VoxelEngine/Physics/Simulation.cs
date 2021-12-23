namespace HexaEngine.Physics
{
    using HexaEngine.Mathematics;
    using HexaEngine.Objects;
    using HexaEngine.Objects.VoxelGen;
    using System.Collections.Generic;
    using System.Numerics;

    public class Simulation
    {
        public World World { get; set; }

        public List<Actor> Actors { get; set; }

        public Vector3 Gravity { get; set; } = new(0, -9.81f, 0);

        public bool IsSimulating { get; set; }

        public void Step(float delta)
        {
            if (!IsSimulating) return;

            foreach (Actor actor in Actors)
            {
                Vector3 chunkPos = actor.Position.Floor() / Chunk.CHUNK_SIZE;
                var region = World.GetRegion(chunkPos);
            }
        }
    }
}