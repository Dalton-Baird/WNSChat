using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Packets
{
    public class PacketPing : Packet
    {
        public string SendingUsername { get; set; }
        public string DestinationUsername { get; set; }
        public State PacketState { get; set; }

        public List<Tuple<string, DateTime>> Timestamps { get; set; }

        public override void ReadFromStream(Stream stream, BinaryReader reader)
        {
            this.SendingUsername = reader.ReadString();
            this.DestinationUsername = reader.ReadString();
            this.PacketState = (State)reader.ReadInt32();

            int listSize = reader.ReadInt32();
            this.Timestamps = new List<Tuple<string, DateTime>>(listSize);

            //Read the tuples from the stream
            for (int i = 0; i < listSize; i++)
            {
                string username = reader.ReadString();
                DateTime time = DateTime.FromBinary(reader.ReadInt64());
                this.Timestamps.Add(new Tuple<string, DateTime>(username, time));
            }
        }

        public override void WriteToStream(Stream stream, BinaryWriter writer)
        {
            writer.Write(this.SendingUsername);
            writer.Write(this.DestinationUsername);
            writer.Write((Int32)this.PacketState);

            writer.Write((Int32)this.Timestamps.Count); //Write the length of the timestamp list

            //Write the tuples to the stream
            foreach (Tuple<string, DateTime> timestamp in this.Timestamps)
            {
                writer.Write(timestamp.Item1);
                writer.Write((Int64)timestamp.Item2.ToBinary());
            }
        }

        /** Adds a timestamp with the specified username */
        public void AddTimestamp(string username)
        {
            if (this.Timestamps == null) //Make the list if it is null
                this.Timestamps = new List<Tuple<string, DateTime>>();

            this.Timestamps.Add(new Tuple<string, DateTime>(username, DateTime.Now)); //Add the timestamp
        }

        /** Returns the traced ping data formatted as a string */
        public string Trace()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Ping Trace:\n");

            DateTime? firstTime = this.Timestamps.FirstOrDefault()?.Item2;

            sb.Append($"\t{"User",-20} Timestamp\n\n");

            foreach (Tuple<string, DateTime> timestamp in this.Timestamps)
            {
                TimeSpan timeOffset = timestamp.Item2 - DateTime.Today; //Default offset from today

                if (firstTime != null)
                    timeOffset = timestamp.Item2 - (DateTime)firstTime; //Offset from the first timestamp

                sb.Append($"\t{timestamp.Item1,-20} {timeOffset.TotalMilliseconds} ms\n");
            }

            DateTime? lastTime = this.Timestamps.LastOrDefault()?.Item2;

            if (firstTime != null && lastTime != null)
                sb.Append($"\nTotal time: {((TimeSpan)(lastTime - firstTime)).TotalMilliseconds} ms\n");

            return sb.ToString();
        }

        public enum State
        {
            GOING_TO,
            GOING_BACK
        }
    }
}
