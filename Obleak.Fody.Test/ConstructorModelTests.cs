using System;
using System.Linq;
using System.Reactive.Disposables;
using Obleak.Fody.Test.Models;
using Xunit;

namespace Obleak.Fody.Test
{
    public class ConstructorModelTests : TestBase
    {
        [Fact]
        public void SingleAttributedConstructorAllObservablesDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 1;
            var model = new SingleConstructorSingleSubscribeModel();

            var container = (CompositeDisposable) model.DisposableTestContainer;

            // Assert
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        // Tests that the two attributed constructors aren't polluting each other post weave 
        [Fact]
        public void TwoAttributedConstructorsWithSingleObservableEachAllObservablesDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 1;
            var model1 = new TwoConstructorsSingleSubscribeModel();
            var model2 = new TwoConstructorsSingleSubscribeModel(true);

            var container1 = (CompositeDisposable)model1.DisposableTestContainer;
            var container2 = (CompositeDisposable)model2.DisposableTestContainer;

            // Assert
            Assert.True(IsExpectedDisposableTestContainerInitialState(container1, expectedNumberOfDisposables));
            Assert.True(IsExpectedDisposableTestContainerInitialState(container2, expectedNumberOfDisposables));

            // Act
            model1.Dispose();
            model2.Dispose();

            // Assert
            Assert.True(container1.Count == expectedNumberOfDisposables);
            Assert.True(container1.OfType<CompositeDisposable>().All(d => d.IsDisposed));
            Assert.True(container2.Count == expectedNumberOfDisposables);
            Assert.True(container2.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        // Tests that this works with multiple subscriptions in one constructor
        [Fact]
        public void SingleAttributeConstructorWithThreeObservablesDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 3;
            var model = new SingleConstructorThreeSubscribesModel();

            var container = (CompositeDisposable)model.DisposableTestContainer;

            // Assert
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }

        // Tests that this works with inheritance
        [Fact]
        public void SingleComplexConstructorIheritedFromSingleConstructorThreeSubscribeFourDisposedTest()
        {
            // Arrange
            const int expectedNumberOfDisposables = 4;
            var model = new SingleComplexConstructorIheritedFromSingleConstructorThreeSubscribeModel();

            var container = (CompositeDisposable)model.DisposableTestContainer;

            // Assert
            Assert.True(IsExpectedDisposableTestContainerInitialState(container, expectedNumberOfDisposables));

            // Act
            model.Dispose();

            // Assert
            Assert.True(container.Count == expectedNumberOfDisposables);
            Assert.True(container.OfType<CompositeDisposable>().All(d => d.IsDisposed));
        }
    }
}
