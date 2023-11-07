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
        private static List<int> temperatureCache = new List<int>();

        static List<int> ReadTemperatures(DateTime startDate, DateTime endDate)
        {
            if (temperatureCache.Count > 0)
            {
                Console.WriteLine("Used cache for read temperature");
                return temperatureCache;
            }

            List<int> temps = new List<int>();
            using (SqlConnection connection = new SqlConnection(dataConnectionString)) {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT id, sensor, name, temp, date, guid, remarks FROM TempReadings", connection)){
                    using (SqlDataReader reader = command.ExecuteReader()) {
                        while(reader.Read()) {
                            DateTime date = (DateTime)reader["date"];
                            if(date >= startDate && date <= endDate){
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
            }
            temperatureCache = temps;
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
            using (SqlConnection connection = new SqlConnection(dataConnectionString)){
                connection.Open();
                using (SqlCommand command = new SqlCommand("INSERT INTO TempReadings (sensor, name, temp, date, guid, remarks) VALUES (@sensor, @name, @temp, @date, @guid, @remarks)", connection)){
                    command.Parameters.AddWithValue("@sensor", sensor);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@temp", temp);
                    command.Parameters.AddWithValue("@date", DateTime.Now);
                    command.Parameters.AddWithValue("@guid", guid);
                    command.Parameters.AddWithValue("@remarks", remarks);
                    command.ExecuteNonQuery();
                }
            }
            temperatureCache.Clear();
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