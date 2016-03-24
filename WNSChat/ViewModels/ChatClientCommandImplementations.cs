using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Common;
using WNSChat.Common.Cmd;
using WNSChat.Common.Messages;
using WNSChat.Common.Packets;

namespace WNSChat.ViewModels
{
    /// <summary>
    /// The part of the ChatClientViewModel class that initializes and uninitailizes the client's handling of the commands.
    /// </summary>
    public partial class ChatClientViewModel
    {
        private void InitCommands()
        {
            List<Command> commandsToNotSend = new List<Command>();

            Commands.Say.Execute += (u, s) =>
            {
                NetworkManager.Instance.WritePacket(this.Server.Stream, new PacketSimpleMessage() { Message = s });
            };
            commandsToNotSend.Add(Commands.Say);

            Commands.Logout.Execute += (u, s) =>
            {
                string disconnectReason = "Logging out";
                if (this.DisconnectCommand.CanExecute(disconnectReason))
                    this.DisconnectCommand.Execute(disconnectReason);
            };
            commandsToNotSend.Add(Commands.Logout);

            Commands.Ping.Execute += (u, s) =>
            {
                string usernameToPing = s.Trim();

                PacketPing packet = new PacketPing()
                { DestinationUsername = usernameToPing, PacketState = PacketPing.State.GOING_TO, SendingUsername = u.Username };

                packet.AddTimestamp(u.Username); //Add a timestamp now

                NetworkManager.Instance.WritePacket(this.Server.Stream, packet); //Send the packet
            };
            commandsToNotSend.Add(Commands.Ping);

            Commands.Clear.Execute += (u, s) =>
            {
                //Dispose of the messages if they need disposed of
                foreach (Message message in this.MessageLog)
                    if (message is IDisposable)
                        ((IDisposable)message).Dispose();

                this.MessageLog.Clear(); //Clears the message log
            };
            commandsToNotSend.Add(Commands.Clear);

            //Hook up unhandled commands to the say command so that the server can handle them
            foreach (Command command in Commands.AllCommands)
                if (!commandsToNotSend.Contains(command))
                    command.Execute += (u, s) => Commands.Say.OnExecute(u, $"/{command.Name} {s}");
        }

        private void UnInitCommands()
        {
            foreach (Command command in Commands.AllCommands)
                command.ClearExecuteHandlers();
        }
    }
}
