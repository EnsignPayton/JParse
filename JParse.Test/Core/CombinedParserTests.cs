using System.Collections.Generic;
using System.Dynamic;
using JParse.Core;
using Xunit;

namespace JParse.Test.Core
{
    public class CombinedParserTests
    {
        #region Basic Types

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("null", null)]
        public void ParsesBasicTypes(string json, object expected) => Parses(json, expected);

        [Theory]
        [InlineData("0", 0.0)]
        [InlineData("10", 10.0)]
        [InlineData("123456789", 123456789.0)]
        public void ParsesPositiveIntegers(string json, double expected) => Parses(json, expected);

        [Theory]
        [InlineData("-0", 0.0)]
        [InlineData("-1", -1.0)]
        [InlineData("-10", -10.0)]
        [InlineData("-999999", -999999.0)]
        public void ParsesNegativeIntegers(string json, double expected) => Parses(json, expected);

        [Theory]
        [InlineData("0.0", 0.0)]
        [InlineData("1.0", 1.0)]
        [InlineData("-1.0", -1.0)]
        [InlineData("3.14159", 3.14159)]
        [InlineData("12345678.9", 12345678.9)]
        public void ParsesReals(string json, double expected) => Parses(json, expected);

        [Theory]
        [InlineData("1e1", 10.0)]
        [InlineData("1e+1", 10.0)]
        [InlineData("1.0e1", 10.0)]
        [InlineData("1.0e+1", 10.0)]
        [InlineData("1.0e2", 100.0)]
        [InlineData("2.0e1", 20.0)]
        [InlineData("1e-1", 0.1)]
        [InlineData("1.0e-1", 0.1)]
        [InlineData("1e-2", 0.01)]
        [InlineData("1.0e-2", 0.01)]
        public void ParsesExponents(string json, double expected) => Parses(json, expected);

        [Theory]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        [InlineData("\"Hello, world!\"", "Hello, world!")]
        public void ParsesBasicStrings(string json, string expected) => Parses(json, expected);

        [Theory]
        [InlineData("\"\\\"\"", "\"")]
        [InlineData("\"\\\\\"", "\\")]
        [InlineData("\"\\/\"", "/")]
        [InlineData("\"\\b\"", "\b")]
        [InlineData("\"\\n\"", "\n")]
        [InlineData("\"\\r\"", "\r")]
        [InlineData("\"\\t\"", "\t")]
        [InlineData("\"\\u1234\"", "\u1234")]
        [InlineData("\"\\uabcd\"", "\uabcd")]
        public void ParsesEscapeStrings(string json, string expected) => Parses(json, expected);

        [Theory]
        [InlineData("[]", new object[] { })]
        [InlineData("[ ]", new object[] { })]
        [InlineData("[ true ]", new object[] { true })]
        [InlineData("[ false ]", new object[] { false })]
        [InlineData("[ null ]", new object[] { null })]
        [InlineData("[ true, true ]", new object[] { true, true })]
        [InlineData("[ true, false, null ]", new object[] { true, false, null })]
        public void ParsesBasicTypeArrays(string json, object[] expected) => Parses(json, expected);

        [Theory]
        [InlineData("[ 1, 2, 3 ]", new object[] { 1.0, 2.0, 3.0 })]
        [InlineData("[ \"hello\", \"world\" ]", new object[] { "hello", "world" })]
        public void ParsesSimpleTypeArrays(string json, object[] expected) => Parses(json, expected);

        [Theory]
        [InlineData("[ \"foo\", 1.2 ]", new object[] { "foo", 1.2 })]
        [InlineData("[ \"bar\", false ]", new object[] { "bar", false })]
        [InlineData("[ 0.5, null ]", new object[] { 0.5, null })]
        public void ParsesMixedTypeArrays(string json, object[] expected) => Parses(json, expected);

        #endregion

        #region Objects

        [Fact]
        public void ParsesEmptyObject()
        {
            const string json = "{}";
            var dict = new Dictionary<string, object>();
            ParsesObject(json, dict);
        }

        [Fact]
        public void ParsesSinglePropObject()
        {
            const string json = "{\"foo\": true}";
            var dict = new Dictionary<string, object>
            {
                { "foo", true }
            };
            ParsesObject(json, dict);
        }

        [Fact]
        public void ParsesMultiPropObject()
        {
            const string json = "{\"foo\": 123, \"bar\": null }";
            var dict = new Dictionary<string, object>
            {
                { "foo", 123.0 },
                { "bar", null }
            };
            ParsesObject(json, dict);
        }

        [Fact]
        public void ParsesNestedObject()
        {
            const string json = "{ \"obj\": {} }";
            var dict = new Dictionary<string, object>
            {
                { "obj", new Dictionary<string, object>() }
            };
            ParsesObject(json, dict);
        }

