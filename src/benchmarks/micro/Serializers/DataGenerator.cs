// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using BenchmarkDotNet.Extensions;
using MessagePack;
using ProtoBuf;

namespace MicroBenchmarks.Serializers
{
    internal static class DataGenerator
    {
        internal static T Generate<T>()
        {
            if (typeof(T) == typeof(LoginViewModel))
                return (T)(object)CreateLoginViewModel();
            if (typeof(T) == typeof(Location))
                return (T)(object)CreateLocation();
            if (typeof(T) == typeof(IndexViewModel))
                return (T)(object)CreateIndexViewModel();
            if (typeof(T) == typeof(MyEventsListerViewModel))
                return (T)(object)CreateMyEventsListerViewModel();
            if (typeof(T) == typeof(BinaryData))
                return (T)(object)CreateBinaryData(1024);
            if (typeof(T) == typeof(CollectionsOfPrimitives))
                return (T)(object)CreateCollectionsOfPrimitives(1024); // 1024 values was copied from CoreFX benchmarks
            if (typeof(T) == typeof(XmlElement))
                return (T)(object)CreateXmlElement();
            if (typeof(T) == typeof(SimpleStructWithProperties))
                return (T)(object)new SimpleStructWithProperties { Num = 1, Text = "Foo" };
            if (typeof(T) == typeof(SimpleListOfInt))
                return (T)(object)new SimpleListOfInt { 10, 20, 30 };
            if (typeof(T) == typeof(ClassImplementingIXmlSerialiable))
                return (T)(object)new ClassImplementingIXmlSerialiable { StringValue = "Hello world" };
            if (typeof(T) == typeof(Dictionary<string, string>))
                return (T)(object)ValuesGenerator.ArrayOfUniqueValues<string>(100).ToDictionary(value => value);
            if (typeof(T) == typeof(ImmutableDictionary<string, string>))
                return (T)(object)ImmutableDictionary.CreateRange(ValuesGenerator.ArrayOfUniqueValues<string>(100).ToDictionary(value => value));
            if (typeof(T) == typeof(ImmutableSortedDictionary<string, string>))
                return (T)(object)ImmutableSortedDictionary.CreateRange(ValuesGenerator.ArrayOfUniqueValues<string>(100).ToDictionary(value => value));
            if (typeof(T) == typeof(HashSet<string>))
                return (T)(object)new HashSet<string>(ValuesGenerator.ArrayOfUniqueValues<string>(100));
            if (typeof(T) == typeof(ArrayList))
                return (T)(object)new ArrayList(ValuesGenerator.ArrayOfUniqueValues<string>(100));
            if (typeof(T) == typeof(Hashtable))
                return (T)(object)new Hashtable(ValuesGenerator.ArrayOfUniqueValues<string>(100).ToDictionary(value => value));
            if (typeof(T) == typeof(LargeStructWithProperties))
                return (T)(object)CreateLargeStructWithProperties();
            if (typeof(T) == typeof(int))
                return (T)(object)42;
            if (typeof(T) == typeof(SimpleStructWithProperties_Immutable))
                return (T)(object)new SimpleStructWithProperties_Immutable(num: 1, text: "Foo");
            if (typeof(T) == typeof(SimpleStructWithProperties_1Arg))
                return (T)(object)new SimpleStructWithProperties_1Arg(text: "Foo") { Num = 1 };
            if (typeof(T) == typeof(Parameterized_LoginViewModel_Immutable))
                return (T)(object)CreateParameterizedLoginViewModelImmutable();
            if (typeof(T) == typeof(Parameterized_LoginViewModel_2Args))
                return (T)(object)CreateParameterizedLoginViewModel2Args();
            if (typeof(T) == typeof(Parameterized_Location_Immutable))
                return (T)(object)CreateParameterizedLocationImmutable();
            if (typeof(T) == typeof(Parameterized_Location_5Args))
                return (T)(object)CreateParameterizedLocation5Args();
            if (typeof(T) == typeof(Parameterized_IndexViewModel_Immutable))
                return (T)(object)CreateParameterizedIndexViewModelImmutable();
            if (typeof(T) == typeof(Parameterized_IndexViewModel_2Args))
                return (T)(object)CreateParameterizedIndexViewModel2Args();
            if (typeof(T) == typeof(Parameterized_MyEventsListerViewModel_Immutable))
                return (T)(object)CreateParameterizedMyEventsListerViewModelImmutable();
            if (typeof(T) == typeof(Parameterized_MyEventsListerViewModel_2Args))
                return (T)(object)CreateParameterizedMyEventsListerViewModel2Args();
            if (typeof(T) == typeof(Parameterless_Point))
                return (T)(object)CreateParameterlessPoint();
            if (typeof(T) == typeof(Parameterized_Point_Immutable))
                return (T)(object)CreateParameterizedPointImmutable();
            if (typeof(T) == typeof(Parameterized_Point_1Arg))
                return (T)(object)CreateParameterizedPoint1Arg();
            if (typeof(T) == typeof(Parameterless_ClassWithPrimitives))
                return (T)(object)CreateParameterlessClassWithPrimitives();
            if (typeof(T) == typeof(Parameterized_ClassWithPrimitives_Immutable))
                return (T)(object)CreateParameterizedClassWithPrimitivesImmutable();
            if (typeof(T) == typeof(Parameterized_ClassWithPrimitives_4Args))
                return (T)(object)CreateParameterizedClassWithPrimitives4Args();

            throw new NotImplementedException();
        }

