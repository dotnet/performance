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
            Action test = () => { 
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
            Action test = () => { 
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
            Action test = () => { 
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
        public void TestValidSchema()
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
            DynamicEvent sampleEvent = new DynamicEvent(
                "SampleEventName",
                DateTime.Now,
                new byte[] {1, 0, 2, 0, 0, 0, 0, 0, 0, 0}
            );
        
            List<DynamicEvent> dynamicEvents = new List<DynamicEvent>
            {
                sampleEvent
            };
            dynamic index = new DynamicIndex(dynamicEvents);

            ((int)index.SampleEventName.version).Should().Be(1);
            ((int)index.SampleEventName.Number).Should().Be(2);
        }
    }
}