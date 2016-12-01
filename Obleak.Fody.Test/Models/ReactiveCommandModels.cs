using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Obleak.Fody.Core;
using Obleak.Fody.Test.Helpers;
using ReactiveUI;

namespace Obleak.Fody.Test.Models
{
    public class SingleCommandObleakPropertyAttributeReactiveCommandModel : BaseModel
    {
        public SingleCommandObleakPropertyAttributeReactiveCommandModel()
        {
            Command = new ReactiveCommandDecorator<object>(ReactiveCommand.Create(Observable.Return(true)));
        }

        [ObleakReactiveCommand]
        public ReactiveCommandDecorator<object> Command { get; set; }
    }

    [ObleakReactiveCommand]
    public class TwoCommandsObleakClassAttributeReactiveCommandModel : BaseModel
    {
        public TwoCommandsObleakClassAttributeReactiveCommandModel()
        {
            Command1 = new ReactiveCommandDecorator<object>(ReactiveCommand.Create(Observable.Return(true)));
            Command2 = new ReactiveCommandDecorator<object>(ReactiveCommand.CreateAsyncTask((o, ct) => Task.FromResult(true)));
        }

        public ReactiveCommandDecorator<object> Command1 { get; set; }
        public ReactiveCommandDecorator<object> Command2 { get; set; }
    }
}
