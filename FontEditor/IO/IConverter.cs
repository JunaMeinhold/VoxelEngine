using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FontEditor.IO
{
    public interface IConverter<Tin, Tout>
    {
        public Tout Convert(Tin t);
    }
}