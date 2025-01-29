namespace App.Pipelines.Effects
{
    using Hexa.NET.D3D11;

    public interface IEffect
    {
        bool Enabled { get; set; }

        EffectFlags Flags { get; }

        public void Update(ID3D11DeviceContext context);

        public void PrePass(ID3D11DeviceContext context)
        {
        }

        public void Pass(ID3D11DeviceContext context);
    }
}