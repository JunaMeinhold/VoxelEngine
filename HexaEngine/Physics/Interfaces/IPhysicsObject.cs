namespace HexaEngine.Physics.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IPhysicsObject
    {
        public Actor Actor { get; set; }
    }
}