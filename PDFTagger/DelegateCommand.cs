using System;
using System.Diagnostics;
using System.Windows.Input;
using PDFTagger.ExtensionMethods;

namespace PDFTagger {

    public class DelegateCommand : DelegateCommand<object> {
        public DelegateCommand(Action execute) : base(obj => execute()) { }
        public DelegateCommand(Action execute, Predicate<object> canExecute) : base(obj => execute(), canExecute) { }
    }

    public class DelegateCommandWithCanExecute : ICommand {

        private bool canExecuteValue;
        public bool CanExecuteValue {
            get { return canExecuteValue; }
            set {
                if (value != canExecuteValue) {
                    canExecuteValue = value;
                    CanExecuteChanged.Raise("CanExecuteChanged", this);
                }
            }
        }

        private Action execute;

        public DelegateCommandWithCanExecute(Action execute, bool canExecute = true) {
            this.execute = execute;
            canExecuteValue = canExecute;
        }

        public void Execute(object parameter) {
            if (execute != null) {
                execute();
            }
        }

        public bool CanExecute(object parameter) {
            return CanExecuteValue;
        }

        public void RemoveActions() {
            execute = null;
        }

        public event EventHandler CanExecuteChanged;
    }

    public class DelegateCommand<T> : ICommand {

        private Action<T> execute;
        private Predicate<T> canExecute;
        private static readonly Predicate<T> CanAlwaysExecute = p => true;

        public DelegateCommand(Action<T> execute)
            : this(execute, CanAlwaysExecute) {
        }

        public DelegateCommand(Action<T> execute, Predicate<T> canExecute) {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public void RemoveActions() {
            execute = null;
            canExecute = null;
        }

        public void Execute(object parameter) {
            if (execute != null) {
                execute((T)parameter);
            }
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter) {
            return canExecute == null || canExecute((T)parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void FireCanExecuteChanged() {
            CanExecuteChanged.Raise("CanExecuteChanged", this);
        }
    }

    public class DelegateCommandWithCanExecute<T> : ICommand {

        private Action<T> execute;

        private bool canExecuteValue;
        public bool CanExecuteValue {
            get { return canExecuteValue; }
            set {
                if (value != canExecuteValue) {
                    canExecuteValue = value;
                    CanExecuteChanged.Raise("CanExecuteChanged", this);
                }
            }
        }

        public DelegateCommandWithCanExecute(Action<T> execute, bool canExecute = true) {
            this.execute = execute;
            canExecuteValue = canExecute;
        }

        public void Execute(object parameter) {
            if (execute != null) {
                execute((T)parameter);
            }
        }

        public bool CanExecute(object parameter) {
            return CanExecuteValue;
        }

        public void RemoveActions() {
            execute = null;
        }

        public event EventHandler CanExecuteChanged;
    }
}
