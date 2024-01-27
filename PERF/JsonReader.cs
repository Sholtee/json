/********************************************************************************
* JsonReader.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.IO;
using System.Threading;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    [MemoryDiagnoser]
    public class JsonReaderParsingTests
    {
        public static IEnumerable<string> Params
        {
            get
            {
                //
                // String
                //

                yield return "\"cica\"";
                yield return "\"cica\\r\\n\"";
                yield return "\"Let's smile \\uD83D\\uDE01\"";

                //
                // Number
                //

                yield return "100";
                yield return "-100";
                yield return "100.0";
                yield return "-100.0";

                //
                // List
                //

                yield return "[]";
                yield return "[true]";
                yield return "[0, true, false, null, \"cica\"]";

                //
                // Object
                //

                yield return "{}";
                yield return "{\"cica\": 1986}";
                yield return "{\"a\": 0, \"b\": true, \"c\": false, \"d\": null, \"e\": \"cica\"}";

                //
                // Mixed, large
                //

                yield return File.ReadAllText("large.json");
            }
        }

        [ParamsSource(nameof(Params))]
        public string Input { get; set; } = null!;

        [Benchmark]
        public void Parse()
        {
            using JsonReader rdr = new(new StringReader(Input), UntypedDeserializationContext.Instance, JsonReaderFlags.None, 256);
            _ = rdr.Parse(CancellationToken.None);
        }
    }

    [MemoryDiagnoser]
    public class JsonReaderInstantiationTests
    {
        [Benchmark]
        public void CreateAndDestroyReader()
        {
            using JsonReader rdr = new(new StringReader(""), UntypedDeserializationContext.Instance, JsonReaderFlags.None, 256);
        }
    }
}
