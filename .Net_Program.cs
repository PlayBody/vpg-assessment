// #define USE_MYSQL

using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

#if USE_MYSQL
using SqlConnection = MySql.Data.MySqlClient.MySqlConnection;
using SqlCommand = MySql.Data.MySqlClient.MySqlCommand;
using SqlDataReader = MySql.Data.MySqlClient.MySqlDataReader;
#else
using System.Data.SqlClient;
#endif

namespace TestTemp
{
    internal class Program
    {
#if USE_MYSQL
        private static string dataConnectionString = "server=localhost;user=root;database=vpgdb;password=;port=3306";
#else
        private static string dataConnectionString = "Data Source=tcp:xxx.database.windows.net;Initial Catalog=mydb;Integrated Security=False;Persist Security Info=False;User ID=xxx;Password=xxxxxx;";
#endif

        public class CacheTemperatures
        {
            private static int cacheMaxSize = 5;
            private static int cacheIndex = 0;
            private static List<CacheTemperatures?> temperatureCacheList = new List<CacheTemperatures?>();
            private DateTime fromDateTime;
            private DateTime toDateTime;
            private List<int> temps;
            private int index;

            public List<int> Temps
            {
                get { return this.temps; }
            }

            public CacheTemperatures(DateTime fromDateTime, DateTime toDateTime, List<int> temps)
            {
                this.Update(fromDateTime, toDateTime, temps);
            }

            public bool Check(DateTime fromDateTime, DateTime toDateTime)
            {
                return this.fromDateTime == fromDateTime && this.toDateTime == toDateTime;
            }

            public void Update(DateTime fromDateTime, DateTime toDateTime, List<int> temps)
            {
                this.temps = temps;
                this.fromDateTime = fromDateTime;
                this.toDateTime = toDateTime;
                this.index = cacheIndex;
                cacheIndex++;
            }

            public void UpdateIndex()
            {
                this.index = cacheIndex;
                cacheIndex++;
            }

            public static void AddTemp(DateTime fromDateTime, DateTime toDateTime, List<int> temps)
            {
                int minCachePosition = -1;
                int minCacheIndex = int.MaxValue;
                if (temperatureCacheList.Count < cacheMaxSize)
                {
                    temperatureCacheList.Add(new CacheTemperatures(fromDateTime, toDateTime, temps));
                }
                else
                {
                    for (int position = 0; position < temperatureCacheList.Count; position++)
                    {
                        var cache = temperatureCacheList[position];
                        if (cache != null && cache.index < minCacheIndex)
                        {
                            minCacheIndex = cache.index;
                            minCachePosition = position;
                        }
                        else if (cache == null)
                        {
                            temperatureCacheList[position] = new CacheTemperatures(fromDateTime, toDateTime, temps);
                            return;
                        }
                    }
                    temperatureCacheList[minCachePosition]!.Update(fromDateTime, toDateTime, temps);
                }
            }

            public static CacheTemperatures? GetCache(DateTime fromDateTime, DateTime toDateTime)
            {
                foreach (var cache in temperatureCacheList)
                {
                    if (cache != null && cache.Check(fromDateTime, toDateTime))
                    {
                        return cache;
                    }
                }
                return null;
            }

            public static void Clear(DateTime dateTime)
            {
                for (int position = 0; position < temperatureCacheList.Count; position++)
                {
                    var cache = temperatureCacheList[position];
                    if (cache != null && cache.fromDateTime <= dateTime && cache.toDateTime >= dateTime)
                    {
                        temperatureCacheList[position] = null;
                    }
                }
            }
            
            public static void ClearAll()
            {
                cacheIndex = 0;
                temperatureCacheList.Clear();
            }
        }

