namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Resources;

    public class BlendState : Resource
    {
        private ID3D11BlendState blendState;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlendState"/> class.
        /// </summary>
        /// <param name="description">The description.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlendState(ID3D11Device device, BlendDescription description)
        {
            blendState = device.CreateBlendState(description);
            blendState.DebugName = nameof(BlendState);
            Description = description;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BlendState(ID3D11BlendState state)
        {
            blendState = state;
            Description = state.Description;
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public BlendDescription Description { get; }

        /// <summary>
        /// Sets the state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetState(ID3D11DeviceContext context)
        {
            context.OMSetBlendState(blendState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlendState GetState(ID3D11DeviceContext context)
        {
            return new(context.OMGetBlendState());
        }

        protected override void Dispose(bool disposing)
        {
            blendState.Dispose();
            blendState = null;
        }
    }
}