/********************************************************************************
* ParserComparison.My.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;
using System.Threading;

namespace Solti.Utils.Json.Perf
{
    public partial class ParserComparison
    {
        private sealed class MyParser<T> : IParser<T>
        {
            private JsonParser FParser = null!;
            private DeserializationContext FContext = null!;

            public void Init()
            {
                FParser = new(JsonParserFlags.CaseInsensitive | JsonParserFlags.ThrowOnUnknownProperty | JsonParserFlags.ThrowOnUnknownListItem, 256);
                FContext = DeserializationContext.For(typeof(T));
            }

            public T Parse(string json)
            {
                using StringReader content = new(json);

                return (T) FParser.Parse(content, FContext, CancellationToken.None)!;
            }

            public override string ToString() => typeof(JsonParser).Namespace!;
        }
    }
}
