/********************************************************************************
* ParserComparison.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    [MemoryDiagnoser]
    public partial class ParserComparison
    {
        public interface IParser<T>
        {
            void Init();

            T Parse(TextReader json);

            string ToString();
        }

        public sealed class Entity
        {
            public required string _Id { get; set; }
            public int Index { get; set; }
            public Guid Guid { get; set; }
            public bool IsActive { get; set; }
            public string? Balance { get; set; }
            public string? Picture { get; set; }
            public int? Age { get; set; }
            public KnownColor? EyeColor { get; set; }
            public required string Name { get; set; }
            public Genders? Gender { get; set; }
            public string? Company { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? About { get; set; }
            public DateTime? Registered { get; set; }
            public Double? Latitude { get; set; }
            public Double? Longitude { get; set; }
            public List<string>? Tags { get; set; }
            public string? Greeting { get; set; }
            public string? FavoriteFruit { get; set; }
            public List<SubEntity>? Friends { get; set; }

            public sealed class SubEntity
            {
                public required int Id { get; set; }
                public required string Name { get; set; }
            }

            public enum Genders
            {
                Male,
                Female
            }
        }

        private static string Json { get; } = File.ReadAllText("large1.json");

        public static IEnumerable<IParser<List<Entity>>> Parsers
        {
            get
            {
                yield return new JsonDotNet<List<Entity>>();
                yield return new MyParser<List<Entity>>();
            }
        }

        [ParamsSource(nameof(Parsers))]
        public IParser<List<Entity>> Parser { get; set; } = null!;

        [GlobalSetup(Target = nameof(Parse))]
        public void Setup() => Parser.Init();

        [Benchmark]
        public List<Entity> Parse()
        {
            using StringReader json = new(Json);
            return Parser.Parse(json);
        }
    }
}
