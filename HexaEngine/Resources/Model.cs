using HexaEngine.IO;
using HexaEngine.Models.ObjLoader.Loader.Loaders;
using HexaEngine.Shaders;
using HexaEngine.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Resources
{
    public class Model : Resource
    {
        public Vertex[] Vertices { get; private set; }

        public InstanceType[] Instances { get; set; }

        public int[] Indices { get; private set; }

        public LoadResult ModelResult { get; private set; }

        public VertexBufferView VertexBufferView { get; private set; }

        public VertexBufferView InstanceBufferView { get; private set; }

        public ID3D11Buffer InstanceBuffer { get; private set; }

        public ID3D11Buffer VertexBuffer { get; private set; }

        public ID3D11Buffer IndexBuffer { get; private set; }

        public Format IndexBufferFormat { get; set; } = Format.R32_UInt;

        public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;

        public void Render(ID3D11DeviceContext context, int slot = 0)
        {
            context.IASetVertexBuffers(slot, VertexBufferView);
            context.IASetIndexBuffer(IndexBuffer, IndexBufferFormat, 0);
            context.IASetPrimitiveTopology(Topology);
        }

        public void RenderInstanced(ID3D11DeviceContext context, int slot = 0)
        {
            Shader.SWrite(DeviceManager.Current, InstanceBuffer, Instances);
            context.IASetVertexBuffers(slot, new VertexBufferView[] { VertexBufferView, InstanceBufferView });
            context.IASetIndexBuffer(IndexBuffer, IndexBufferFormat, 0);
            context.IASetPrimitiveTopology(Topology);
        }

        public void Load(DeviceManager manager, params Vertex[] vertices)
        {
            Vertices = vertices;
            Indices = new int[Vertices.Length];
            for (var i = 0; i < Vertices.Length; i++)
            {
                Indices[i] = i;
            }

            IndexBuffer = manager.ID3D11Device.CreateBuffer(Indices, new BufferDescription(Marshal.SizeOf<int>() * Indices.Length, BindFlags.IndexBuffer, ResourceUsage.Default));
            IndexBuffer.DebugName = nameof(IndexBuffer) + ": " + Path.GetFileName("Unknown");
            VertexBuffer = manager.ID3D11Device.CreateBuffer(Vertices, new BufferDescription(Marshal.SizeOf<Vertex>() * Vertices.Length, BindFlags.VertexBuffer, ResourceUsage.Default));
            VertexBuffer.DebugName = nameof(VertexBuffer) + ": " + Path.GetFileName("Unknown");
            VertexBufferView = new VertexBufferView(VertexBuffer, Marshal.SizeOf<Vertex>());
        }

        public void Load(DeviceManager manager, Vertex[] vertices, int[] indices)
        {
            Vertices = vertices;
            Indices = indices;

            IndexBuffer = manager.ID3D11Device.CreateBuffer(Indices, new BufferDescription(Marshal.SizeOf<int>() * Indices.Length, BindFlags.IndexBuffer, ResourceUsage.Default));
            IndexBuffer.DebugName = nameof(IndexBuffer) + ": " + Path.GetFileName("Unknown");
            VertexBuffer = manager.ID3D11Device.CreateBuffer(Vertices, new BufferDescription(Marshal.SizeOf<Vertex>() * Vertices.Length, BindFlags.VertexBuffer, ResourceUsage.Default));
            VertexBuffer.DebugName = nameof(VertexBuffer) + ": " + Path.GetFileName("Unknown");
            VertexBufferView = new VertexBufferView(VertexBuffer, Marshal.SizeOf<Vertex>());
        }

        public void Load(DeviceManager manager, string debugName, int vertexCount, int indexCount)
        {
            IndexBuffer = manager.ID3D11Device.CreateBuffer(new BufferDescription(Marshal.SizeOf<int>() * indexCount, ResourceUsage.Dynamic, BindFlags.IndexBuffer, CpuAccessFlags.Write));
            IndexBuffer.DebugName = nameof(IndexBuffer) + ": " + debugName;
            VertexBuffer = manager.ID3D11Device.CreateBuffer(new BufferDescription(Marshal.SizeOf<Vertex>() * vertexCount, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write));
            VertexBuffer.DebugName = nameof(VertexBuffer) + ": " + debugName;
            VertexBufferView = new VertexBufferView(VertexBuffer, Marshal.SizeOf<Vertex>());
        }

        public void Resize(int vertexCount, int indexCount)
        {
            if (vertexCount == 0) return;
            if (indexCount == 0) return;
            var debugName = IndexBuffer.DebugName;
            var debugName1 = VertexBuffer.DebugName;
            IndexBuffer?.Dispose();
            IndexBuffer = null;
            VertexBuffer.Dispose();
            VertexBuffer = null;
            IndexBuffer = DeviceManager.Current.ID3D11Device.CreateBuffer(new BufferDescription(Marshal.SizeOf<int>() * indexCount, ResourceUsage.Dynamic, BindFlags.IndexBuffer, CpuAccessFlags.Write));
            IndexBuffer.DebugName = debugName;
            VertexBuffer = DeviceManager.Current.ID3D11Device.CreateBuffer(new BufferDescription(Marshal.SizeOf<Vertex>() * vertexCount, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write));
            VertexBuffer.DebugName = debugName1;
            VertexBufferView = new VertexBufferView(VertexBuffer, Marshal.SizeOf<Vertex>());
        }

        public void Update(Vertex[] vertices, int[] indices)
        {
            if (vertices.Length == 0) return;
            if (indices.Length == 0) return;
            Vertices = vertices;
            Indices = indices;
            Shader.SWrite(DeviceManager.Current, VertexBuffer, vertices);
            Shader.SWrite(DeviceManager.Current, IndexBuffer, indices);
        }

        public void LoadObj(DeviceManager manager, string path)
        {
            using var fs = FileSystem.Open(path);
            ObjLoaderFactory factory = new();
            var loader = factory.Create(new MaterialStreamProvider(path.Replace(Path.GetFileName(path), "")));
            ModelResult = loader.Load(fs);
            List<Vertex> vertices = new();

            for (int i = 0; i < ModelResult.Groups.Count; i++)
            {
                for (int j = 0; j < ModelResult.Groups[i].Faces.Count; j++)
                {
                    for (int jj = 0; jj < ModelResult.Groups[i].Faces[j].Count; jj++)
                    {
                        var vertexIndex = ModelResult.Groups[i].Faces[j][jj].VertexIndex - 1;
                        var textureIndex = ModelResult.Groups[i].Faces[j][jj].TextureIndex - 1;
                        var normalIndex = ModelResult.Groups[i].Faces[j][jj].NormalIndex - 1;
                        var vertex = new Vertex(ModelResult.Vertices[vertexIndex], ModelResult.Textures[textureIndex], ModelResult.Normals[normalIndex]);
                        vertex.InvertTexture();
                        vertices.Add(vertex);
                    }
                }
            }

            Vertices = vertices.ToArray();
            Indices = new int[Vertices.Length];
            for (var i = 0; i < Vertices.Length; i++)
            {
                Indices[i] = i;
            }

            IndexBuffer = manager.ID3D11Device.CreateBuffer(Indices, new BufferDescription(Marshal.SizeOf<int>() * Indices.Length, BindFlags.IndexBuffer, ResourceUsage.Default));
            IndexBuffer.DebugName = nameof(IndexBuffer) + ": " + Path.GetFileName(path);
            VertexBuffer = manager.ID3D11Device.CreateBuffer(Vertices, new BufferDescription(Marshal.SizeOf<Vertex>() * Vertices.Length, BindFlags.VertexBuffer, ResourceUsage.Default));
            VertexBuffer.DebugName = nameof(VertexBuffer) + ": " + Path.GetFileName(path);
            VertexBufferView = new VertexBufferView(VertexBuffer, Marshal.SizeOf<Vertex>());
            InstanceBuffer = manager.ID3D11Device.CreateBuffer(new BufferDescription(Marshal.SizeOf<InstanceType>() * 16 * 16 * 128, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write));
            InstanceBuffer.DebugName = nameof(InstanceBuffer) + ": " + Path.GetFileName(path);
            InstanceBufferView = new VertexBufferView(InstanceBuffer, Marshal.SizeOf<InstanceType>());
        }

        protected override void Dispose(bool disposing)
        {
            IndexBuffer?.Dispose();
            IndexBuffer = null;
            VertexBuffer?.Dispose();
            VertexBuffer = null;
            InstanceBuffer?.Dispose();
            InstanceBuffer = null;
            base.Dispose(disposing);
        }
    }
}