        private static LoginViewModel CreateLoginViewModel()
            => new LoginViewModel
            {
                Email = "name.familyname@not.com",
                Password = "abcdefgh123456!@",
                RememberMe = true
            };

        private static Location CreateLocation()
            => new Location
            {
                Id = 1234,
                Address1 = "The Street Name",
                Address2 = "20/11",
                City = "The City",
                State = "The State",
                PostalCode = "abc-12",
                Name = "Nonexisting",
                PhoneNumber = "+0 11 222 333 44",
                Country = "The Greatest"
            };

        private static IndexViewModel CreateIndexViewModel()
            => new IndexViewModel
            {
                IsNewAccount = false,
                FeaturedCampaign = new CampaignSummaryViewModel
                {
                    Description = "Very nice campaing",
                    Headline = "The Headline",
                    Id = 234235,
                    OrganizationName = "The Company XYZ",
                    ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                    Title = "Promoting Open Source"
                },
                ActiveOrUpcomingEvents = Enumerable.Repeat(
                    new ActiveOrUpcomingEvent
                    {
                        Id = 10,
                        CampaignManagedOrganizerName = "Name FamiltyName",
                        CampaignName = "The very new campaing",
                        Description = "The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.",
                        EndDate = DateTime.UtcNow.AddYears(1),
                        Name = "Just a name",
                        ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                        StartDate = DateTime.UtcNow
                    },
                    count: 20).ToList()
            };

        private static LargeStructWithProperties CreateLargeStructWithProperties()
            => new LargeStructWithProperties
            {
                String1 = "1",
                String2 = "2",
                String3 = "3",
                String4 = "4",
                String5 = "5",
            };

        private static MyEventsListerViewModel CreateMyEventsListerViewModel()
            => new MyEventsListerViewModel
            {
                CurrentEvents = Enumerable.Repeat(CreateMyEventsListerItem(), 3).ToList(),
                FutureEvents = Enumerable.Repeat(CreateMyEventsListerItem(), 9).ToList(),
                PastEvents = Enumerable.Repeat(CreateMyEventsListerItem(), 60).ToList() // usually  there is a lot of historical data
            };

        private static MyEventsListerItem CreateMyEventsListerItem()
            => new MyEventsListerItem
            {
                Campaign = "A very nice campaing",
                EndDate = DateTime.UtcNow.AddDays(7),
                EventId = 321,
                EventName = "wonderful name",
                Organization = "Local Animal Shelter",
                StartDate = DateTime.UtcNow.AddDays(-7),
                TimeZone = TimeZoneInfo.Utc.DisplayName,
                VolunteerCount = 15,
                Tasks = Enumerable.Repeat(
                    new MyEventsListerItemTask
                    {
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(1),
                        Name = "A very nice task to have"
                    }, 4).ToList()
            };

        private static BinaryData CreateBinaryData(int size)
            => new BinaryData
            {
                ByteArray = CreateByteArray(size)
            };

        private static CollectionsOfPrimitives CreateCollectionsOfPrimitives(int count)
            => new CollectionsOfPrimitives
            {
                ByteArray = CreateByteArray(count),
                DateTimeArray = CreateDateTimeArray(count),
                Dictionary = CreateDictionaryOfIntString(count),
                ListOfInt = CreateListOfInt(count)
            };

