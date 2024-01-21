/********************************************************************************
* JsonReaderContextBase.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Json
{
    public abstract class JsonReaderContextBase() : IJsonReaderContext
    {
        public abstract object CreateRawObject();
        public abstract void PopState();
        public abstract bool PushState(ReadOnlySpan<char> property, StringComparison comparison);
        public abstract void SetValue(object obj, object? value);
        public abstract void SetValue(object obj, ReadOnlySpan<char> value);
        
    }
}
