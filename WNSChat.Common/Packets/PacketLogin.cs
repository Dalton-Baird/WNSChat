using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Packets
{
    public class PacketLogin : Packet
    {
        public uint ProtocolVersion { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        public override void ReadFromStream(Stream stream, BinaryReader reader)
        {
            this.ProtocolVersion = reader.ReadUInt32();
            this.Username = reader.ReadString();
            this.PasswordHash = reader.ReadString();
        }

        public override void WriteToStream(Stream stream, BinaryWriter writer)
        {
            writer.Write(this.ProtocolVersion);
            writer.Write(this.Username);
            writer.Write(this.PasswordHash);
        }
    }
}
