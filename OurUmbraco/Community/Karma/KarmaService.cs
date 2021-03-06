﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Examine;
using OurUmbraco.Community.Controllers;
using OurUmbraco.Our.Api;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Web;

namespace OurUmbraco.Community.Karma
{
    public class KarmaService
    {
        public KarmaStatistics GetKarmaStatistics()
        {
            var karmaRecentStatistics = UmbracoContext.Current.Application.ApplicationCache.RuntimeCache.GetCacheItem<PeopleData>("OurRecentKarmaStatistics",
                () =>
                {
                    var lastweekStart = GetLastWeekStartDate();
                    var karmaRecent = Our.Api.StatisticsController.GetPeopleData(lastweekStart, DateTime.Now.AddYears(-1));

                    foreach (var period in karmaRecent.MostActiveDateRange)
                    {
                        foreach (var person in period.MostActive)
                        {
                            var criteria = ExamineManager.Instance.SearchProviderCollection["InternalMemberSearcher"]
                                .CreateSearchCriteria();
                            var filter = criteria.RawQuery("__NodeId: " + person.MemberId);
                            var searchResult = ExamineManager.Instance
                                .SearchProviderCollection["InternalMemberSearcher"].Search(filter).FirstOrDefault();
                            if (searchResult == null)
                                continue;

                            int totalKarma;
                            if (int.TryParse(searchResult.Fields["reputationTotal"], out totalKarma))
                                person.TotalKarma = totalKarma;
                        }
                    }

                    return karmaRecent;

                }, TimeSpan.FromMinutes(15));

            var karmaYearStatistics = UmbracoContext.Current.Application.ApplicationCache.RuntimeCache.GetCacheItem<PeopleData>("OurYearKarmaStatistics",
                () =>
                {
                    // Yearly karma counts from May 1st until May 1st each year. If the date in this year is before May 1st, shift back an extra year
                    var yearShift = 0;
                    if (DateTime.Now > new DateTime(DateTime.Now.Year, 5, 1))
                        yearShift = 1;

                    var karmaYear = Our.Api.StatisticsController.GetPeopleData(
                        new DateTime(DateTime.Now.Year - 1 - yearShift, 5, 1),
                        new DateTime(DateTime.Now.Year - yearShift, 5, 1));

                    foreach (var period in karmaYear.MostActiveDateRange)
                    {
                        foreach (var person in period.MostActive)
                        {
                            var criteria = ExamineManager.Instance.SearchProviderCollection["InternalMemberSearcher"].CreateSearchCriteria();
                            var filter = criteria.RawQuery("__NodeId: " + person.MemberId);
                            var searchResult = ExamineManager.Instance.SearchProviderCollection["InternalMemberSearcher"].Search(filter).FirstOrDefault();
                            if (searchResult == null)
                                continue;

                            int totalKarma;
                            if (int.TryParse(searchResult.Fields["reputationTotal"], out totalKarma))
                                person.TotalKarma = totalKarma;
                        }
                    }

                    return karmaYear;

                }, TimeSpan.FromHours(24));


            var karmaStatistics = new KarmaStatistics
            {
                KarmaRecent = karmaRecentStatistics,
                KarmaYear = karmaYearStatistics
            };

            return karmaStatistics;
        }

        private static DateTime GetLastWeekStartDate()
        {
            DateTime date = DateTime.Now.AddYears(-1).AddDays(-7);
            while (date.DayOfWeek != DayOfWeek.Monday)
            {
                date = date.AddDays(-1);
            }

            return date;
        }
    }
}
