using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Obleak.Fody.Core;
using ReactiveUI;

namespace Obleak.Fody.Test.Models
{
    public class SingleMethodSingleSubscribeModel : BaseModel
    {
        [Obleak]
        public void SingleMethod()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }
    }

    public class SingleMethodWithOneObserableParameterTwoSubscribeModel : BaseModel
    {
        [Obleak]
        public void SingleMethod(IObservable<string> observable)
        {
            DisposableTestContainer.Add(
                observable.Subscribe());

            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }
    }

    public class ThreeMethodsWithMultipleSubscribesModel : BaseModel
    {
        [Obleak]
        public void MethodOne()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }

        [Obleak]
        public int MethodTwo(IObservable<string> observable)
        {
            DisposableTestContainer.Add(
                observable.Subscribe());

            return 1;
        }

        [Obleak]
        public IDisposable MethodThree()
        {
            var disposable = this.WhenAnyValue(x => x.StringProperty).Subscribe();
            DisposableTestContainer.Add(disposable);

            DisposableTestContainer.Add(
                Observable.FromAsync(_ => Task.FromResult(true)).Subscribe());

            return disposable;
        }
    }

    public class TwoMethodsOnlyOneWithObleakAttributeModel : BaseModel
    {
        [Obleak]
        public IDisposable MethodWithObleak()
        {
            var disposable = this.WhenAnyValue(x => x.StringProperty).Subscribe();
            DisposableTestContainer.Add(disposable);
            return disposable;
        }

        // The returned disposable *should not* be disposed when the class is
        public IDisposable MethodWithoutObleak()
        {
            var disposable = this.WhenAnyValue(x => x.StringProperty).Subscribe();
            DisposableTestContainer.Add(disposable);
            return disposable;
        }
    }
}
