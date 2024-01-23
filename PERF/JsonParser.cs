/********************************************************************************
* JsonParser.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    internal sealed class DummyContext : IJsonReaderContext
    {
        public void CommentParsed(ReadOnlySpan<char> value) => throw new NotImplementedException();
        public object CreateRawObject(ObjectKind objectKind) => new List<object?>();
        public void PopState() => throw new NotImplementedException();
        public bool PushState(ReadOnlySpan<char> property, StringComparison comparison) => throw new NotImplementedException();
        public void SetValue(object obj, object? value) => ((List<object?>) obj).Add(value);
        public void SetValue(object obj, ReadOnlySpan<char> value) => ((List<object?>) obj).Add(new string(value));
    }

    [MemoryDiagnoser]
    public class JsonParser
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

        public JsonReader Reader { get; set; } = null!;

        public ITextReader Content { get; set; } = null!;

        [GlobalSetup(Target = nameof(Parse))]
        public void SetupParse()
        {
            Content = new StringReader(Input);
            Reader = new JsonReader(Content, new DummyContext(), JsonReaderFlags.None, 256);
        }

        [Benchmark]
        public void Parse()
        {
            Content.Reset();
            _ = Reader.Parse(CancellationToken.None);
        }
    }
}