        [Fact]
        public void ParsesEmptyArrayObject()
        {
            const string json = "{\"arr\": []}";
            var dict = new Dictionary<string, object>
            {
                { "arr", new object[]{} }
            };
            ParsesObject(json, dict);
        }

        [Fact]
        public void ParsesRealWorldObject()
        {
            const string json = @"
{
    ""Dog"": {
        ""Name"": ""Javvy"",
        ""Age"": 4,
        ""IsGoodBoy"": true
    },
    ""Names"": [
        ""Alice"",
        ""Bob"",
        ""Catherine"",
        ""David""
    ]
}
";
            var dict = new Dictionary<string, object>
            {
                {
                    "Dog", new Dictionary<string, object>
                    {
                        { "Name", "Javvy" },
                        { "Age", 4.0 },
                        { "IsGoodBoy", true }
                    }
                },
                {
                    "Names", new object[]
                    {
                        "Alice",
                        "Bob",
                        "Catherine",
                        "David"
                    }
                }
            };
            ParsesObject(json, dict);
        }

        #endregion

        #region Arrays

        [Fact]
        public void ParsesSingleEmptyObjectArray()
        {
            const string json = "[{}]";
            var arr = new object[]
            {
                new Dictionary<string, object>()
            };
            ParsesArray(json, arr);
        }

        [Fact]
        public void ParsesMultiEmptyObjectArray()
        {
            const string json = "[{},{}]";
            var arr = new object[]
            {
                new Dictionary<string, object>(),
                new Dictionary<string, object>()
            };
            ParsesArray(json, arr);
        }

        [Fact]
        public void ParsesObjectWithMembersArray()
        {
            const string json = "[{\"Foo\": \"Bar\"}]";
            var arr = new object[]
            {
                new Dictionary<string, object>
                {
                    { "Foo", "Bar" }
                }
            };
            ParsesArray(json, arr);
        }

        [Fact]
        public void ParsesHeavyNestedObjectArray()
        {
            const string json = "[ { \"A\": [ { \"B\": [ { } ] } ] } ]";
            var arr = new object[]
            {
                new Dictionary<string, object>
                {
                    {
                        "A", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                {
                                    "B", new object[]
                                    {
                                        new Dictionary<string, object>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            ParsesArray(json, arr);
        }

        #endregion

        #region Helpers

        private static void ParsesObject(string json, IDictionary<string, object> expectedProps)
        {
            var actual = CombinedParser.Parse(json);
            Assert.IsType<ExpandoObject>(actual);

            var actualProps = (IDictionary<string, object>) actual;
            AssertDictionariesMatch(expectedProps, actualProps);
        }

        private static void ParsesArray(string json, object[] expectedArray)
        {
            var actual = CombinedParser.Parse(json);
            Assert.IsType<object[]>(actual);

            var actualArray = (object[]) actual;
            AssertArraysMatch(expectedArray, actualArray);
        }

        private static void AssertDictionariesMatch(
            IDictionary<string, object> expectedProps,
            IDictionary<string, object> actualProps)
        {
            foreach (var expected in expectedProps)
            {
                if (expected.Value is IDictionary<string, object> expectedObject)
                {
                    // Object Member
                    Assert.Contains(expected.Key, actualProps.Keys);
                    Assert.IsType<ExpandoObject>(actualProps[expected.Key]);

                    var actualObject = (IDictionary<string, object>) actualProps[expected.Key];
                    AssertDictionariesMatch(expectedObject, actualObject);
                }
                else if (expected.Value is object[] expectedArray)
                {
                    // Array Member
                    Assert.Contains(expected.Key, actualProps.Keys);

                    var actualArray = (object[]) actualProps[expected.Key];
                    AssertArraysMatch(expectedArray, actualArray);
                }
                else
                {
                    // Simple Type Member
                    Assert.Contains(expected, actualProps);
                }
            }
        }

        private static void AssertArraysMatch(
            object[] expectedArray,
            object[] actualArray)
        {
            Assert.Equal(expectedArray.Length, actualArray.Length);

            for (int i = 0; i < expectedArray.Length; i++)
            {
                if (expectedArray[i] is IDictionary<string, object> expectedObject)
                {
                    Assert.IsType<ExpandoObject>(actualArray[i]);

                    var actualObject = (IDictionary<string, object>) actualArray[i];
                    AssertDictionariesMatch(expectedObject, actualObject);
                }
                else if (expectedArray[i] is object[] expectedInnerArray)
                {
                    var actualInnerArray = (object[]) actualArray[i];
                    AssertArraysMatch(expectedInnerArray, actualInnerArray);
                }
                else
                {
                    Assert.Equal(expectedArray[i], actualArray[i]);
                }
            }
        }

        private static void Parses(string json, object expected)
        {
            var actual = CombinedParser.Parse(json);
            Assert.Equal(expected, actual);
        }

        #endregion
    }
}
