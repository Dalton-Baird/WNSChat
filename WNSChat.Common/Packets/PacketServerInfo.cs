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
        public uint ProtocolVersion { get; set; }
        public int UserCount { get; set; }
        public bool PasswordRequired { get; set; }
        public string ServerName { get; set; }

        public override void ReadFromStream(Stream stream, BinaryReader reader)
        {
            this.ProtocolVersion = reader.ReadUInt32();
            this.UserCount = reader.ReadInt32();
            this.PasswordRequired = reader.ReadBoolean();
            this.ServerName = reader.ReadString();
        }

        public override void WriteToStream(Stream stream, BinaryWriter writer)
        {
            writer.Write(this.ProtocolVersion);
            writer.Write(this.UserCount);
            writer.Write(this.PasswordRequired);
            writer.Write(this.ServerName);
        }
    }
}
