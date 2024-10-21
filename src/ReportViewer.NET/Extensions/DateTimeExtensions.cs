using Microsoft.VisualBasic;
using ReportViewer.NET.Parsers.DateAndTime;
using System;

namespace ReportViewer.NET.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ParseDateInterval(this DateTime dtt, DateInterval dateInterval, int increment)
        {
            switch (dateInterval)
            {
                case DateInterval.Year:
                    return dtt.AddYears(increment);
                case DateInterval.Quarter:
                    return dtt.AddMonths(4 * increment);
                case DateInterval.Month:
                    return dtt.AddMonths(increment);
                case DateInterval.DayOfYear:
                case DateInterval.Day:
                case DateInterval.Weekday:
                    return dtt.AddDays(increment);
                case DateInterval.WeekOfYear:
                    return dtt.AddDays(7 * increment);
                case DateInterval.Hour:
                    return dtt.AddHours(increment);
                case DateInterval.Minute:
                    return dtt.AddMinutes(increment);
                case DateInterval.Second:
                    return dtt.AddSeconds(increment);
            }

            return dtt;
        }

        public static DateTime ParseDateIntervalShortString(this DateTime dtt, string dateInterval, int increment) 
        {
            // Order switch as per https://support.microsoft.com/en-gb/office/dateadd-function-63befdf6-1ffa-4357-9424-61e8c57afc19
            switch (dateInterval.ToLower())
            {
                case "yyyy": // Year
                    dtt = dtt.AddYears(increment);
                    break;
                case "q": // Quarter
                    dtt = dtt.AddMonths(4 * increment);
                    break;
                case "m": // Month
                    dtt = dtt.AddMonths(increment);
                    break;
                case "y": // Day of year
                case "w": // Weekday
                case "d": // Day
                          // How are these intervals different - confused?
                    dtt = dtt.AddDays(increment);
                    break;
                case "ww": // Week
                    dtt = dtt.AddDays(7 * increment);
                    break;
                case "h": // Hour
                    dtt = dtt.AddHours(increment);
                    break;
                case "n": // Minute
                    dtt = dtt.AddMinutes(increment);
                    break;
                case "s": // Second
                    dtt = dtt.AddSeconds(increment);
                    break;
            }

            return dtt;
        }

        public static long ParseDateDiff(this DateTime dtt, DateInterval dateInterval, DateTime compareTo)
        {
            if (compareTo < dtt)
            {
                var sub = dtt;
                dtt = compareTo;
                compareTo = sub;
            }

            var span = DateTimeSpan.CompareDates(compareTo, dtt);

            switch (dateInterval)
            {
                case DateInterval.Year:                    
                    return span.Years;
                case DateInterval.Quarter:
                    return (span.Years * 12) + span.Months / 4;
                case DateInterval.Month:
                    return (span.Years * 12) + span.Months;
                case DateInterval.DayOfYear:
                case DateInterval.Day:
                case DateInterval.Weekday:
                    return (compareTo - dtt).Days;
                case DateInterval.WeekOfYear:
                    return (compareTo - dtt).Days / 7;
                case DateInterval.Hour:
                    return Convert.ToInt64((compareTo - dtt).TotalHours);
                case DateInterval.Minute:
                    return Convert.ToInt64((compareTo - dtt).TotalMinutes);
                case DateInterval.Second:
                    return Convert.ToInt64((compareTo - dtt).TotalSeconds);
            }

            return 0;
        }

        public static long ParseDateDiffShortString(this DateTime dtt, string dateInterval, DateTime compareTo)
        {
            if (compareTo < dtt)
            {
                var sub = dtt;
                dtt = compareTo;
                compareTo = sub;
            }

            var span = DateTimeSpan.CompareDates(compareTo, dtt);

            switch (dateInterval.ToLower())
            {
                case "yyyy": // Year
                    return span.Years;
                case "q": // Quarter
                    return span.Months / 4;
                case "m": // Month
                    return span.Months;
                case "y": // Day of year
                case "w": // Weekday
                case "d": // Day
                          // How are these intervals different - confused?
                    return (compareTo - dtt).Days;
                case "ww": // Week
                    return (compareTo - dtt).Days / 7;
                case "h": // Hour
                    return Convert.ToInt64((compareTo - dtt).TotalHours);
                case "n": // Minute
                    return Convert.ToInt64((compareTo - dtt).TotalMinutes);
                case "s": // Second
                    return Convert.ToInt64((compareTo - dtt).TotalSeconds);
            }

            return 0;
        }
    }
}
