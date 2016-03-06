using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Packets
{
    public class PacketSimpleMessage : Packet
    {
        public string Message { get; set; }

        public override void ReadFromStream(Stream stream, BinaryReader reader)
        {
            this.Message = reader.ReadString();
        }

        public override void WriteToStream(Stream stream, BinaryWriter writer)
        {
            writer.Write(this.Message);
        }
    }
}
