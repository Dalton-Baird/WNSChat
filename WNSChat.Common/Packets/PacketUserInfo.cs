using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Common.Cmd;

namespace WNSChat.Common.Packets
{
    public class PacketUserInfo : Packet
    {
        public string Username { get; set; }
        public PermissionLevel PermissionLevel { get; set; }

        public override void ReadFromStream(Stream stream, BinaryReader reader)
        {
            this.Username = reader.ReadString();
            this.PermissionLevel = (PermissionLevel)reader.ReadInt32();
        }

        public override void WriteToStream(Stream stream, BinaryWriter writer)
        {
            writer.Write(this.Username);
            writer.Write((Int32)this.PermissionLevel);
        }
    }
}
