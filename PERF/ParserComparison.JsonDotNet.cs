/********************************************************************************
* ParserComparison.JsonDotNet.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;

using Newtonsoft.Json;

namespace Solti.Utils.Json.Perf
{
    public partial class ParserComparison
    {
        private sealed class JsonDotNet<T> : IParser<T>
        {
            private JsonSerializer FSerializer = null!;

            public void Init() => FSerializer = new JsonSerializer();

            public T Parse(TextReader json) => (T) FSerializer.Deserialize(json, typeof(T))!;

            public override string ToString() => typeof(JsonSerializer).Namespace!;
        }
    }
}