        private static DateTime[] CreateDateTimeArray(int count)
        {
            DateTime[] arr = new DateTime[count];
            int kind = (int)DateTimeKind.Unspecified;
            int maxDateTimeKind = (int)DateTimeKind.Local;
            DateTime val = DateTime.Now.AddHours(count / 2);
            for (int i = 0; i < count; i++)
            {
                arr[i] = DateTime.SpecifyKind(val, (DateTimeKind)kind);
                val = val.AddHours(1);
                kind = (kind + 1) % maxDateTimeKind;
            }

            return arr;
        }

        private static Dictionary<int, string> CreateDictionaryOfIntString(int count)
        {
            Dictionary<int, string> dictOfIntString = new Dictionary<int, string>(count);
            for (int i = 0; i < count; ++i)
            {
                dictOfIntString[i] = i.ToString();
            }

            return dictOfIntString;
        }

        private static byte[] CreateByteArray(int size)
        {
            byte[] obj = new byte[size];
            for (int i = 0; i < obj.Length; ++i)
            {
                unchecked
                {
                    obj[i] = (byte)i;
                }
            }
            return obj;
        }

        private static List<int> CreateListOfInt(int count) => Enumerable.Range(0, count).ToList();

        private static XmlElement CreateXmlElement()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"<html></html>");
            XmlElement xmlElement = xmlDoc.CreateElement("Element");
            xmlElement.InnerText = "Element innertext";
            return xmlElement;
        }

        private static Parameterized_LoginViewModel_2Args CreateParameterizedLoginViewModel2Args()
            => new Parameterized_LoginViewModel_2Args(email: "name.familyname@not.com", rememberMe: true)
            {
                Password = "abcdefgh123456!@",
            };

        private static Parameterized_LoginViewModel_Immutable CreateParameterizedLoginViewModelImmutable()
            => new Parameterized_LoginViewModel_Immutable(email: "name.familyname@not.com", password: "abcdefgh123456!@", rememberMe: true);

        private static Parameterized_Location_5Args CreateParameterizedLocation5Args() =>
            new Parameterized_Location_5Args(
                id: 1234,
                city: "The City",
                state: "The State",
                postalCode: "abc-12",
                name: "Nonexisting")
            {
                Address1 = "The Street Name",
                Address2 = "20/11",
                PhoneNumber = "+0 11 222 333 44",
                Country = "The Greatest"
            };

        private static Parameterized_Location_Immutable CreateParameterizedLocationImmutable() =>
            new Parameterized_Location_Immutable(
                id: 1234,
                address1: "The Street Name",
                address2: "20/11",
                city: "The City",
                state: "The State",
                postalCode: "abc-12",
                name: "Nonexisting",
                phoneNumber: "+0 11 222 333 44",
                country: "The Greatest");

        private static Parameterized_IndexViewModel_Immutable CreateParameterizedIndexViewModelImmutable()
            => new Parameterized_IndexViewModel_Immutable(
                isNewAccount: false,
                featuredCampaign: new CampaignSummaryViewModel
                {
                    Description = "Very nice campaing",
                    Headline = "The Headline",
                    Id = 234235,
                    OrganizationName = "The Company XYZ",
                    ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                    Title = "Promoting Open Source"
                },
                activeOrUpcomingEvents: Enumerable.Repeat(
                    new ActiveOrUpcomingEvent
                    {
                        Id = 10,
                        CampaignManagedOrganizerName = "Name FamiltyName",
                        CampaignName = "The very new campaing",
                        Description = "The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.",
                        EndDate = DateTime.UtcNow.AddYears(1),
                        Name = "Just a name",
                        ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                        StartDate = DateTime.UtcNow
                    },
                    count: 20).ToList());

        private static Parameterized_IndexViewModel_2Args CreateParameterizedIndexViewModel2Args()
            => new Parameterized_IndexViewModel_2Args(
                featuredCampaign: new CampaignSummaryViewModel
                {
                    Description = "Very nice campaing",
                    Headline = "The Headline",
                    Id = 234235,
                    OrganizationName = "The Company XYZ",
                    ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                    Title = "Promoting Open Source"
                },
                isNewAccount: false
                )
            {
                ActiveOrUpcomingEvents = Enumerable.Repeat(
                    new ActiveOrUpcomingEvent
                    {
                        Id = 10,
                        CampaignManagedOrganizerName = "Name FamiltyName",
                        CampaignName = "The very new campaing",
                        Description = "The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.",
                        EndDate = DateTime.UtcNow.AddYears(1),
                        Name = "Just a name",
                        ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                        StartDate = DateTime.UtcNow
                    },
                    count: 20).ToList()
            };

        private static Parameterized_MyEventsListerViewModel_2Args CreateParameterizedMyEventsListerViewModel2Args()
            => new Parameterized_MyEventsListerViewModel_2Args(
                currentEvents: Enumerable.Repeat(CreateMyEventsListerItem(), 3).ToList(),
                pastEvents: Enumerable.Repeat(CreateMyEventsListerItem(), 60).ToList() // usually  there is a lot of historical data
                )
            {
                FutureEvents = Enumerable.Repeat(CreateMyEventsListerItem(), 9).ToList()
            };

        private static Parameterized_MyEventsListerViewModel_Immutable CreateParameterizedMyEventsListerViewModelImmutable()
            => new Parameterized_MyEventsListerViewModel_Immutable(
                currentEvents: Enumerable.Repeat(CreateMyEventsListerItem(), 3).ToList(),
                futureEvents: Enumerable.Repeat(CreateMyEventsListerItem(), 9).ToList(),
                pastEvents: Enumerable.Repeat(CreateMyEventsListerItem(), 60).ToList() // usually  there is a lot of historical data
                );

        private static Parameterless_Point CreateParameterlessPoint()
            => new Parameterless_Point()
            {
                X = 234235,
                Y = 912874
            };

        private static Parameterized_Point_1Arg CreateParameterizedPoint1Arg()
            => new Parameterized_Point_1Arg(234235)
            {
                Y = 912874
            };

        private static Parameterized_Point_Immutable CreateParameterizedPointImmutable()
        {
            var point = new Parameterized_Point_Immutable(234235, 912874);
            return point;
        }

        private static Parameterless_ClassWithPrimitives CreateParameterlessClassWithPrimitives()
            => new Parameterless_ClassWithPrimitives()
            {
                FirstInt = 348943,
                FirstString = "934sdkjfskdfssf",
                FirstDateTime = DateTime.Now,
                SecondDateTime = DateTime.Now.AddHours(1).AddYears(1),
                X = 234235,
                Y = 912874,
                Z = 434934,
                W = 348943,
            };

        private static Parameterized_ClassWithPrimitives_4Args CreateParameterizedClassWithPrimitives4Args()
            => new Parameterized_ClassWithPrimitives_4Args(w: 349943, x: 234235, y: 912874, z: 434934)
            {
                FirstInt = 348943,
                FirstString = "934sdkjfskdfssf",
                FirstDateTime = DateTime.Now,
                SecondDateTime = DateTime.Now.AddHours(1).AddYears(1)
            };

        private static Parameterized_ClassWithPrimitives_Immutable CreateParameterizedClassWithPrimitivesImmutable()
            => new Parameterized_ClassWithPrimitives_Immutable(
                firstDateTime: DateTime.Now,
                secondDateTime: DateTime.Now.AddHours(1).AddYears(1),
                x: 234235,
                y: 912874,
                z: 434934,
                w: 348943,
                firstInt: 348943,
                firstString: "934sdkjfskdfssf");
    }
    // the view models come from a real world app called "AllReady"
    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class LoginViewModel
    {
        [ProtoMember(1)] [Key(0)] public string Email { get; set; }
        [ProtoMember(2)] [Key(1)] public string Password { get; set; }
        [ProtoMember(3)] [Key(2)] public bool RememberMe { get; set; }
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class Location
    {
        [ProtoMember(1)] [Key(0)] public int Id { get; set; }
        [ProtoMember(2)] [Key(1)] public string Address1 { get; set; }
        [ProtoMember(3)] [Key(2)] public string Address2 { get; set; }
        [ProtoMember(4)] [Key(3)] public string City { get; set; }
        [ProtoMember(5)] [Key(4)] public string State { get; set; }
        [ProtoMember(6)] [Key(5)] public string PostalCode { get; set; }
        [ProtoMember(7)] [Key(6)] public string Name { get; set; }
        [ProtoMember(8)] [Key(7)] public string PhoneNumber { get; set; }
        [ProtoMember(9)] [Key(8)] public string Country { get; set; }
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class ActiveOrUpcomingCampaign
    {
        [ProtoMember(1)] [Key(0)] public int Id { get; set; }
        [ProtoMember(2)] [Key(1)] public string ImageUrl { get; set; }
        [ProtoMember(3)] [Key(2)] public string Name { get; set; }
        [ProtoMember(4)] [Key(3)] public string Description { get; set; }
        [ProtoMember(5)] [Key(4)] public DateTimeOffset StartDate { get; set; }
        [ProtoMember(6)] [Key(5)] public DateTimeOffset EndDate { get; set; }
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class ActiveOrUpcomingEvent
    {
        [ProtoMember(1)] [Key(0)] public int Id { get; set; }
        [ProtoMember(2)] [Key(1)] public string ImageUrl { get; set; }
        [ProtoMember(3)] [Key(2)] public string Name { get; set; }
        [ProtoMember(4)] [Key(3)] public string CampaignName { get; set; }
        [ProtoMember(5)] [Key(4)] public string CampaignManagedOrganizerName { get; set; }
        [ProtoMember(6)] [Key(5)] public string Description { get; set; }
        [ProtoMember(7)] [Key(6)] public DateTimeOffset StartDate { get; set; }
        [ProtoMember(8)] [Key(7)] public DateTimeOffset EndDate { get; set; }
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class CampaignSummaryViewModel
    {
        [ProtoMember(1)] [Key(0)] public int Id { get; set; }
        [ProtoMember(2)] [Key(1)] public string Title { get; set; }
        [ProtoMember(3)] [Key(2)] public string Description { get; set; }
        [ProtoMember(4)] [Key(3)] public string ImageUrl { get; set; }
        [ProtoMember(5)] [Key(4)] public string OrganizationName { get; set; }
        [ProtoMember(6)] [Key(5)] public string Headline { get; set; }
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class IndexViewModel
    {
        [ProtoMember(1)] [Key(0)] public List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
        [ProtoMember(2)] [Key(1)] public CampaignSummaryViewModel FeaturedCampaign { get; set; }
        [ProtoMember(3)] [Key(2)] public bool IsNewAccount { get; set; }
        [IgnoreMember] public bool HasFeaturedCampaign => FeaturedCampaign != null;
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class MyEventsListerViewModel
    {
        // the orginal type defined these fields as IEnumerable,
        // but XmlSerializer failed to serialize them with "cannot serialize member because it is an interface" error
        [ProtoMember(1)] [Key(0)] public List<MyEventsListerItem> CurrentEvents { get; set; } = new List<MyEventsListerItem>();
        [ProtoMember(2)] [Key(1)] public List<MyEventsListerItem> FutureEvents { get; set; } = new List<MyEventsListerItem>();
        [ProtoMember(3)] [Key(2)] public List<MyEventsListerItem> PastEvents { get; set; } = new List<MyEventsListerItem>();
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class MyEventsListerItem
    {
        [ProtoMember(1)] [Key(0)] public int EventId { get; set; }
        [ProtoMember(2)] [Key(1)] public string EventName { get; set; }
        [ProtoMember(3)] [Key(2)] public DateTimeOffset StartDate { get; set; }
        [ProtoMember(4)] [Key(3)] public DateTimeOffset EndDate { get; set; }
        [ProtoMember(5)] [Key(4)] public string TimeZone { get; set; }
        [ProtoMember(6)] [Key(5)] public string Campaign { get; set; }
        [ProtoMember(7)] [Key(6)] public string Organization { get; set; }
        [ProtoMember(8)] [Key(7)] public int VolunteerCount { get; set; }

        [ProtoMember(9)] [Key(8)] public List<MyEventsListerItemTask> Tasks { get; set; } = new List<MyEventsListerItemTask>();
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class MyEventsListerItemTask
    {
        [ProtoMember(1)] [Key(0)] public string Name { get; set; }
        [ProtoMember(2)] [Key(1)] public DateTimeOffset? StartDate { get; set; }
        [ProtoMember(3)] [Key(2)] public DateTimeOffset? EndDate { get; set; }

        [IgnoreMember]
        public string FormattedDate
        {
            get
            {
                if (!StartDate.HasValue || !EndDate.HasValue)
                {
                    return null;
                }

                var startDateString = string.Format("{0:g}", StartDate.Value);
                var endDateString = string.Format("{0:g}", EndDate.Value);

                return string.Format($"From {startDateString} to {endDateString}");
            }
        }
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class BinaryData
    {
        [ProtoMember(1)] [Key(0)] public byte[] ByteArray { get; set; }
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public class CollectionsOfPrimitives
    {
        [ProtoMember(1)] [Key(0)] public byte[] ByteArray { get; set; }
        [ProtoMember(2)] [Key(1)] public DateTime[] DateTimeArray { get; set; }

        [XmlIgnore] // xml serializer does not support anything that implements IDictionary..
        [ProtoMember(3)] [Key(2)] public Dictionary<int, string> Dictionary { get; set; }

        [ProtoMember(4)] [Key(3)] public List<int> ListOfInt { get; set; }
    }

    [Serializable]
    [ProtoContract]
    [MessagePackObject]
    public struct LargeStructWithProperties
    {
        [ProtoMember(1)] [Key(0)] public string String1 { get; set; }
        [ProtoMember(2)] [Key(1)] public string String2 { get; set; }
        [ProtoMember(3)] [Key(2)] public string String3 { get; set; }
        [ProtoMember(4)] [Key(3)] public string String4 { get; set; }
        [ProtoMember(5)] [Key(4)] public string String5 { get; set; }
        [ProtoMember(6)] [Key(5)] public int Int1 { get; set; }
        [ProtoMember(7)] [Key(6)] public int Int2 { get; set; }
        [ProtoMember(8)] [Key(7)] public int Int3 { get; set; }
        [ProtoMember(9)] [Key(8)] public int Int4 { get; set; }
        [ProtoMember(10)] [Key(9)] public int Int5 { get; set; }
    }

    public struct SimpleStructWithProperties
    {
        public int Num { get; set; }
        public string Text { get; set; }
    }

    public class SimpleListOfInt : List<int> { }

    public struct SimpleStructWithProperties_Immutable
    {
        public int Num { get; }
        public string Text { get; }

        //[JsonConstructor]
        public SimpleStructWithProperties_Immutable(int num, string text) => (Num, Text) = (num, text);
    }

    public struct SimpleStructWithProperties_1Arg
    {
        public int Num { get; set; }
        public string Text { get; }

        //[JsonConstructor]
        public SimpleStructWithProperties_1Arg(string text) => (Num, Text) = (0, text);
    }

    public class Parameterized_LoginViewModel_Immutable
    {
        public string Email { get; }
        public string Password { get; }
        public bool RememberMe { get; }

        public Parameterized_LoginViewModel_Immutable(string email, string password, bool rememberMe)
        {
            Email = email;
            Password = password;
            RememberMe = rememberMe;
        }
    }

    public class Parameterized_LoginViewModel_2Args
    {
        public string Password { get; set; }
        public string Email { get; }
        public bool RememberMe { get; }

        public Parameterized_LoginViewModel_2Args(string email, bool rememberMe) => (Email, RememberMe) = (email, rememberMe);
    }

    public class Parameterized_Location_Immutable
    {
        public int Id { get; }
        public string Address1 { get; }
        public string Address2 { get; }
        public string City { get; }
        public string State { get; }
        public string PostalCode { get; }
        public string Name { get; }
        public string PhoneNumber { get; }
        public string Country { get; }

        public Parameterized_Location_Immutable(
            int id,
            string address1,
            string address2,
            string city,
            string state,
            string postalCode,
            string name,
            string phoneNumber,
            string country)
        {
            Id = id;
            Address1 = address1;
            Address2 = address2;
            City = city;
            State = state;
            PostalCode = postalCode;
            Name = name;
            PhoneNumber = phoneNumber;
            Country = country;
        }
    }

    public class Parameterized_Location_5Args
    {
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; }
        public string State { get; }
        public int Id { get; }
        public string PostalCode { get; }
        public string Name { get; }
        public string PhoneNumber { get; set; }
        public string Country { get; set; }

        public Parameterized_Location_5Args(string city, string state, int id, string postalCode, string name)
        {
            City = city;
            State = state;
            Id = id;
            PostalCode = postalCode;
            Name = name;
        }
    }

    public class Parameterized_IndexViewModel_Immutable
    {
        public List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; }
        public CampaignSummaryViewModel FeaturedCampaign { get; }
        public bool IsNewAccount { get; }
        public bool HasFeaturedCampaign => FeaturedCampaign != null;

        public Parameterized_IndexViewModel_Immutable(
            List<ActiveOrUpcomingEvent> activeOrUpcomingEvents,
            CampaignSummaryViewModel featuredCampaign,
            bool isNewAccount)
        {
            ActiveOrUpcomingEvents = activeOrUpcomingEvents;
            FeaturedCampaign = featuredCampaign;
            IsNewAccount = isNewAccount;
        }
    }

    public class Parameterized_IndexViewModel_2Args
    {
        public List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
        public CampaignSummaryViewModel FeaturedCampaign { get; }
        public bool IsNewAccount { get; }
        public bool HasFeaturedCampaign => FeaturedCampaign != null;

        public Parameterized_IndexViewModel_2Args(CampaignSummaryViewModel featuredCampaign, bool isNewAccount)
        {
            FeaturedCampaign = featuredCampaign;
            IsNewAccount = isNewAccount;
        }
    }

    public class Parameterized_MyEventsListerViewModel_Immutable
    {
        public List<MyEventsListerItem> CurrentEvents { get; } = new List<MyEventsListerItem>();
        public List<MyEventsListerItem> FutureEvents { get; } = new List<MyEventsListerItem>();
        public List<MyEventsListerItem> PastEvents { get; } = new List<MyEventsListerItem>();

        public Parameterized_MyEventsListerViewModel_Immutable(
            List<MyEventsListerItem> currentEvents,
            List<MyEventsListerItem> futureEvents,
            List<MyEventsListerItem> pastEvents)
        {
            CurrentEvents = currentEvents;
            FutureEvents = futureEvents;
            PastEvents = pastEvents;
        }
    }

    public class Parameterized_MyEventsListerViewModel_2Args
    {
        public List<MyEventsListerItem> FutureEvents { get; set; } = new List<MyEventsListerItem>();
        public List<MyEventsListerItem> CurrentEvents { get; } = new List<MyEventsListerItem>();
        public List<MyEventsListerItem> PastEvents { get; } = new List<MyEventsListerItem>();

        public Parameterized_MyEventsListerViewModel_2Args(
            List<MyEventsListerItem> currentEvents, List<MyEventsListerItem> pastEvents
            ) => (CurrentEvents, PastEvents) = (currentEvents, pastEvents);
    }

    public class Parameterless_Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Parameterized_Point_Immutable
    {
        public int X { get; }
        public int Y { get; }

        public Parameterized_Point_Immutable(int x, int y) => (X, Y) = (x, y);
    }

    public class Parameterized_Point_1Arg
    {
        public int X { get; }
        public int Y { get; set; }

        public Parameterized_Point_1Arg(int x) => X = x;
    }

    public class Parameterless_ClassWithPrimitives
    {
        public DateTime FirstDateTime { get; set; }
        public DateTime SecondDateTime { get; set; }
        public int W { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int FirstInt { get; set; }
        public string FirstString { get; set; }
    }

    public class Parameterized_ClassWithPrimitives_Immutable
    {
        public DateTime FirstDateTime { get; }
        public DateTime SecondDateTime { get; }
        public int W { get; }
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public int FirstInt { get; }
        public string FirstString { get; }

        public Parameterized_ClassWithPrimitives_Immutable(
            DateTime firstDateTime,
            DateTime secondDateTime,
            int x,
            int y,
            int z,
            int w,
            int firstInt,
            string firstString)
        {
            FirstDateTime = firstDateTime;
            SecondDateTime = secondDateTime;
            W = w;
            X = x;
            Y = y;
            Z = z;
            FirstInt = firstInt;
            FirstString = firstString;
        }
    }

    public class Parameterized_ClassWithPrimitives_4Args
    {
        public DateTime FirstDateTime { get; set; }
        public DateTime SecondDateTime { get; set; }
        public int W { get; }
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public int FirstInt { get; set; }
        public string FirstString { get; set; }

        public Parameterized_ClassWithPrimitives_4Args(int w, int x, int y, int z) => (W, X, Y, Z) = (w, x, y, z);
    }

    public class ClassImplementingIXmlSerialiable : IXmlSerializable
    {
        public string StringValue { get; set; }
        private bool BoolValue { get; set; }

        public ClassImplementingIXmlSerialiable() => BoolValue = true;

        public System.Xml.Schema.XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            StringValue = reader.GetAttribute("StringValue");
            BoolValue = bool.Parse(reader.GetAttribute("BoolValue"));
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("StringValue", StringValue);
            writer.WriteAttributeString("BoolValue", BoolValue.ToString());
        }
    }
}