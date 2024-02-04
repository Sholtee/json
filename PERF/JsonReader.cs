/********************************************************************************
* JsonReader.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    [MemoryDiagnoser]
    public class JsonReaderParsingTests
    {
        public static IEnumerable<TextReader> Params
        {
            get
            {
                //
                // String
                //

                yield return new StringReader("\"cica\"");
                yield return new StringReader("\"cica\\r\\n\"");
                yield return new StringReader("\"Let's smile \\uD83D\\uDE01\"");

                //
                // Number
                //

                yield return new StringReader("100");
                yield return new StringReader("-100");
                yield return new StringReader("100.0");
                yield return new StringReader("-100.0");

                //
                // List
                //

                yield return new StringReader("[]");
                yield return new StringReader("[true]");
                yield return new StringReader("[0, true, false, null, \"cica\"]");

                //
                // Object
                //

                yield return new StringReader("{}");
                yield return new StringReader("{\"cica\": 1986}");
                yield return new StringReader("{\"a\": 0, \"b\": true, \"c\": false, \"d\": null, \"e\": \"cica\"}");

                //
                // Mixed, large
                //

                yield return new StreamReader
                (
                    Path.Combine
                    (
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                        "large.json"
                    )
                );
            }
        }

        [ParamsSource(nameof(Params))]
        public TextReader Input { get; set; } = null!;

        [Benchmark]
        public void Parse()
        {
            using JsonReader rdr = new(Input, UntypedDeserializationContext.Instance, JsonReaderFlags.None, 256);
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
