using AwalarBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwalarBot.Utilities
{
    public class Converters
    {

        public static long DurationStringToUnixTimestamp(string duration)
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(DurationStringToDateTime(duration));

            return dateTimeOffset.ToUnixTimeSeconds();
        }

        public static TimeSpan DurationStringToTimeSpan(string duration)
        {

            int days = ExtractValue(duration, "d");
            int hours = ExtractValue(duration, "h");
            int minutes = ExtractValue(duration, "m");

            TimeSpan timeSpan = new TimeSpan().Add(TimeSpan.FromDays(days)).Add(TimeSpan.FromHours(hours)).Add(TimeSpan.FromMinutes(minutes));

            return timeSpan;

        }

        public static DateTime DurationStringToDateTime(string duration)
        {

            int days = ExtractValue(duration, "d");
            int hours = ExtractValue(duration, "h");
            int minutes = ExtractValue(duration, "m");

            DateTime dateTime = DateTime.Now.Add(DurationStringToTimeSpan(duration));

            return dateTime;
        }

        public static string LongToDiscordTimestamp(long timestamp = 0)
        {
            if (timestamp == 0)
                return $"<t:{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}>";
            
            return $"<t:{timestamp}>";
        }

        private static int ExtractValue(string timeString, string unit)
        {
            int startIndex = timeString.IndexOf(unit);
            if (startIndex == -1)
            {
                return 0;
            }

            int endIndex = startIndex;
            while (endIndex < timeString.Length && char.IsDigit(timeString[endIndex]))
            {
                endIndex++;
            }

            string valueString = timeString.Substring(startIndex-1, endIndex);
            return int.Parse(valueString);
        }

    }
}
