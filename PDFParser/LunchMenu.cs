using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace PDFParser
{
    public static class DateTimeExtension
    {
        public static bool IsExpired(this DateTime experitaion)
        {
            return DateTime.Now.Date > experitaion;
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.Fields)]
    public class LunchMenu : List<Day>
    {
        public DateTime ExpirationDate { get; set; }

        public LunchMenu()
        {
            DateTime GetNextSunday()
            {
                var now = DateTime.Now;
                var today = (int)DateTime.Now.DayOfWeek;
                var sunday = 7;

                return now.AddDays((sunday - today));
            }

            ExpirationDate = GetNextSunday();
        }
    }
    public class Day
    {
        public string DayName { get; set; }
        public List<FoodStation> FoodStations { get; set; }

        public Day()
        {
            FoodStations = new List<FoodStation>();
        }

        public override string ToString()
        {
            var stations = this.FoodStations
                .Where(s => s.Price != 0)
                .Select(p => $"{p.FoodName} ${p.Price}");

            return string.Join("\n", stations);
        }
    }

    public class FoodStation
    {
        public string StationName { get; set; }
        public string FoodName { get; set; }
        public Double Price { get; set; }
    }
}
