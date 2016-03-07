using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Common.Packets;

namespace WNSChat.Common
{
    public class NetworkManager
    {
        /** The network protocol version */
        public const uint ProtocolVersion = 1;

        /** The single instance of the NetworkManager */
        private static NetworkManager _Instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new NetworkManager();
                return _Instance;
            }
        }

        /** The packet map */
        private Dictionary<byte, Type> PacketMap;

        /** The last used packet ID */
        private byte PacketID;

        private NetworkManager()
        {
            this.PacketID = 0;
            this.PacketMap = new Dictionary<byte, Type>(256);

            //Register packets
            this.RegisterPacketType(typeof(PacketSimpleMessage));
            this.RegisterPacketType(typeof(PacketLogin));
            this.RegisterPacketType(typeof(PacketServerInfo));
        }

        /// <summary>
        /// Registers a packet type
        /// </summary>
        /// <param name="packetType">The Type of the packet to register</param>
        public void RegisterPacketType(Type packetType)
        {
            if (!typeof(Packet).IsAssignableFrom(packetType))
                throw new ArgumentException(packetType + " does not inherit from Packet!", "packetType");

            if (this.PacketID >= byte.MaxValue)
                throw new IndexOutOfRangeException("The maximum amount of packet types has been exceded.");

            this.PacketMap.Add(this.PacketID++, packetType);
        }

        /// <summary>
        /// Writes a packet to the stream
        /// </summary>
        /// <param name="stream">The Stream to write to</param>
        /// <param name="packet">The packet to write to the stream</param>
        public void WritePacket(Stream stream, Packet packet)
        {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                byte packetID = this.PacketMap.Single(kvp => kvp.Value == packet.GetType()).Key;

                writer.Write(packetID);
                packet.WriteToStream(stream, writer);
            }
        }

        /// <summary>
        /// Reads and returns a packet from the stream.  Blocks until a packet comes in.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <returns>The packet read from the stream</returns>
        public Packet ReadPacket(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                byte packetID = reader.ReadByte();

                Type packetType = this.PacketMap[packetID];
                Packet packet = Activator.CreateInstance(packetType) as Packet;

                if (packet == null)
                    throw new InvalidCastException("Unable to instantiate packetType and cast to Packet");

                packet.ReadFromStream(stream, reader);

                return packet;
            }
        }
    }
}
