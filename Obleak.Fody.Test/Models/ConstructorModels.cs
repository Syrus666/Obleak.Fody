﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Obleak.Fody.Core;
using ReactiveUI;

namespace Obleak.Fody.Test.Models
{
    public class SingleConstructorSingleSubscribeModel : BaseModel
    {
        [SubscriptionObleak]
        public SingleConstructorSingleSubscribeModel()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }
    }

    public class TwoConstructorsSingleSubscribeModel : BaseModel
    {
        [SubscriptionObleak]
        public TwoConstructorsSingleSubscribeModel()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }

        [SubscriptionObleak]
        public TwoConstructorsSingleSubscribeModel(bool b)
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }
    }

    public class SingleConstructorThreeSubscribesModel : BaseModel
    {
        [SubscriptionObleak]
        public SingleConstructorThreeSubscribesModel()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());

            DisposableTestContainer.Add(
                Observable.FromAsync(_ => Task.FromResult(true)).Subscribe());

            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }
    }

    public class SingleComplexConstructorIheritedFromSingleConstructorThreeSubscribeModel : SingleConstructorThreeSubscribesModel
    {
        [SubscriptionObleak]
        public SingleComplexConstructorIheritedFromSingleConstructorThreeSubscribeModel()
        {
            var i = 5;
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty, x => x == "Test").Subscribe(_ => i = MultiplyByTwo(i)));
            StringProperty = "Test";
        }
    }

    public class TwoConstructorsOnlyOneWithObleakAttributeModel : BaseModel
    {
        [SubscriptionObleak]
        public TwoConstructorsOnlyOneWithObleakAttributeModel()
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }

        public TwoConstructorsOnlyOneWithObleakAttributeModel(bool a)
        {
            DisposableTestContainer.Add(
                this.WhenAnyValue(x => x.StringProperty).Subscribe());
        }
    }
}
