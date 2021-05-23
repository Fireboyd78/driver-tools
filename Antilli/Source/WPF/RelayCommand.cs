using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Antilli
{
    public class RelayCommand<T> : ICommand
    {
        public delegate bool CanExecuteHandler(T obj);
        public delegate void ExecuteHandler(T obj);

        public CanExecuteHandler CanExecuteDelegate { get; set; }

        public ExecuteHandler ExecuteDelegate { get; set; }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            try
            {
                if (CanExecuteDelegate == null)
                    return true;

                return CanExecuteDelegate((T)parameter);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Execute(object parameter)
        {
            var handler = ExecuteDelegate;
            if (handler != null)
                handler((T)parameter);
        }

        public RelayCommand() { }
        public RelayCommand(ExecuteHandler execute) : this(execute, null) { }
        public RelayCommand(ExecuteHandler execute, CanExecuteHandler canExecute)
        {
            ExecuteDelegate = execute;
            CanExecuteDelegate = canExecute;
        }
    }

    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand() { }

        public RelayCommand(ExecuteHandler execute)
            : base(execute, null) { }

        public RelayCommand(ExecuteHandler execute, CanExecuteHandler canExecute)
            : base(execute, canExecute) { }
    }
}
