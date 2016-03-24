using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Messages
{
    public class MessageText : Message
    {
        public MessageText() : this(null) { }

        public MessageText(string text)
        {
            this.Text = text;
        }

        private string _Text;
        public string Text
        {
            get { return this._Text; }
            set
            {
                this._Text = value;
                this.OnPropertyChanged();
            }
        }

        public override string ToString() => this.Text;
    }
}
