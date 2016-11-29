using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Obleak.Fody.Core;
using ReactiveUI;

namespace Obleak.Fody.Test.Models
{
    [SubscriptionObleak]
    public class ClassSingleConstructorTwoMethods : BaseModel
    {
        public ClassSingleConstructorTwoMethods()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }

        public ClassSingleConstructorTwoMethods(string s) { }

        public void MethodOne()
        {
            DisposableTestContainer.Add(
                Observable.FromAsync(_ => Task.FromResult(true)).Subscribe());
        }

        public void MethodTwo()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }
    }

    [SubscriptionObleak]
    public class ComplexClass : ClassSingleConstructorTwoMethods
    {
        private int _intValue = 10;
        public ComplexClass()
        {
            SerialDisposable = new SerialDisposable();
            MethodModel = new ThreeMethodsWithMultipleSubscribesModel();
            Subject = new Subject<string>();

            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }

        [SubscriptionObleak] // This attribute is irrelevant as it's on the class
        public ComplexClass(string s) : base(s)
        {
            SerialDisposable = new SerialDisposable();
            MethodModel = new ThreeMethodsWithMultipleSubscribesModel();
            Subject = new Subject<string>();

            // NO CALLS TO SUBSCRIBE HERE
        }

        public SerialDisposable SerialDisposable { get; set; }
        public ThreeMethodsWithMultipleSubscribesModel MethodModel { get; set; }
        public Subject<string> Subject { get; set; }
        public int IntValue => _intValue;

        public void ComplexMethodOne()
        {
            var disposable = MethodModel.MethodThree();
            SerialDisposable.Disposable = disposable;

            // First Disposable
            DisposableTestContainer.Add(
                Subject
                    .StartWith("TestString")
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        // Second disposable; note the StartWith meaning the subject doesn't have to fire for this to be hit
                        MethodOne();
                        _intValue = MultiplyByTwo(73);
                    }, 
                    exception => { }, 
                    () => _intValue = MultiplyByTwo(_intValue)));

            // Third disposable
            DisposableTestContainer.Add(disposable);
        }

        public void FireSubjectAndSubscribeToNewObservable()
        {
            Subject.OnNext("Yes");
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }

        public void SubscribeToNewObservableAndCallComplexMethodOne()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());

            // This will call subscrbe within MethodModel and again here on the subject.
            ComplexMethodOne();
        }

        public override void Dispose()
        {
            _intValue = 0;
            // NOTE: NO CALL TO base.Dispose() !!!
            // This means any disposables in the base class will not be disposed.
        }
    }
}
