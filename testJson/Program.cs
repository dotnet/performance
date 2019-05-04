using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace testJson
{
    class Program
    {

        // ..\tools\dotnet\x64\dotnet.exe run -c Release -- 1 {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true}
        // ..\tools\dotnet\x64\dotnet.exe run -c Release -- 2 {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true}

        // ..\tools\dotnet\x64\dotnet.exe run -c Release -- 1 {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true} {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh12345!@\",\"RememberMe\":true} {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh1235!@\",\"RememberMe\":true} {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh2345!@\",\"RememberMe\":true} {\"Email\":\"name.familyname@not.com\",\"Password\":\"acdefgh12345!@\",\"RememberMe\":true}
        // ..\tools\dotnet\x64\dotnet.exe run -c Release -- 2 {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true} {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh12345!@\",\"RememberMe\":true} {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh1235!@\",\"RememberMe\":true} {\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh2345!@\",\"RememberMe\":true} {\"Email\":\"name.familyname@not.com\",\"Password\":\"acdefgh12345!@\",\"RememberMe\":true}
    
        static void Main(string[] args)
        {
            //ReadLoginViewModel();
            //ReadIndexViewModel();

            // LoginViewModel _1 = JsonSerializer.Parse<LoginViewModel>("{\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true}");
            // LoginViewModel _2 = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginViewModel>("{\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true}");

            // Stopwatch sw = new Stopwatch();

            // sw.Restart();
            // for (int i = 0; i < 100; i++)
            // {
            //     LoginViewModel result = JsonSerializer.Parse<LoginViewModel>("{\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true}");
            // }
            // sw.Stop();
            // Console.WriteLine(sw.ElapsedTicks);


            // sw.Restart();
            // for (int i = 0; i < 100; i++)
            // {
            //     LoginViewModel result = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginViewModel>("{\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true}");
            // }
            // sw.Stop();
            // Console.WriteLine(sw.ElapsedTicks);

            // string[] strings = new string[1_000];

            // strings[0] = JsonSerializer.ToString(_1);
            // strings[0] = Newtonsoft.Json.JsonConvert.SerializeObject(_2);

            // sw.Restart();
            // for (int i = 0; i < 100; i++)
            // {
            //     strings[i] = JsonSerializer.ToString(_1);
            // }
            // sw.Stop();
            // Console.WriteLine(sw.ElapsedTicks);


            // sw.Restart();
            // for (int i = 0; i < 100; i++)
            // {
            //     strings[i] = Newtonsoft.Json.JsonConvert.SerializeObject(_2);
            // }
            // sw.Stop();
            // Console.WriteLine(sw.ElapsedTicks);

            var sw = new Stopwatch();
            sw.Restart();
            if (args[0] == "1")
            {
                for (int j = 0; j < 100_000; j++)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        RoundtripSTJ(args[i]);
                    }
                }
            }
            else
            {
                for (int j = 0; j < 100_000; j++)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        RoundtripNewtonsoft(args[i]);
                    }
                }
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);
        }

        private static void RoundtripSTJ(string str)
        {
            LoginViewModel _1 = JsonSerializer.Parse<LoginViewModel>(str);
            string roundTrip = JsonSerializer.ToString(_1);
            if (str != roundTrip)
            {
                throw new Exception();
            }
        }

        private static void RoundtripNewtonsoft(string str)
        {
            LoginViewModel _1 = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginViewModel>(str);
            string roundTrip =  Newtonsoft.Json.JsonConvert.SerializeObject(_1);
            if (str != roundTrip)
            {
                throw new Exception();
            }
        }

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

        public static void ReadLoginViewModel()
        {
            LoginViewModel result = JsonSerializer.Parse<LoginViewModel>("{\"Email\":\"name.familyname@not.com\",\"Password\":\"abcdefgh123456!@\",\"RememberMe\":true}");

            Console.WriteLine(result.Email);
            Console.WriteLine(result.Password);
            Console.WriteLine(result.RememberMe);
        }

        public static void ReadIndexViewModel()
        {
            string str = JsonSerializer.ToString(CreateIndexViewModel());
            Console.WriteLine(str);
            IndexViewModel result = JsonSerializer.Parse<IndexViewModel>(str);

            Console.WriteLine(result.IsNewAccount);
            Console.WriteLine(result.HasFeaturedCampaign);
            Console.WriteLine(result.ActiveOrUpcomingEvents.Count);
            Console.WriteLine(result.FeaturedCampaign.OrganizationName);
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

        [Serializable]
        public class LoginViewModel : IVerifiable
        {
            public virtual string Email { get; set; }
            public virtual string Password { get; set; }
            public virtual bool RememberMe { get; set; }

            public long TouchEveryProperty() => Email.Length + Password.Length + Convert.ToInt32(RememberMe);
        }

        [Serializable]
        public class IndexViewModel : IVerifiable
        {
            public virtual List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
            public virtual CampaignSummaryViewModel FeaturedCampaign { get; set; }
            public virtual bool IsNewAccount { get; set; }
            public bool HasFeaturedCampaign => FeaturedCampaign != null;

            public long TouchEveryProperty()
            {
                long result = FeaturedCampaign.TouchEveryProperty() + Convert.ToInt32(IsNewAccount);

                for (int i = 0; i < ActiveOrUpcomingEvents.Count; i++) // no LINQ here to prevent from skewing allocations results
                    result += ActiveOrUpcomingEvents[i].TouchEveryProperty();

                return result;
            }
        }

        [Serializable]
        public class ActiveOrUpcomingEvent : IVerifiable
        {
            public virtual int Id { get; set; }
            public virtual string ImageUrl { get; set; }
            public virtual string Name { get; set; }
            public virtual string CampaignName { get; set; }
            public virtual string CampaignManagedOrganizerName { get; set; }
            public virtual string Description { get; set; }
            public virtual DateTimeOffset StartDate { get; set; }
            public virtual DateTimeOffset EndDate { get; set; }

            public long TouchEveryProperty() => Id + ImageUrl.Length + Name.Length + CampaignName.Length + CampaignManagedOrganizerName.Length + Description.Length + StartDate.Ticks + EndDate.Ticks;
        }

        [Serializable]
        public class CampaignSummaryViewModel : IVerifiable
        {
            public virtual int Id { get; set; }
            public virtual string Title { get; set; }
            public virtual string Description { get; set; }
            public virtual string ImageUrl { get; set; }
            public virtual string OrganizationName { get; set; }
            public virtual string Headline { get; set; }

            public long TouchEveryProperty() => Id + Title.Length + Description.Length + ImageUrl.Length + OrganizationName.Length + Headline.Length;
        }
    }
}
