using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MipsSharp.Zelda64.Overlay;

namespace MipsSharp.Exceptions
{
    public class RelocationException : MipsSharpException
    {
        public override string ExtendedInformation { get; }

        public RelocationException(string message, IEnumerable<Relocation> relocations)
            : this(message, null, relocations)
        {

        }

        public RelocationException(string message, Exception inner, IEnumerable<Relocation> relocations)
            : this(message, inner, relocations, null)
        {

        }

        public RelocationException(string message, IEnumerable<Relocation> relocations, uint? offending)
            : this(message, null, relocations, offending)
        {

        }

        public RelocationException(string message, Exception inner, IEnumerable<Relocation> relocations, uint? offending)
            : base(message)
        {
            ExtendedInformation =
                string.Join(
                    Environment.NewLine,
                    "Relocation table dump:",
                    string.Join(
                        Environment.NewLine,
                        relocations.Select(x => 
                            offending == null || offending.Value != x.Location
                            ? "    " + x
                            : " >> " + x
                        )
                    )
                );
        }
    }
}
