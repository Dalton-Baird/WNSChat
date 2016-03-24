using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Messages
{
    public abstract class Message : INotifyPropertyChanged
    {
        public Message()
        {
            this.CreatedDate = DateTime.Now; //Set the sent date
        }

        /** The date that the message was sent */
        private DateTime _CreatedDate;
        public DateTime CreatedDate
        {
            get { return this._CreatedDate; }
            set
            {
                this._CreatedDate = value;
                this.OnPropertyChanged();
            }
        }

        public abstract override string ToString(); //You must implement this for the console log

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        /** Allows strings to be implicitly converted to a MessageText */
        public static implicit operator Message(string str) => new MessageText(str);
    }
}
