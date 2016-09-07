using System;
using System.Linq;
using System.Reactive.Disposables;
using Obleak.Fody.Test.Models;
using Xunit;

namespace Obleak.Fody.Test
{
    public class ClassModelTests : TestBase
    {
        [Fact]
        public void ClassWideAttributeAppliedToConstructorAndAllMethodsThreeDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 3;
            var model = new ClassSingleConstructorTwoMethods();
            var container = (CompositeDisposable)model.DisposableTestContainer;

            // Should now be one obserable being tracked from the constructor
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, 1)); // Validate this worked

            model.MethodOne();
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, 2));

            model.MethodTwo();
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        [Fact]
        public void ComplexClassComplexClassComplexMethodOneTwoDisposablesTrackedBothDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 3;
            var model = new ComplexClass("This constructor doesn't have any calls to subscribe in inheritance hierarchy");
            var container = (CompositeDisposable)model.DisposableTestContainer;

            Assert.True(container.Count == 0); // Should be no obserables

            model.ComplexMethodOne(); // See method for the disposables (expect 3 per call)
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        [Fact]
        public void ComplexClassCallingFireSubjectAndSubscribeToNewObservableTracksTwoNewObservablesWhichAreDisposed()
        {
            const int expectedNumberOfDisposables = 5;
            var model = new ComplexClass("This constructor doesn't have any calls to subscribe in inheritance hierarchy");
            var container = (CompositeDisposable)model.DisposableTestContainer;

            Assert.True(container.Count == 0); // Should be no disposables

            model.ComplexMethodOne(); // See method for the disposables (expect 3 per call)
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, 3));

            model.FireSubjectAndSubscribeToNewObservable();
            // The above will have triggered the Subject.Subscribe() set up on ComplexMethodOne 
            // and therefore called MethodOne() adding a new disposable, and done another WhenAnyValue
            // Meaning we should now be at 3 + 2 = *5* disposables being tracked
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        [Fact]
        public void ComplexClassCallingConstructorComplexMethodOneSubscribeToNewObservableAndCallComplexMethodOneDisposesEightTest()
        {
            const int expectedNumberOfDisposables = 9;
            const int expectedNumberDisposed = 8;
            const int expectedNumberNotDisposed = 1;
            var model = new ComplexClass(); // One disposable in this, one in the base constructor = 2 disposables
            var container = (CompositeDisposable)model.DisposableTestContainer;
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, 2));

            model.ComplexMethodOne(); // See method for the disposables (expect 3 per call)
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, 5)); // 3 + Constructor 2 

            model.SubscribeToNewObservableAndCallComplexMethodOne(); // 5 + 1 (from Subscribe) + 3 (from Complex Call)
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose(); // See

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);

            // As the ComplexClass.Dispose() method does not call base.Dispose() the disposable created within the
            // base ctor will not be cleaned up.
            // This shows that we're not forcing a call to base.Dispose() when generating the IL if you haven't specified one
            // when overriding the Dispose method yourself (we do this correctly when we auto gen this for you)
            Assert.True(container.OfType<CompositeDisposable>().Count(x => x.IsDisposed) == expectedNumberDisposed);
            Assert.True(container.OfType<CompositeDisposable>().Count(x => !x.IsDisposed) == expectedNumberNotDisposed); // So one will still leak

            // Check that the IntValue is still reset to 0 in the DisposeMethod
            Assert.Equal(0, model.IntValue);
        }
    }
}
