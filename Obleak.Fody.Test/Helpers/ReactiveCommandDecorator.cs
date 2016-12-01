using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace Obleak.Fody.Test.Helpers
{
    public class ReactiveCommandDecorator<T> : ReactiveCommand<T>
    {
        public IReactiveCommand Command { get; set; }
        public ReactiveCommandDecorator(IReactiveCommand command) : base(command.CanExecuteObservable, null)
        {
            Command = command;
        }

        public ReactiveCommandDecorator(IObservable<bool> canExecute, Func<object, IObservable<T>> executeAsync, System.Reactive.Concurrency.IScheduler scheduler) : base(canExecute, executeAsync, scheduler)
        {
        }

        public bool IsDisposed { get; set; }

        public override void Dispose()
        {
            IsDisposed = true;
            Command.Dispose();
            base.Dispose();
        }
    }
}
