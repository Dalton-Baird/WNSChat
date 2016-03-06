using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Packets
{
    public class PacketServerInfo : Packet
    {
        public int UserCount { get; set; }
        public bool PasswordRequired { get; set; }

        public override void ReadFromStream(Stream stream, BinaryReader reader)
        {
            this.UserCount = reader.ReadInt32();
            this.PasswordRequired = reader.ReadBoolean();
        }

        public override void WriteToStream(Stream stream, BinaryWriter writer)
        {
            writer.Write(this.UserCount);
            writer.Write(this.PasswordRequired);
        }
    }
}
