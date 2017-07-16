using System;
using System.Collections.Generic;
using System.Text;

namespace MipsSharp
{
    public abstract class MipsSharpException : Exception
    {
        public abstract string ExtendedInformation { get; }

        public MipsSharpException(string message)
            : base(message)
        {

        }

        public MipsSharpException(string message, Exception inner)
            : base(message, inner)
        {

        }
        
        public override string ToString()
        {
            var top = base.ToString();
            var ext = ExtendedInformation;

            if (!string.IsNullOrWhiteSpace(ext))
            {
                return string.Join(
                    Environment.NewLine + Environment.NewLine,
                    new[] { top, ext }
                );
            }

            return top;
        }
    }
}
