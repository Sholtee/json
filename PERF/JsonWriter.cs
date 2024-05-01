/********************************************************************************
* JsonWriter.cs                                                                 *
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
    public class JsonWriterTests
    {
        public static IEnumerable<object> Params
        {
            get
            {
                //
                // String
                //

                yield return "cica";
                yield return "cica\r\n";
                yield return "cica😁";

                //
                // Number
                //

                yield return 100;
                yield return -100;
                yield return 100.0;
                yield return -100.0;

                //
                // List
                //

                yield return new object[0];
                yield return new object[] { true };
                yield return new object?[] { 0, false, true, null, "cica"};

                //
                // Object
                //

                yield return new Dictionary<string, object>();
                yield return new Dictionary<string, object> { {"cica", 1986} };
                yield return new Dictionary<string, object?> { { "a", 0 }, { "b", true }, { "c", false }, { "d", null }, { "e", "cica" }, };

                //
                // Mixed, large
                //

                foreach (string file in new string[] { "large1.json", "large2.json" })
                {
                    yield return new JsonParser().Parse
                    (
                        new StreamReader
                        (
                            Path.Combine
                            (
                                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                                file
                            )
                        ),
                        DeserializationContext.For(typeof(object)),
                        default
                    )!;
                }
            }
        }

        public JsonWriter Writer { get; set; } = null!;

        [ParamsSource(nameof(Params))]
        public object? Param { get; set; }

        [GlobalSetup(Target = nameof(Write))]
        public void SetupWrite() => Writer = new();

        [Benchmark]
        public void Write() => Writer.Write
        (
            new StringWriter(),
            closeDest: true,
            Param,
            SerializationContext.For(typeof(object)),
            CancellationToken.None
        );
    }
}
