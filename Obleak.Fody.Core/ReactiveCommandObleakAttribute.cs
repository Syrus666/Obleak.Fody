using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obleak.Fody.Core
{
    /// <summary>
    /// Used to call dispose on properties of type Reactive Command.
    /// Each property that is to be disposed will require this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ReactiveCommandObleakAttribute : Attribute
    {
    }
}
