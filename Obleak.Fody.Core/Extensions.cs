using System;
using System.Reactive.Disposables;

namespace Obleak.Fody.Core
{
    public static class Extensions
    {
        public static IDisposable HandleWith(this IDisposable disposable, CompositeDisposable composite)
        {
            composite.Add(disposable);
            return disposable;
        }
    }
}
