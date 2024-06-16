/********************************************************************************
* UseCases.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Json.Tests
{
    using Attributes;


    [TestFixture]
    public class UseCases
    {
        private sealed class Entity
        {
            [Alias(Name = "_id")]
            public required string Id { get; set; }
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

        [Test]
        public void LoadLargeTypedContent()
        {
            JsonParser parser = new(JsonParserFlags.CaseInsensitive | JsonParserFlags.ThrowOnUnknownProperty | JsonParserFlags.ThrowOnUnknownListItem);

            using StreamReader content = new
            (
                Path.Combine
                (
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                    "large1.json"
                )
            );

            List<Entity> result = (List<Entity>) parser.Parse(content, DeserializationContext.For(typeof(List<Entity>)), default)!;
            Assert.That(result.Count, Is.EqualTo(5000));
        }
    }
}
