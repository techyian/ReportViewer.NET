using Microsoft.VisualBasic;
using ReportViewer.NET.Parsers.DateAndTime;
using System;
using System.Globalization;

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

        public static int ParseDatePartInterval(this DateTime dtt, DateInterval dateInterval, FirstDayOfWeek fdow, FirstWeekOfYear fwoy)
        {            
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            Calendar calendar = cultureInfo.Calendar;
                        
            /*cultureInfo.DateTimeFormat.CalendarWeekRule = FirstWeekOfYearToCalendarWeekRule(fwoy);
            cultureInfo.DateTimeFormat.FirstDayOfWeek = FirstDayOfWeekToDayOfWeek(fdow);*/

            switch (dateInterval)
            {
                case DateInterval.Year:
                    return calendar.GetYear(dtt);
                case DateInterval.Quarter:
                    return Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(calendar.GetMonth(dtt)) / 4));
                case DateInterval.Month:
                    return calendar.GetMonth(dtt);
                case DateInterval.DayOfYear:
                case DateInterval.Day:
                case DateInterval.Weekday:                    
                    return calendar.GetDayOfMonth(dtt);
                case DateInterval.WeekOfYear:
                    return calendar.GetWeekOfYear(dtt, FirstWeekOfYearToCalendarWeekRule(fwoy), FirstDayOfWeekToDayOfWeek(fdow));
                case DateInterval.Hour:
                    return calendar.GetHour(dtt);
                case DateInterval.Minute:
                    return calendar.GetMinute(dtt);
                case DateInterval.Second:
                    return calendar.GetSecond(dtt);
            }

            return 0;
        }

        public static int ParseDatePartShortString(this DateTime dtt, string dateInterval, FirstDayOfWeek fdow, FirstWeekOfYear fwoy)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            Calendar calendar = cultureInfo.Calendar;

            switch (dateInterval.ToLower())
            {
                case "yyyy": // Year
                    return calendar.GetYear(dtt);
                case "q": // Quarter
                    return Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(calendar.GetMonth(dtt)) / 4));
                case "m": // Month
                    return calendar.GetMonth(dtt);
                case "y": // Day of year
                case "w": // Weekday
                case "d": // Day
                          // How are these intervals different - confused?
                    return calendar.GetDayOfMonth(dtt);
                case "ww": // Week
                    return calendar.GetWeekOfYear(dtt, FirstWeekOfYearToCalendarWeekRule(fwoy), FirstDayOfWeekToDayOfWeek(fdow));
                case "h": // Hour
                    return calendar.GetHour(dtt);
                case "n": // Minute
                    return calendar.GetMinute(dtt);
                case "s": // Second
                    return calendar.GetSecond(dtt);
            }

            return 0;
        }

        public static string FormatDateTime(this DateTime dtt, DateFormat format)
        {
            switch (format)
            {
                case DateFormat.GeneralDate:
                    return dtt.ToString("d");                    
                case DateFormat.LongDate:
                    return dtt.ToLongDateString();                    
                case DateFormat.ShortDate:
                    return dtt.ToShortDateString();                    
                case DateFormat.LongTime:
                    return dtt.ToLongTimeString();                    
                case DateFormat.ShortTime:
                    return dtt.ToShortTimeString();
                default:
                    return dtt.ToString("d");
            }            
        }

        private static DayOfWeek FirstDayOfWeekToDayOfWeek(FirstDayOfWeek fdow)
        {
            switch (fdow)
            {
                case FirstDayOfWeek.Monday:
                    return DayOfWeek.Monday;
                case FirstDayOfWeek.Tuesday:
                    return DayOfWeek.Tuesday;
                case FirstDayOfWeek.Wednesday:
                    return DayOfWeek.Wednesday;
                case FirstDayOfWeek.Thursday:
                    return DayOfWeek.Thursday;
                case FirstDayOfWeek.Friday:
                    return DayOfWeek.Friday;
                case FirstDayOfWeek.Saturday:
                    return DayOfWeek.Saturday;
                case FirstDayOfWeek.Sunday:
                    return DayOfWeek.Sunday;
                default:
                    CultureInfo cultureInfo = CultureInfo.CurrentCulture;                    
                    return cultureInfo.DateTimeFormat.FirstDayOfWeek;
            }
        }

        private static CalendarWeekRule FirstWeekOfYearToCalendarWeekRule(FirstWeekOfYear fwoy)
        {
            switch (fwoy)
            {
                case FirstWeekOfYear.Jan1:
                    return CalendarWeekRule.FirstDay;
                case FirstWeekOfYear.FirstFourDays:
                    return CalendarWeekRule.FirstFourDayWeek;
                case FirstWeekOfYear.FirstFullWeek:
                    return CalendarWeekRule.FirstFullWeek;
                default:
                    CultureInfo cultureInfo = CultureInfo.CurrentCulture;
                    return cultureInfo.DateTimeFormat.CalendarWeekRule;
            }
        }
    }
}
