using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obleak.Fody.Core
{
    /// <summary>
    /// Track disposables returned from calls to .Subscribe() within attribute scope.
    /// Returned disposables are placed in a composite disposable, which is disposed of when the object is.
    /// 
    /// When placed on a class this will apply to every constructor and method within, else it will only 
    /// manage the disposables of the explicitly decorated constructors / methods.
    /// </summary>
    /// <remarks>Only works on classes which implemented IDisposable</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method)]
    public class ObleakSubscriptionAttribute : Attribute
    {
    }
}
