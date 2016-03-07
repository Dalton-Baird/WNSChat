using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WNSChat.Client.Utilities
{
    /// <summary>
    /// ButtonCommand class that Dalton made for the Anderson Perry Support Ticket Tracker
    /// Not modified from original - last updated: March 3, 2016 10:35 PM
    /// </summary>
    public class ButtonCommand : ICommand
    {
        private Action<object> _Execute; //The action to execute
        private Predicate<object> _CanExecute; //A function that returns when the action can be executed

        /// <summary>
        /// Creates a new ButtonCommand with the specified Execute and CanExecute delegates.
        /// </summary>
        /// <param name="Execute">A delegate to be called on Execute</param>
        /// <param name="CanExecute">A delegate to call to determine the result of CanExecute</param>
        public ButtonCommand(Action<object> Execute, Predicate<object> CanExecute)
        {
            this._Execute = Execute;
            this._CanExecute = CanExecute;
        }

        /// <summary>
        /// Creates a new ButtonCommand that the Execute and CanExecute delagates that don't take any parameters
        /// </summary>
        /// <param name="Execute">A delegate to be called on Execute</param>
        /// <param name="CanExecute">A delegate to call to determine the result of CanExecute</param>
        public ButtonCommand(Action Execute, Func<bool> CanExecute) : this(o => Execute(), o => CanExecute()) { }

        /// <summary>
        /// Creates a new ButtonCommand that can always be executed.
        /// </summary>
        /// <param name="Execute">A delegate to be called on Execute</param>
        public ButtonCommand(Action<object> Execute)
        {
            this._Execute = Execute;
            this._CanExecute = o => true;
        }

        /// <summary>
        /// Creates a new ButtonCommand that can always be executed.  This uses a non-generic Action
        /// that takes no arguments, allowing method references to be used.
        /// </summary>
        /// <param name="Execute">A delegate to be called on Execute</param>
        public ButtonCommand(Action Execute) : this(o => Execute(), o => true) { }

        /// <summary>
        /// Allows a ButtonCommand to execute any number of other ButtonCommmands.  The ButtonCommands
        /// must all return true for CanExecute in order for this command's CanExecute to return true.
        /// </summary>
        /// <param name="ButtonCommands">A list of ButtonCommands to execute</param>
        public ButtonCommand(params ButtonCommand[] ButtonCommands)
        {
            this._Execute = o =>
            {
                foreach (ButtonCommand command in ButtonCommands)
                    command.Execute(o);
            };
            this._CanExecute = o =>
            {
                foreach (ButtonCommand command in ButtonCommands)
                    if (!command.CanExecute(o))
                        return false;
                return true;
            };
        }

        public bool CanExecute(object o)
        {
            return this._CanExecute(o);
        }

        public void Execute(object o)
        {
            this._Execute(o);
        }

        public event EventHandler CanExecuteChanged;
        //public event EventHandler CanExecuteChanged
        //{
        //    add { CommandManager.RequerySuggested += value; }
        //    remove { CommandManager.RequerySuggested -= value; }
        //}

        public void OnCanExecuteChanged(object sender)
        {
            if (this.CanExecuteChanged != null)
                this.CanExecuteChanged(sender, new EventArgs());
        }
    }
}