        static List<int> ReadTemperatures(DateTime startDate, DateTime endDate)
        {
            CacheTemperatures? cache = CacheTemperatures.GetCache(startDate, endDate);
            if(cache != null) {
                cache.UpdateIndex();
                Console.WriteLine("Used cache for read temperature");
                return cache.Temps;
            }

            List<int> temps = new List<int>();
            using (SqlConnection connection = new SqlConnection(dataConnectionString)) {
                connection.Open();
                using (SqlCommand command = new SqlCommand($"SELECT id, sensor, name, temp, date, guid, remarks FROM TempReadings WHERE date >= '{startDate.ToString("yyyy-MM-dd HH:mm:ss.fff")}' AND date <= '{endDate.ToString("yyyy-MM-dd HH:mm:ss.fff")}'", connection)){
                    using (SqlDataReader reader = command.ExecuteReader()) {
                        while(reader.Read()) {
                            int temp = 0;

                            if(int.TryParse(reader["temp"].ToString(), out temp)){
                                temps.Add(temp);
                            } else {
                                Console.WriteLine("Failed to parse temperature: " + reader["temp"].ToString());
                            }
                        }
                    }
                }
            }
            CacheTemperatures.AddTemp(startDate, endDate, temps);
            return temps;
        }

        static Tuple<bool, double, double, int, int, double> CalculateStatistics(DateTime startDate, DateTime endDate)
        {
            List<int> temps = ReadTemperatures(startDate, endDate);
            bool success = temps.Count > 0;
            double mean_temp = temps.Count > 0 ? temps.Average() : 0;
            double median_temp = temps.Count > 0 ? temps.OrderBy(t => t).Skip(temps.Count / 2).FirstOrDefault() : 0;
            int min_temp = temps.Count > 0 ? temps.Min() : 0;
            int max_temp = temps.Count > 0 ? temps.Max() : 0;
            double std_dev = temps.Count > 0 ? Math.Sqrt(temps.Select(t => Math.Pow(t - mean_temp, 2)).Sum() / temps.Count) : 0;
            return Tuple.Create(success, mean_temp, median_temp, min_temp, max_temp, std_dev);
        }

        static void AddTemperatureReading(int sensor, string name, int temp, Guid guid, string remarks)
        {
            DateTime now = DateTime.Now;
            using (SqlConnection connection = new SqlConnection(dataConnectionString)){
                connection.Open();
                using (SqlCommand command = new SqlCommand("INSERT INTO TempReadings (sensor, name, temp, date, guid, remarks) VALUES (@sensor, @name, @temp, @date, @guid, @remarks)", connection)){
                    command.Parameters.AddWithValue("@sensor", sensor);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@temp", temp);
                    command.Parameters.AddWithValue("@date", now);
                    command.Parameters.AddWithValue("@guid", guid);
                    command.Parameters.AddWithValue("@remarks", remarks);
                    command.ExecuteNonQuery();
                }
            }
            CacheTemperatures.Clear(now);
        }

        static void Main(string[] args)
        {
            DateTime start = DateTime.Now.AddDays(-1);
            DateTime end = DateTime.Now;

            // Similar to Python code, AddTemperatureReading is called in loop
            Random rand = new Random();
            for (int i = 0; i < 5; i++)
            {
                AddTemperatureReading(rand.Next(i, 100), $"Sensor {DateTime.Now.Ticks}", rand.Next(i, 100), Guid.NewGuid(), $"Remarks {DateTime.Now.Ticks}");
            }

            Tuple<bool, double, double, int, int, double> statistics = CalculateStatistics(start, end);
            if(statistics.Item1 == false) {
              Console.WriteLine("Empty Temperature Table: Error Here!");
              return;
            }
            // Debug.Assert(statistics == null, "Empty Temperature Table: Error Here!");
            
            double mean = statistics.Item2;
            double median = statistics.Item3;
            int minimum = statistics.Item4;
            int maximum = statistics.Item5;
            double std_dev = statistics.Item6;

            Console.WriteLine($"mean: {mean}, median: {median}, minimum: {minimum}, maximum: {maximum}, std_dev: {std_dev}");

            List<int> temps = ReadTemperatures(start, end);
            for (int i = 0; i< temps.Count; i++)
                Console.WriteLine($"{i}th - {temps[i]}");
        }
    }
}