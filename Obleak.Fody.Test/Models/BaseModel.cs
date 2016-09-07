using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace Obleak.Fody.Test.Models
{
    public abstract class TestModelBase : ReactiveObject, IDisposable
    {
        /// <summary>
        /// Add disposables to this but DO NOT call it's dispose.
        /// This is used to validate that any contained disposables have been disposed
        /// automatically via weaving.
        /// </summary>
        public CompositeDisposable DisposableTestContainer { get; set; }

        protected TestModelBase()
        {
            DisposableTestContainer = new CompositeDisposable();
        }

        public virtual void Dispose() {}
    }

    public abstract class BaseModel : TestModelBase
    {
        private string _string;
        public string StringProperty
        {
            get { return _string; }
            set { this.RaiseAndSetIfChanged(ref _string, value);}
        }

        // Throw in some random methods to use within any subscribes to ensure things don't break

        public int MultiplyByTwo(int number)
        {
            return number * 2;
        }
    }
}
