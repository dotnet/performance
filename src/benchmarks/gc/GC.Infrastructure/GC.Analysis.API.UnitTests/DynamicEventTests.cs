using FluentAssertions;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.GCDynamic;
using GC.Analysis.API.DynamicEvents;
using System.Reflection;

namespace GC.Analysis.API.UnitTests
{
    [TestClass]
    public class DynamicEventTests
    {
        [TestMethod]
        public void TestDuplicatedSchema()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                                new KeyValuePair<string, Type>("Number", typeof(ulong)),
                            }
                        },
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                                new KeyValuePair<string, Type>("Number", typeof(ulong)),
                            }
                        }
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestDuplicatedFields()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                                new KeyValuePair<string, Type>("version", typeof(ulong)),
                            }
                        },
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestUnsupportedType()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(DateTime)),
                            }
                        },
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestNegativeMinOccurrence()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            MinOccurrence = -1,
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                            }
                        },
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestSmallerMaxOccurrence()
        {
            Action test = () =>
            {
                DynamicEventSchema.Set(
                    new List<DynamicEventSchema>
                    {
                        new DynamicEventSchema
                        {
                            DynamicEventName = "SampleEventName",
                            MinOccurrence = 1,
                            MaxOccurrence = 0,
                            Fields = new List<KeyValuePair<string, Type>>
                            {
                                new KeyValuePair<string, Type>("version", typeof(ushort)),
                            }
                        },
                    }
                );
            };
            test.Should().Throw<Exception>();
        }

        private List<DynamicEventSchema> correctSingleSchema = new List<DynamicEventSchema>
        {
            new DynamicEventSchema
            {
                DynamicEventName = "SampleEventName",
                MinOccurrence = 1,
                Fields = new List<KeyValuePair<string, Type>>
                {
                    new KeyValuePair<string, Type>("version", typeof(ushort)),
                    new KeyValuePair<string, Type>("Number", typeof(ulong)),
                }
            },
        };

        private GCDynamicEvent sampleEvent = new GCDynamicEvent(
            "SampleEventName",
            DateTime.Now,
            new byte[] { 1, 0, 2, 0, 0, 0, 0, 0, 0, 0 }
        );

        private GCDynamicEvent unknownEvent = new GCDynamicEvent(
            "UnknownEventName",
            DateTime.Now,
            new byte[] { 1, 0, 2, 0, 0, 0, 0, 0, 0, 0 }
        );

        [TestMethod]
        public void TestMissedSingleEvent()
        {
            DynamicEventSchema.Set(correctSingleSchema);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>();
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestDuplicatedSingleEvent()
        {
            DynamicEventSchema.Set(correctSingleSchema);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>
            {
                sampleEvent,
                sampleEvent
            };
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestSingleEvent()
        {
            DynamicEventSchema.Set(correctSingleSchema);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>
            {
                sampleEvent
            };
            dynamic index = new DynamicIndex(dynamicEvents);

            ((int)index.SampleEventName.version).Should().Be(1);
            ((int)index.SampleEventName.Number).Should().Be(2);
            string pattern = @"
SampleEventName
version   : 1
Number    : 2
TimeStamp : *
".Trim();
            ((string)index.SampleEventName.ToString()).Should().Match(pattern);
        }

        private List<DynamicEventSchema> correctMultipleSchema = new List<DynamicEventSchema>
        {
            new DynamicEventSchema
            {
                DynamicEventName = "SampleEventName",
                MinOccurrence = 1,
                MaxOccurrence = 2,
                Fields = new List<KeyValuePair<string, Type>>
                {
                    new KeyValuePair<string, Type>("version", typeof(ushort)),
                    new KeyValuePair<string, Type>("Number", typeof(ulong)),
                }
            },
        };

        [TestMethod]
        public void TestMissedMultipleEvent()
        {
            DynamicEventSchema.Set(correctMultipleSchema);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>();
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestTooManyMultipleEvents()
        {
            DynamicEventSchema.Set(correctMultipleSchema);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>
            {
                sampleEvent,
                sampleEvent,
                sampleEvent,
            };
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestMultipleEvents()
        {
            DynamicEventSchema.Set(correctMultipleSchema);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>
            {
                sampleEvent,
                sampleEvent,
            };
            dynamic index = new DynamicIndex(dynamicEvents);

            ((int)index.SampleEventName[0].version).Should().Be(1);
            ((int)index.SampleEventName[0].Number).Should().Be(2);
            ((int)index.SampleEventName[1].version).Should().Be(1);
            ((int)index.SampleEventName[1].Number).Should().Be(2);
        }

        [TestMethod]
        public void TestOptionalEvent()
        {
            DynamicEventSchema.Set(
                new List<DynamicEventSchema>
                {
                    new DynamicEventSchema
                    {
                        DynamicEventName = "SampleEventName",
                        Fields = new List<KeyValuePair<string, Type>>
                        {
                            new KeyValuePair<string, Type>("version", typeof(ushort)),
                            new KeyValuePair<string, Type>("Number", typeof(ulong)),
                        }
                    },
                }
            );
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>();
            dynamic index = new DynamicIndex(dynamicEvents);
            ((bool)(index.SampleEventName == null)).Should().Be(true);
        }

        [TestMethod]
        public void TestForgivingUnknownEvent()
        {
            DynamicEventSchema.Set(correctSingleSchema);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>
            {
                sampleEvent,
                unknownEvent
            };
            dynamic index = new DynamicIndex(dynamicEvents);
            // As long as we don't throw exception, this is forgiving.
        }

        [TestMethod]
        public void TestReportingUnknownEvent()
        {
            DynamicEventSchema.Set(correctSingleSchema, false);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>
            {
                sampleEvent,
                unknownEvent
            };
            Action test = () =>
            {
                dynamic index = new DynamicIndex(dynamicEvents);
            };
            test.Should().Throw<Exception>();
        }

        [TestMethod]
        public void TestArray()
        {
            List<DynamicEventSchema> arraySchema = new List<DynamicEventSchema>
            {
                new DynamicEventSchema
                {
                    DynamicEventName = "ArrayEventName",
                    Fields = new List<KeyValuePair<string, Type>>
                    {
                        new KeyValuePair<string, Type>("version", typeof(ushort)),
                        new KeyValuePair<string, Type>("Array1", typeof(ushort[])),
                        new KeyValuePair<string, Type>("Number1", typeof(ulong)),
                        new KeyValuePair<string, Type>("Array2", typeof(byte[])),
                        new KeyValuePair<string, Type>("Number2", typeof(ulong)),
                    }
                },
            };
            DynamicEventSchema.Set(arraySchema, false);
            List<GCDynamicEvent> dynamicEvents = new List<GCDynamicEvent>
            {
                new GCDynamicEvent(
                    "ArrayEventName",
                    DateTime.Now,
                    //           ver   [ Array 1                     ]  [ Number 1           ]  [ Array 2               ]  [ Number 2           ]
                    new byte[] { 1, 0, 5, 1, 0, 0, 0, 0, 0, 8, 0, 6, 0, 1, 0, 0, 0, 0, 0, 0, 0, 8, 2, 8, 9, 6, 3, 0, 3, 5, 2, 0, 0, 0, 0, 0, 0, 0 }
                )
            };
            dynamic index = new DynamicIndex(dynamicEvents);
            ushort[] array1 = (ushort[])index.ArrayEventName.Array1;
            ulong number1 = (ulong)index.ArrayEventName.Number1;
            byte[] array2 = (byte[])index.ArrayEventName.Array2;
            ulong number2 = (ulong)index.ArrayEventName.Number2;
            array1.Length.Should().Be(5);
            array2.Length.Should().Be(8);
            array1[0].Should().Be(1);
            array1[1].Should().Be(0);
            array1[2].Should().Be(0);
            array1[3].Should().Be(8);
            array1[4].Should().Be(6);
            number1.Should().Be(1);
            array2[0].Should().Be(2);
            array2[1].Should().Be(8);
            array2[2].Should().Be(9);
            array2[3].Should().Be(6);
            array2[4].Should().Be(3);
            array2[5].Should().Be(0);
            array2[6].Should().Be(3);
            array2[7].Should().Be(5);
            number2.Should().Be(2);
        }
    }
}
