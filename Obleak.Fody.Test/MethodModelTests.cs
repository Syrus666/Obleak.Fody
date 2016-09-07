using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Obleak.Fody.Test.Models;
using Xunit;

namespace Obleak.Fody.Test
{
    public class MethodModelTests : TestBase
    {
        [Fact]
        public void SingleMethodOneSubscribeOneDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 1;
            var model = new SingleMethodSingleSubscribeModel();
            model.SingleMethod(); // Contains the call to .Subscribe()
            var container = (CompositeDisposable)model.DisposableTestContainer;

            // Assert
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        [Fact]
        public void SingleMethodWithObsParamTwoSubscribeTwoDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 2;
            var model = new SingleMethodWithOneObserableParameterTwoSubscribeModel();
            model.SingleMethod(new Subject<string>()); // Contains the two calls to .Subscribe() 
            var container = (CompositeDisposable)model.DisposableTestContainer;

            // Assert
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        [Fact]
        public void ThreeMethodsFourSubscribesFourDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 4;
            var model = new ThreeMethodsWithMultipleSubscribesModel();
            var container = (CompositeDisposable)model.DisposableTestContainer;

            model.MethodOne(); // Set up the first subscribe
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, 1)); // Validate this worked

            model.MethodTwo(new Subject<string>()); // Set up the second subscribe
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, 2)); // Validate this worked

            var disposable = model.MethodThree(); // Contains two further calls to subscribe with use of local variables, and a return
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));
            Assert.Contains(disposable, container); // Check this returned dispoable is already being tracked

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        [Fact]
        public void TwoMethodsOneObleakMethodTwoCallsToSubscribeOneDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposablesInTestContainer = 2;
            const int expectedNumberDisposed = 1;
            const int expectedNumberNotDisposed = 1;
            var model = new TwoMethodsOnlyOneWithObleakAttributeModel();
            var container = (CompositeDisposable)model.DisposableTestContainer;

            var obleakDisposable = (CompositeDisposable) model.MethodWithObleak(); // This one has the attribute and should be disposed
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, 1)); // Validate this worked

            var nonObleakDisposable = (CompositeDisposable)model.MethodWithoutObleak(); // No obleak attribute, therefore this should not get disposed
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposablesInTestContainer));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposablesInTestContainer);
            Assert.True(container.OfType<CompositeDisposable>().Count(x => x.IsDisposed) == expectedNumberDisposed);
            Assert.True(container.OfType<CompositeDisposable>().Count(x => !x.IsDisposed) == expectedNumberNotDisposed);
            Assert.True(obleakDisposable.IsDisposed);
            Assert.False(nonObleakDisposable.IsDisposed);

        }
    }
}
