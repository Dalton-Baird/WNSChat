using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Packets
{
    public abstract class Packet
    {
        /// <summary>
        /// Writes data to the stream.  Both parameters should not be disposed of.
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="writer">A BinaryWriter linked to the stream</param>
        public abstract void WriteToStream(Stream stream, BinaryWriter writer);

        /// <summary>
        /// Reads data from the stream.  Both parameters should not be disposed of.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="reader">A BinaryReader linked to the stream</param>
        public abstract void ReadFromStream(Stream stream, BinaryReader reader);

        public override string ToString()
        {
            return $"{this.GetType().Name}";
        }
    }
}
