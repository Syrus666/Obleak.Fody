using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Obleak.Fody.Test.Models;
using Xunit;

namespace Obleak.Fody.Test
{
    public class ReactiveCommandModelTests : TestBase
    {
        [Fact]
        public void SingleCommandObleakPropertyAttributeReactiveCommandModelIsDisposed()
        {
            // Arrange
            var model = new SingleCommandObleakPropertyAttributeReactiveCommandModel();

            // Act
            model.Dispose();

            // Assert
            Assert.True(model.Command.IsDisposed);
        }

        [Fact]
        public void TwoCommandObleakClassAttributeReactiveCommandTwoDisposed()
        {
            // Arrange
            var model = new TwoCommandsObleakClassAttributeReactiveCommandModel();

            // Act
            model.Dispose();

            // Assert
            Assert.True(model.Command1.IsDisposed);
            Assert.True(model.Command2.IsDisposed);
        }

    }
}
