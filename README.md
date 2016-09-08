# Obleak.Fody

This is a Fody weaver to help manage the disposables returned from calling IObservable.Subscribe()

The returned handles on your subscription are magically stored in a composite disposable which in turn is disposed of when the obj lifetime ends.

[![Build status](https://ci.appveyor.com/api/projects/status/xitay7hifqix06r0/branch/master?svg=true)](https://ci.appveyor.com/project/Syrus/obleak-fody/branch/master)

# Install

Install from NuGet with 

```
Install-Package Obleak.Fody
```

If this is your first Fody extension have a read of: https://github.com/Fody/Fody for some initial set up and other cool extensions you can use.

To get this up and running you'll need to add this weever to your ```FodyWeavers.xml``` config, so

```
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
    <Obleak />
</Weavers>
```

Then you can add the ```[Obleak]``` attribute to any class, constructor or method where you'd like to leave this manage the returned disposable from calling .Subscribe().

# Code!

What this does by weaving the IL is as follows, take this sample class...

```C#
    [Obleak]
    public class MyAmazingThingy : BaseThingy // BaseThingy implements IDisposable! (This is a must!)
    {
        public MyAmazingThingy()
        {
            this.WhenAnyValue(x => x.StringProperty).Subscribe();
        }
    }
```

When this gets compiled using this weaver the decompiled code does is something akin to the following

```C#
    [Obleak]
    public class MyAmazingThingy : BaseThingy // BaseThingy implements IDisposable! (This is a must!)
    {
        private CompositeDisposable $ObleakDisposable = new CompositeDisposable();

        public MyAmazingThingy()
        {
            var disposable = this.WhenAnyValue(x => x.StringProperty).Subscribe();
            this.$ObleakDisposable.Add(disposable);
        }

        public override void Dispose() 
        {
            base.Dispose();
            this.$ObleakDisposable.Dispose();
        }
    }
```

The above is slight over simplification so it's easy to see what's going on; what actually happens is that .Subscribe() is appended with a call to a new extension method on IDisposable with a signature of 

```public void IDisposable HandleWith(this IDisposable disposable, CompositeDisposable composite)``` 

which is fluent in that the disposable is again returned so it won't break anywhere that you'd already stored this in a variable or property.

If you place the ```[Obleak]``` attribute on classes **every constructor and method in the class will be processed**, but if you want finely grained control you can just pick out specific constructors or methods. If you do both within the same class, (so put the attribute on the class and a method, the class level supersedes the method one and everything will get weaved).








