using System;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    class InvokeCommand : ICommand
    {
        private readonly Action<object> _onExecuted;

        public InvokeCommand(Action<object> onExecuted)
        {
            _onExecuted = onExecuted;
        }

        public bool CanExecute(object parameter)
        {
            return _onExecuted != null;
        }

#pragma warning disable 67 //Required by ICommand but not needed
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public void Execute(object parameter)
        {
            _onExecuted(parameter);
        }
    }
}
