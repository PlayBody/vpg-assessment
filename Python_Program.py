from django.conf import settings
from datetime import datetime, timedelta
import psycopg2
import statistics
import uuid
import random
import mysql.connector

# switch mysql/psycopg2
use_mysql = False

# This value will be used for caching
temperature_cache_list = []

def ConnectionDB():
    global use_mysql
    if use_mysql:
        connection = mysql.connector.connect(host="localhost", user="root", password="", database="vpgdb", port=3306)
    else:
        db = settings.DATABASES['default']
        connection = psycopg2.connect(database=db['NAME'], user=db['USER'], password=db['PASSWORD'], host=db['HOST'], port=db['PORT'])
    return connection, connection.cursor()

def ReadTemperatures(from_datetime: datetime, to_datetime: datetime):
    global temperature_cache_list
    if temperature_cache_list:
        print("Used cache for read temperature")
        return temperature_cache_list
    temperature_list = []
    con, cur = ConnectionDB()
    sql = "SELECT id, sensor, name, temp, date, guid, remarks FROM TempReadings"
    cur.execute(sql)
    # id = 0, sensor = 1, name = 2, temp = 3, date = 4, guid = 5, remarks = 6
    while True:
        data = cur.fetchone()
        if data is None:
            break
        if data[4] <= to_datetime and data[4] >= from_datetime:
            temp = 0
            try:
                temp = int(data[3])
                temperature_list.append(temp)
            except:
                print("Failed to parse temperature:", data[3])
    con.close()
    temperature_cache_list = temperature_list
    return temperature_list

def CalculateStatistics(from_datetime: datetime, to_datetime: datetime):
    temperature_list = ReadTemperatures(from_datetime, to_datetime)
    if not temperature_list:
        return None
    mean = statistics.mean(temperature_list)
    median = statistics.median(temperature_list)
    min_temp = min(temperature_list)
    max_temp = max(temperature_list)
    std_dev = statistics.stdev(temperature_list)
    return [mean, median, min_temp, max_temp, std_dev]

def AddTemperatureReading(sensor: int, name: str, temp: int, guid: uuid.UUID, remarks: str):
    global temperature_cache_list
    con, cur = ConnectionDB()
    sql = "INSERT INTO TempReadings (sensor, name, temp, date, guid, remarks) VALUES (%s, %s, %s, %s, %s, %s)"
    cur.execute(sql, (sensor, name, temp, datetime.now(), str(guid), remarks))
    con.commit()
    con.close()
    temperature_cache_list = []


# some test cases
start = datetime.now() - timedelta(days=1)
end = datetime.now()
for i in range(0, 5):
    AddTemperatureReading(random.randint(i, 100), f"Sensor {datetime.now().microsecond}", random.randint(i, 100), uuid.uuid4(), f"Remarks {datetime.now().microsecond}")
results = CalculateStatistics(start, end)
assert results is not None, "Empty Temperature Table: Error Here!"
mean, median, minimum, maximum, std_dev = results
print(f"mean: {mean:.2f}, median: {median}, minium: {minimum}, maximum: {maximum}, std_dev: {std_dev:.2f}")

temperature_list = ReadTemperatures(start, end) # it will return cached temperature list
for index, temperature in enumerate(temperature_list):
    print(f"{index}th - {temperature}")