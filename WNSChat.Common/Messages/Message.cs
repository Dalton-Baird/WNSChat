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
        public abstract override string ToString(); //You must implement this for the console log

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
