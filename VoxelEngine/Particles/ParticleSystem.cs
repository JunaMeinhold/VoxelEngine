using HexaEngine.Resources;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Shaders;
using HexaEngine.Windows;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace HexaEngine.Particles
{
    public class ParticleSystem : IDisposable
    {
        // Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct ParticleType
        {
            public float positionX, positionY, positionZ;
            public float red, green, blue;
            public float velocity;
            public bool active;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexType
        {
            public Vector3 position;
            public Vector2 texture;
            public Vector4 color;
        }

        // Variables
        private float m_ParticleDeviationX, m_ParticleDeviationY, m_ParticleDeviationZ;

        private float m_ParticleVelocity, m_ParticleVelocityVariation;
        private float m_ParticleSize, m_ParticlesPerSecond;
        private int m_MaxParticles;
        private int m_CurrentParticleCount;
        private float m_AccumulatedTime;
        private bool disposedValue;

        public ID3D11Buffer VertexBuffer { get; set; }
        public ID3D11Buffer IndexBuffer { get; set; }
        private int VertexCount { get; set; }
        public int IndexCount { get; private set; }
        public Texture Texture { get; private set; }
        public ParticleType[] ParticleList { get; set; }
        public VertexType[] Vertices { get; set; }

        public Matrix4x4 GlobalPose { get; set; } = Matrix4x4.Identity;

        public ParticleShader ParticleShader { get; set; }

        // Methods.
        public bool Initialize(string textureFileName)
        {
            Texture = ResourceManager.LoadTexture(textureFileName);

            // Initialize the particle system.
            if (!InitializeParticleSystem())
                return false;

            // Create the buffers that will be used to render the particles with.
            if (!InitializeBuffers(DeviceManager.Current.ID3D11Device))
                return false;

            return true;
        }

        private bool InitializeParticleSystem()
        {
            // Set the random deviation of where the particles can be located when emitted.
            m_ParticleDeviationX = 2.5f;
            m_ParticleDeviationY = 0.1f;
            m_ParticleDeviationZ = 0.2f;

            // Set the speed and speed variation of particles.
            m_ParticleVelocity = 2.0f;
            m_ParticleVelocityVariation = 0.05f;

            // Set the physical size of the particles.
            m_ParticleSize = 0.2f;
            // Set the number of particles to emit per second.
            m_ParticlesPerSecond = 0.001f;
            // Set the maximum number of particles allowed in the particle system.
            m_MaxParticles = 30000;
            // Create the particle list.
            ParticleList = new ParticleType[m_MaxParticles];

            // Initialize the particle list.
            for (var i = 0; i < m_MaxParticles; i++)
                ParticleList[i].active = false;

            // Initialize the current particle count to zero since none are emitted yet.
            m_CurrentParticleCount = 0;

            // Clear the initial accumulated time for the particle per second emission rate.
            m_AccumulatedTime = 0.0f;

            return true;
        }

        private bool InitializeBuffers(ID3D11Device device)
        {
            try
            {
                // Set the maximum number of vertices in the vertex array.
                VertexCount = m_MaxParticles * 6;
                // Set the maximum number of indices in the index array.
                IndexCount = VertexCount;

                // Create the vertex array for the particles that will be rendered.
                Vertices = new VertexType[VertexCount];
                // Create the index array.
                var indices = new int[IndexCount];

                // Initialize the index array.
                for (var i = 0; i < IndexCount; i++)
                    indices[i] = i;

                // Set up the description of the dynamic vertex buffer.
                var vertexBufferDescription = new BufferDescription()
                {
                    Usage = ResourceUsage.Dynamic,
                    SizeInBytes = Marshal.SizeOf<VertexType>() * VertexCount,
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    StructureByteStride = 0
                };

                // Create the Dynamic vertex buffer.
                VertexBuffer = device.CreateBuffer(Vertices, vertexBufferDescription);
                // VertexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, Vertices);

                // Create the static index buffer.
                IndexBuffer = device.CreateBuffer(BindFlags.IndexBuffer, indices);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Tick(float frameTime, DeviceManager manager)
        {
            // Release old particles.
            KillParticles();

            // Emit new particles.
            EmitParticles(frameTime);

            // Update the position of the particles.
            UpdateParticles(frameTime);

            // Update the dynamic vertex buffer with the new position of each particle.
            _ = UpdateBuffers(manager);
        }

        private bool UpdateBuffers(DeviceManager manager)
        {
            // Initialize vertex array to zeros at first.
            Vertices = new VertexType[VertexCount];

            // Now build the vertex array from the particle list array.
            // Each particle is a quad made out of two triangles.
            var index = 0;
            for (var i = 0; i < m_CurrentParticleCount; i++)
            {
                // Bottom left.
                Vertices[index].position = new Vector3(ParticleList[i].positionX - m_ParticleSize, ParticleList[i].positionY - m_ParticleSize, ParticleList[i].positionZ);
                Vertices[index].texture = new Vector2(0.0f, 1.0f);
                Vertices[index].color = new Vector4(ParticleList[i].red, ParticleList[i].green, ParticleList[i].blue, 1.0f);
                index++;

                // Top left.
                Vertices[index].position = new Vector3(ParticleList[i].positionX - m_ParticleSize, ParticleList[i].positionY + m_ParticleSize, ParticleList[i].positionZ);
                Vertices[index].texture = new Vector2(0.0f, 0.0f);
                Vertices[index].color = new Vector4(ParticleList[i].red, ParticleList[i].green, ParticleList[i].blue, 1.0f);
                index++;

                // Bottom right.
                Vertices[index].position = new Vector3(ParticleList[i].positionX + m_ParticleSize, ParticleList[i].positionY - m_ParticleSize, ParticleList[i].positionZ);
                Vertices[index].texture = new Vector2(1.0f, 1.0f);
                Vertices[index].color = new Vector4(ParticleList[i].red, ParticleList[i].green, ParticleList[i].blue, 1.0f);
                index++;

                // Bottom right.
                Vertices[index].position = new Vector3(ParticleList[i].positionX + m_ParticleSize, ParticleList[i].positionY - m_ParticleSize, ParticleList[i].positionZ);
                Vertices[index].texture = new Vector2(1.0f, 1.0f);
                Vertices[index].color = new Vector4(ParticleList[i].red, ParticleList[i].green, ParticleList[i].blue, 1.0f);
                index++;

                // Top left.
                Vertices[index].position = new Vector3(ParticleList[i].positionX - m_ParticleSize, ParticleList[i].positionY + m_ParticleSize, ParticleList[i].positionZ);
                Vertices[index].texture = new Vector2(0.0f, 0.0f);
                Vertices[index].color = new Vector4(ParticleList[i].red, ParticleList[i].green, ParticleList[i].blue, 1.0f);
                index++;

                // Top right.
                Vertices[index].position = new Vector3(ParticleList[i].positionX + m_ParticleSize, ParticleList[i].positionY + m_ParticleSize, ParticleList[i].positionZ);
                Vertices[index].texture = new Vector2(1.0f, 0.0f);
                Vertices[index].color = new Vector4(ParticleList[i].red, ParticleList[i].green, ParticleList[i].blue, 1.0f);
                index++;
            }

            Shader.SWrite(manager, VertexBuffer, Vertices);

            return true;
        }

        private void KillParticles()
        {
            // Kill all the particles that have gone below a certain height range.
            for (var i = 0; i < m_MaxParticles; i++)
            {
                if (ParticleList[i].active == true && ParticleList[i].positionY < -3.0f)
                {
                    ParticleList[i].active = false;
                    m_CurrentParticleCount--;

                    // Now shift all the live particles back up the array to erase the destroyed particle and keep the array sorted correctly.
                    for (var j = i; j < m_MaxParticles - 1; j++)
                    {
                        ParticleList[j].positionX = ParticleList[j + 1].positionX;
                        ParticleList[j].positionY = ParticleList[j + 1].positionY;
                        ParticleList[j].positionZ = ParticleList[j + 1].positionZ;
                        ParticleList[j].red = ParticleList[j + 1].red;
                        ParticleList[j].green = ParticleList[j + 1].green;
                        ParticleList[j].blue = ParticleList[j + 1].blue;
                        ParticleList[j].velocity = ParticleList[j + 1].velocity;
                        ParticleList[j].active = ParticleList[j + 1].active;
                    }
                }
            }
        }

        private void UpdateParticles(float frameTime)
        {
            // Each frame we update all the particles by making them move downwards using their position, velocity, and the frame time.
            for (var i = 0; i < m_CurrentParticleCount; i++)
                ParticleList[i].positionY = ParticleList[i].positionY - ParticleList[i].velocity * frameTime;
        }

        private void EmitParticles(float frameTime)
        {
            int i, j;

            // Increment the frame time.
            m_AccumulatedTime += frameTime;

            // Set emit particle to false for now.
            var emitParticle = false;

            // Check if it is time to emit a new particle or not.
            if (m_AccumulatedTime > m_ParticlesPerSecond)
            {
                m_AccumulatedTime = 0.0f;
                emitParticle = true;
            }

            // If there are particles to emit then emit one per frame.
            if (emitParticle == true && m_CurrentParticleCount < m_MaxParticles - 1)
            {
                m_CurrentParticleCount++;

                // Now generate the randomized particle properties.
                var rand = new Random();

                // Now generate the randomized particle properties.
                var positionX = (rand.Next(32767) - (float)rand.Next(32767)) / 32767.0f * m_ParticleDeviationX;
                var positionY = (rand.Next(32767) - (float)rand.Next(32767)) / 32767.0f * m_ParticleDeviationY;
                var positionZ = (rand.Next(32767) - (float)rand.Next(32767)) / 32767.0f * m_ParticleDeviationZ;

                var velocity = m_ParticleVelocity + (rand.Next(32767) - (float)rand.Next(32767)) / 32767.0f * m_ParticleVelocityVariation;

                var red = (rand.Next(32767) - (float)rand.Next(32767)) / 32767.0f + 0.5f;
                var green = (rand.Next(32767) - (float)rand.Next(32767)) / 32767.0f + 0.5f;
                var blue = (rand.Next(32767) - (float)rand.Next(32767)) / 32767.0f + 0.5f;

                // Now since the particles need to be rendered from back to front for blending we have to sort the particle array.
                // We will sort using Z depth so we need to find where in the list the particle should be inserted.
                var index = 0;
                var found = false;
                while (!found)
                {
                    if (ParticleList[index].active == false || ParticleList[index].positionZ < positionZ)
                        found = true;
                    else
                        index++;
                }

                // Now that we know the location to insert into we need to copy the array over by one position from the index to make room for the new particle.
                i = m_CurrentParticleCount;
                j = i - 1;

                while (i != index)
                {
                    ParticleList[i].positionX = ParticleList[j].positionX;
                    ParticleList[i].positionY = ParticleList[j].positionY;
                    ParticleList[i].positionZ = ParticleList[j].positionZ;
                    ParticleList[i].red = ParticleList[j].red;
                    ParticleList[i].green = ParticleList[j].green;
                    ParticleList[i].blue = ParticleList[j].blue;
                    ParticleList[i].velocity = ParticleList[j].velocity;
                    ParticleList[i].active = ParticleList[j].active;
                    i--;
                    j--;
                }

                // Now insert it into the particle array in the correct depth order.
                ParticleList[index].positionX = positionX;
                ParticleList[index].positionY = positionY;
                ParticleList[index].positionZ = positionZ;
                ParticleList[index].red = red;
                ParticleList[index].green = green;
                ParticleList[index].blue = blue;
                ParticleList[index].velocity = velocity;
                ParticleList[index].active = true;
            }
        }

        public void Render(IView view)
        {
            ParticleShader.Render(view, this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ParticleList = null;
                Texture?.Dispose();
                Texture = null;
                VertexBuffer?.Dispose();
                VertexBuffer = null;
                IndexBuffer?.Dispose();
                IndexBuffer = null;

                disposedValue = true;
            }
        }

        ~ParticleSystem()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}