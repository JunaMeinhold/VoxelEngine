namespace HexaEngine.Scenes.Interfaces
{
    using HexaEngine.Scripting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface ILightRenderer
    {
        public void Render(List<HexaElement> elements, ILight light);
    }
}