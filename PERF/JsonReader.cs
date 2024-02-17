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
    public class JsonReaderTests
    {
        public abstract class ContentFactory
        {
            protected ContentFactory(string name) => Name = name;
            public string Name { get; } 
            public abstract TextReader CreateReader();
            public override string ToString() => Name;
        }

        public class StringContentFactory(string str) : ContentFactory(str)
        {
            public override TextReader CreateReader() => new StringReader(str);
        }

        public class FileContentFactory(string fileName) : ContentFactory(fileName)
        {
            public override TextReader CreateReader() => new StreamReader
            (
                Path.Combine
                (
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                    fileName
                )
            );
        }

        public static IEnumerable<ContentFactory> Params
        {
            get
            {
                //
                // String
                //

                yield return new StringContentFactory("\"cica\"");
                yield return new StringContentFactory("\"cica\\r\\n\"");
                yield return new StringContentFactory("\"Let's smile \\uD83D\\uDE01\"");

                //
                // Number
                //

                yield return new StringContentFactory("100");
                yield return new StringContentFactory("-100");
                yield return new StringContentFactory("100.0");
                yield return new StringContentFactory("-100.0");

                //
                // List
                //

                yield return new StringContentFactory("[]");
                yield return new StringContentFactory("[true]");
                yield return new StringContentFactory("[0, true, false, null, \"cica\"]");

                //
                // Object
                //

                yield return new StringContentFactory("{}");
                yield return new StringContentFactory("{\"cica\": 1986}");
                yield return new StringContentFactory("{\"a\": 0, \"b\": true, \"c\": false, \"d\": null, \"e\": \"cica\"}");

                //
                // Mixed, large
                //

                yield return new FileContentFactory("large1.json");
                yield return new FileContentFactory("large2.json");
            }
        }

        [ParamsSource(nameof(Params))]
        public ContentFactory Input { get; set; } = null!;

        public JsonReader Reader { get; set; } = null!;

        [GlobalSetup(Target = nameof(Parse))]
        public void SetupParse() => Reader = new(JsonReaderFlags.None, 256);

        [Benchmark]
        public void Parse()
        {
            using TextReader content = Input.CreateReader();

            _ = Reader.Parse(content, DeserializationContext.Untyped, CancellationToken.None);
        }
    }
}
