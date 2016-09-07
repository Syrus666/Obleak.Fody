#undef DEBUG_WEAVE

using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using ReactiveUI;

namespace Obleak.Fody.Test
{
    public abstract class TestBase
    {
        protected TestBase()
        {
#if DEBUG_WEAVE
            WeaveDebugHelper.WeaveTestAssembly();
#endif
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
        }

        protected bool IsExpectedDisposableTestContainerInitialState(CompositeDisposable compositeDisposable, int expectedNumberOfDisposables)
        {
            return compositeDisposable.Count == expectedNumberOfDisposables && // Validate count
                   // Where we can, ensure that *any* of them are not disposed
                   // Observable.FromAsync(_ => Task.FromResult(true)).Subscribe() will dispose before obj dispose
                   // This is ok & expected! The validation here is that even though we're tracking it
                   // we can still call Dispose later with a NOP, but we still have something live to validate
                   // is cleaned up correctly
                   compositeDisposable.OfType<CompositeDisposable>().Any(d => !d.IsDisposed); 
        }
    }
}
