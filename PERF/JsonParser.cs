/********************************************************************************
* JsonParser.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    internal sealed class DummyContext : IJsonReaderContext
    {
        public void CommentParsed(ReadOnlySpan<char> value) => throw new NotImplementedException();
        public object CreateRawObject(ObjectKind objectKind) => throw new NotImplementedException();
        public void PopState() => throw new NotImplementedException();
        public bool PushState(ReadOnlySpan<char> property, StringComparison comparison) => throw new NotImplementedException();
        public void SetValue(object obj, object? value) => throw new NotImplementedException();
        public void SetValue(object obj, ReadOnlySpan<char> value) => throw new NotImplementedException();
    }

    [MemoryDiagnoser]
    public class StringParsing
    {
        [
            Params
            (
                "\"cica\"",
                "\"cica\\r\\n\"",
                "\"Let's smile \\uD83D\\uDE01\"",
                "\"Loooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong\""
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
            _ = Reader.ParseString();
        }
    }
}
