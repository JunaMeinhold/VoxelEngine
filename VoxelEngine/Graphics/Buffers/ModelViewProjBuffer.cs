namespace VoxelEngine.Graphics.Buffers
{
    using System.Numerics;
    using System.Runtime.InteropServices;
    using VoxelEngine.Graphics.D3D11.Interfaces;

    [Obsolete]
    [StructLayout(LayoutKind.Sequential)]
    public struct ModelViewProjBuffer
    {
        public Matrix4x4 Projection;
        public Matrix4x4 View;
        public Matrix4x4 Model;

        public ModelViewProjBuffer(Matrix4x4 projection, Matrix4x4 view, Matrix4x4 model)
        {
            Projection = Matrix4x4.Transpose(projection);
            View = Matrix4x4.Transpose(view);
            Model = Matrix4x4.Transpose(model);
        }

        public ModelViewProjBuffer(IView view, Matrix4x4 model)
        {
            Projection = Matrix4x4.Transpose(view.Transform.Projection);
            View = Matrix4x4.Transpose(view.Transform.View);
            Model = Matrix4x4.Transpose(model);
        }
    }
}