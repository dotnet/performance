using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using ProtoBuf;
using ZeroFormatter;

#if SGEN
// the new SGEN tool fails to load some of the dependencies, so we need to replace the problematic dependencies for this particular build configuration
// see https://github.com/dotnet/corefx/issues/27281#issuecomment-367449130 fore more
using Benchmarks.Serializers.Helpers;
#else

#endif

namespace Benchmarks.Serializers
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
    }

    /// <summary>
    /// ZeroFormatter requires all properties to be virtual
    /// they are deserialized for real when they are used for the first time
    /// if we don't touch the properites, they are not being deserialized and the result is skewed
    /// </summary>
    public interface IVerifiable
    {
        long TouchEveryProperty();
    }

    // the view models come from a real world app called "AllReady"
    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class LoginViewModel : IVerifiable
    {
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual string Email { get; set; }
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual string Password { get; set; }
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual bool RememberMe { get; set; }

        public long TouchEveryProperty() => Email.Length + Password.Length + Convert.ToInt32(RememberMe);
    }

    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class Location : IVerifiable
    {
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual int Id { get; set; }
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual string Address1 { get; set; }
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual string Address2 { get; set; }
        [ProtoMember(4)] [Index(3)] [Key(3)] public virtual string City { get; set; }
        [ProtoMember(5)] [Index(4)] [Key(4)] public virtual string State { get; set; }
        [ProtoMember(6)] [Index(5)] [Key(5)] public virtual string PostalCode { get; set; }
        [ProtoMember(7)] [Index(6)] [Key(6)] public virtual string Name { get; set; }
        [ProtoMember(8)] [Index(7)] [Key(7)] public virtual string PhoneNumber { get; set; }
        [ProtoMember(9)] [Index(8)] [Key(8)] public virtual string Country { get; set; }

        public long TouchEveryProperty() => Id + Address1.Length + Address2.Length + City.Length + State.Length + PostalCode.Length + Name.Length + PhoneNumber.Length + Country.Length;
    }

    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class ActiveOrUpcomingCampaign : IVerifiable
    {
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual int Id { get; set; }
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual string ImageUrl { get; set; }
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual string Name { get; set; }
        [ProtoMember(4)] [Index(3)] [Key(3)] public virtual string Description { get; set; }
        [ProtoMember(5)] [Index(4)] [Key(4)] public virtual DateTimeOffset StartDate { get; set; }
        [ProtoMember(6)] [Index(5)] [Key(5)] public virtual DateTimeOffset EndDate { get; set; }

        public long TouchEveryProperty() => Id + ImageUrl.Length + Name.Length + Description.Length + StartDate.Ticks + EndDate.Ticks;
    }

    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class ActiveOrUpcomingEvent : IVerifiable
    {
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual int Id { get; set; }
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual string ImageUrl { get; set; }
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual string Name { get; set; }
        [ProtoMember(4)] [Index(3)] [Key(3)] public virtual string CampaignName { get; set; }
        [ProtoMember(5)] [Index(4)] [Key(4)] public virtual string CampaignManagedOrganizerName { get; set; }
        [ProtoMember(6)] [Index(5)] [Key(5)] public virtual string Description { get; set; }
        [ProtoMember(7)] [Index(6)] [Key(6)] public virtual DateTimeOffset StartDate { get; set; }
        [ProtoMember(8)] [Index(7)] [Key(7)] public virtual DateTimeOffset EndDate { get; set; }

        public long TouchEveryProperty() => Id + ImageUrl.Length + Name.Length + CampaignName.Length + CampaignManagedOrganizerName.Length + Description.Length + StartDate.Ticks + EndDate.Ticks;
    }

    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class CampaignSummaryViewModel : IVerifiable
    {
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual int Id { get; set; }
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual string Title { get; set; }
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual string Description { get; set; }
        [ProtoMember(4)] [Index(3)] [Key(3)] public virtual string ImageUrl { get; set; }
        [ProtoMember(5)] [Index(4)] [Key(4)] public virtual string OrganizationName { get; set; }
        [ProtoMember(6)] [Index(5)] [Key(5)] public virtual string Headline { get; set; }

        public long TouchEveryProperty() => Id + Title.Length + Description.Length + ImageUrl.Length + OrganizationName.Length + Headline.Length;
    }

    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class IndexViewModel : IVerifiable
    {
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual CampaignSummaryViewModel FeaturedCampaign { get; set; }
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual bool IsNewAccount { get; set; }
        [IgnoreFormat] [IgnoreMember] public bool HasFeaturedCampaign => FeaturedCampaign != null;

        public long TouchEveryProperty()
        {
            long result = FeaturedCampaign.TouchEveryProperty() + Convert.ToInt32(IsNewAccount);

            for (int i = 0; i < ActiveOrUpcomingEvents.Count; i++) // no LINQ here to prevent from skewing allocations results
                result += ActiveOrUpcomingEvents[i].TouchEveryProperty();

            return result;
        }
    }

    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class MyEventsListerViewModel : IVerifiable
    {
        // the orginal type defined these fields as IEnumerable,
        // but XmlSerializer failed to serialize them with "cannot serialize member because it is an interface" error
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual List<MyEventsListerItem> CurrentEvents { get; set; } = new List<MyEventsListerItem>();
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual List<MyEventsListerItem> FutureEvents { get; set; } = new List<MyEventsListerItem>();
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual List<MyEventsListerItem> PastEvents { get; set; } = new List<MyEventsListerItem>();

        public long TouchEveryProperty()
        {
            long result = 0;

            // no LINQ here to prevent from skewing allocations results
            for (int i = 0; i < CurrentEvents.Count; i++) result += CurrentEvents[i].TouchEveryProperty();
            for (int i = 0; i < FutureEvents.Count; i++) result += FutureEvents[i].TouchEveryProperty();
            for (int i = 0; i < PastEvents.Count; i++) result += PastEvents[i].TouchEveryProperty();

            return result;
        }
    }

    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class MyEventsListerItem : IVerifiable
    {
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual int EventId { get; set; }
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual string EventName { get; set; }
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual DateTimeOffset StartDate { get; set; }
        [ProtoMember(4)] [Index(3)] [Key(3)] public virtual DateTimeOffset EndDate { get; set; }
        [ProtoMember(5)] [Index(4)] [Key(4)] public virtual string TimeZone { get; set; }
        [ProtoMember(6)] [Index(5)] [Key(5)] public virtual string Campaign { get; set; }
        [ProtoMember(7)] [Index(6)] [Key(6)] public virtual string Organization { get; set; }
        [ProtoMember(8)] [Index(7)] [Key(7)] public virtual int VolunteerCount { get; set; }

        [ProtoMember(9)] [Index(8)] [Key(8)] public virtual List<MyEventsListerItemTask> Tasks { get; set; } = new List<MyEventsListerItemTask>();

        public long TouchEveryProperty()
        {
            long result = EventId + EventName.Length + StartDate.Ticks + EndDate.Ticks + TimeZone.Length + Campaign.Length + Organization.Length + VolunteerCount;

            for (int i = 0; i < Tasks.Count; i++) // no LINQ here to prevent from skewing allocations results
                result += Tasks[i].TouchEveryProperty();

            return result;
        }
    }

    [Serializable]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public class MyEventsListerItemTask : IVerifiable
    {
        [ProtoMember(1)] [Index(0)] [Key(0)] public virtual string Name { get; set; }
        [ProtoMember(2)] [Index(1)] [Key(1)] public virtual DateTimeOffset? StartDate { get; set; }
        [ProtoMember(3)] [Index(2)] [Key(2)] public virtual DateTimeOffset? EndDate { get; set; }

        [IgnoreFormat]
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

        public long TouchEveryProperty() => Name.Length + StartDate.Value.Ticks + EndDate.Value.Ticks;
    }
}