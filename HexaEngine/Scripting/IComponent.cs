namespace HexaEngine.Scripting
{
    public interface IComponent
    {
        public void Initialize(HexaElement element);

        public void Update();

        public void Uninitialize();
    }
}