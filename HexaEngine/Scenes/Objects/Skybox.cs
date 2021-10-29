namespace HexaEngine.Scenes.Objects
{
    using HexaEngine.Resources;
    using HexaEngine.Windows;
    using System;
    using System.Numerics;
    using Vortice.Direct3D11;

    public class Skybox : Disposable
    {
        public Skybox(string path) : this(ResourceManager.LoadCubeMapTexture(path))
        {
        }

        public Skybox(string pathModel, string pathTexture)
        {
            Texture = ResourceManager.LoadCubeMapTexture(pathTexture);
            Model = ResourceManager.LoadModelObj(pathModel);
        }

        public Skybox(Texture texture)
        {
            Texture = texture;
            Model = CreateSphere();
        }

        public Texture Texture { get; }

        public Model Model { get; }

        protected override void Dispose(bool disposing)
        {
            Texture.Dispose();
            Model.Dispose();
        }

        public void Render(ID3D11DeviceContext context)
        {
            Texture.Render(context);
            Model.Render(context);
        }

        private static Model CreateSphere(int LatLines = 10, int LongLines = 10)
        {
            var numSphereVertices = (LatLines - 2) * LongLines + 2;
            var numSphereFaces = (LatLines - 3) * LongLines * 2 + LongLines * 2;
            var vertices = new Vertex[numSphereVertices];

            Vector4 currVertPos;

            vertices[0].Position.X = 0.0f;
            vertices[0].Position.Y = 0.0f;
            vertices[0].Position.Z = 1.0f;

            for (var i = 0; i < LatLines - 2; ++i)
            {
                var spherePitch = (float)((i + 1f) * (3.14f / (LatLines - 1f)));
                var Rotationx = Matrix4x4.CreateRotationX(spherePitch);
                for (var j = 0; j < LongLines; ++j)
                {
                    var sphereYaw = j * (6.28f / LongLines);
                    var Rotationy = Matrix4x4.CreateRotationZ(sphereYaw);
                    currVertPos = Vector4.Transform(new Vector4(0.0f, 0.0f, 1.0f, 0.0f), Rotationx * Rotationy);
                    currVertPos = Vector4.Normalize(currVertPos);
                    vertices[i * LongLines + j + 1].Position.X = currVertPos.X;
                    vertices[i * LongLines + j + 1].Position.Y = currVertPos.Y;
                    vertices[i * LongLines + j + 1].Position.Z = currVertPos.Z;
                }
            }

            vertices[numSphereVertices - 1].Position.X = 0.0f;
            vertices[numSphereVertices - 1].Position.Y = 0.0f;
            vertices[numSphereVertices - 1].Position.Z = -1.0f;

            var indices = new int[numSphereFaces * 3];

            var k = 0;
            for (var l = 0; l < LongLines - 1; ++l)
            {
                indices[k] = 0;
                indices[k + 1] = l + 1;
                indices[k + 2] = l + 2;
                k += 3;
            }

            indices[k] = 0;
            indices[k + 1] = LongLines;
            indices[k + 2] = 1;
            k += 3;

            for (var i = 0; i < LatLines - 3; ++i)
            {
                for (var j = 0; j < LongLines - 1; ++j)
                {
                    indices[k] = i * LongLines + j + 1;
                    indices[k + 1] = i * LongLines + j + 2;
                    indices[k + 2] = (i + 1) * LongLines + j + 1;

                    indices[k + 3] = (i + 1) * LongLines + j + 1;
                    indices[k + 4] = i * LongLines + j + 2;
                    indices[k + 5] = (i + 1) * LongLines + j + 2;

                    k += 6; // next quad
                }

                indices[k] = i * LongLines + LongLines;
                indices[k + 1] = i * LongLines + 1;
                indices[k + 2] = (i + 1) * LongLines + LongLines;

                indices[k + 3] = (i + 1) * LongLines + LongLines;
                indices[k + 4] = i * LongLines + 1;
                indices[k + 5] = (i + 1) * LongLines + 1;

                k += 6;
            }

            for (var l = 0; l < LongLines - 1; ++l)
            {
                indices[k] = numSphereVertices - 1;
                indices[k + 1] = numSphereVertices - 1 - (l + 1);
                indices[k + 2] = numSphereVertices - 1 - (l + 2);
                k += 3;
            }

            indices[k] = numSphereVertices - 1;
            indices[k + 1] = numSphereVertices - 1 - LongLines;
            indices[k + 2] = numSphereVertices - 2;

            Model model = new();
            model.Load(DeviceManager.Current, vertices, indices);
            return model;
        }
    }
}