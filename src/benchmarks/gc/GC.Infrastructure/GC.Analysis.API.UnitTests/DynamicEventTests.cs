// TODO, andrewau, remove this condition when new TraceEvent is available through Nuget.
#if CUSTOM_TRACE_EVENT

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
    }
}

#endif