using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WNSChat.Utilities.WindowExtensions
{
    public static class WindowExtensions
    {
        /// <summary>
        /// Centers the window on another window.  Returns the window for chaining.
        /// </summary>
        /// <typeparam name="W">The type of the window that this is called on</typeparam>
        /// <param name="window">The window that this is called on</param>
        /// <param name="other">The window to center on</param>
        /// <returns></returns>
        public static W CenterOnWindow<W>(this W window, Window other) where W : Window
        {
            window.Left = other.Left + other.Width / 2 - window.Width / 2;
            window.Top = other.Top + other.Height / 2 - window.Height / 2;

            return window;
        }
    }
}
