/********************************************************************************
* JsonReader.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    [MemoryDiagnoser]
    public class JsonReaderParsingTests
    {
        [
            Params
            (
                //
                // string
                //

                "\"cica\"",
                "\"cica\\r\\n\"",
                "\"Let's smile \\uD83D\\uDE01\"",
                "\"Loooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong\"",
                
                //
                // Number
                //

                "100",
                "-100",
                "100.0",
                "-100.0",

                //
                // List
                //

                "[]",
                "[true]",
                "[0, true, false, null, \"cica\"]"
            )
        ]
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
