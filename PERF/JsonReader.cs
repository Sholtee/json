/********************************************************************************
* JsonReader.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
        public static IEnumerable<Func<TextReader>> Params
        {
            get
            {
                //
                // String
                //

                yield return static () => new StringReader("\"cica\"");
                yield return static () => new StringReader("\"cica\\r\\n\"");
                yield return static () => new StringReader("\"Let's smile \\uD83D\\uDE01\"");

                //
                // Number
                //

                yield return static () => new StringReader("100");
                yield return static () => new StringReader("-100");
                yield return static () => new StringReader("100.0");
                yield return static () => new StringReader("-100.0");

                //
                // List
                //

                yield return static () => new StringReader("[]");
                yield return static () => new StringReader("[true]");
                yield return static () => new StringReader("[0, true, false, null, \"cica\"]");

                //
                // Object
                //

                yield return static () => new StringReader("{}");
                yield return static () => new StringReader("{\"cica\": 1986}");
                yield return static () => new StringReader("{\"a\": 0, \"b\": true, \"c\": false, \"d\": null, \"e\": \"cica\"}");

                //
                // Mixed, large
                //

                string location = Path.Combine
                (
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                    "large.json"
                );

                yield return () => new StreamReader(location);
            }
        }

        [ParamsSource(nameof(Params))]
        public Func<TextReader> Input { get; set; } = null!;

        public JsonReader Reader { get; set; } = null!;

        [GlobalSetup(Target = nameof(Parse))]
        public void SetupParse() => Reader = new(DeserializationContext.Untyped, JsonReaderFlags.None, 256);

        [Benchmark]
        public void Parse()
        {
            using TextReader content = Input();

            _ = Reader.Parse(content, CancellationToken.None);
        }
    }
}
