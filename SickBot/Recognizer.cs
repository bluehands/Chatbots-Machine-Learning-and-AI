using System;
using System.Collections.Generic;
using Microsoft.Recognizers.Text.DateTime;
using Constants = Microsoft.Recognizers.Text.DataTypes.TimexExpression.Constants;
using System.Linq;

namespace SickBot
{
    public static class Recognizer
    {
        public static bool TryGetDate(string text, string culture, out DateTime date)
        {
            var results = DateTimeRecognizer.RecognizeDateTime(text, culture);
            foreach (var result in results)
            {
                var values = (List<Dictionary<string, string>>)result.Resolution["values"];
                var type = GetTypeProperty(values);
                if (type.Equals(Constants.TimexTypes.DateRange) || type.Equals(Constants.TimexTypes.DateTimeRange))
                {
                    var end = GetEndProperty(values);
                    if (DateTime.TryParse(end, out date))
                    {
                        date = date.AddDays(-1);
                        return true;
                    }
                }

                if (type.Equals(Constants.TimexTypes.Date) || type.Equals(Constants.TimexTypes.DateTime))
                {
                    var item = GetLastValueProperty(values);
                    if (DateTime.TryParse(item, out date))
                    {
                        return true;
                    }
                }
            }

            date = DateTime.MinValue;
            return false;
        }
        private static string GetTypeProperty(List<Dictionary<string, string>> values)
        {
            foreach (var value in values)
            {
                // We are interested in the distinct set of TIMEX expressions.
                if (value.TryGetValue("type", out var type))
                {
                    return type;
                }
            }
            return null;
        }
        private static string GetEndProperty(List<Dictionary<string, string>> values)
        {
            foreach (var value in values)
            {
                // We are interested in the distinct set of TIMEX expressions.
                if (value.TryGetValue("end", out var end))
                {
                    return end;
                }
            }
            return null;
        }
        private static string GetValueProperty(List<Dictionary<string, string>> values)
        {
            foreach (var value in values)
            {
                // We are interested in the distinct set of TIMEX expressions.
                if (value.TryGetValue("value", out var item))
                {
                    return item;
                }
            }
            return null;
        }
        private static string GetLastValueProperty(List<Dictionary<string, string>> values)
        {
            var items = new List<string>();
            foreach (var value in values)
            {
                // We are interested in the distinct set of TIMEX expressions.
                if (value.TryGetValue("value", out var item))
                {
                    items.Add( item);
                }
            }

            return items.LastOrDefault();
        }
    }
}