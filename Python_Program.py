from django.conf import settings
from datetime import datetime, timedelta
import psycopg2
import statistics
import uuid
import random
import mysql.connector

# switch mysql/psycopg2
use_mysql = False
# use_mysql = True

class CacheTemperatures:
    cache_max_size = 5
    cache_index = 0
    temperature_cache_list = []
    def __init__(self, from_datetime: datetime, to_datetime: datetime, temps) -> None:
        self.update(from_datetime, to_datetime, temps)
    def check(self, from_datetime: datetime, to_datetime: datetime):
        if self.from_date == from_datetime and self.to_date == to_datetime:
            return True
        return False
    def update(self, from_datetime: datetime, to_datetime: datetime, temps):
        self.temps = temps
        self.from_date = from_datetime
        self.to_date = to_datetime
        self.index = CacheTemperatures.cache_index
        CacheTemperatures.cache_index += 1
    def update_index(self):
        self.index = CacheTemperatures.cache_index
        CacheTemperatures.cache_index += 1
    @classmethod
    def add_temp(cls, from_datetime: datetime, to_datetime: datetime, temps):
        min_cache_position = -1
        min_cache_index = 1e9
        if len(cls.temperature_cache_list) < cls.cache_max_size:
            cls.temperature_cache_list.append(CacheTemperatures(from_datetime, to_datetime, temps))
        else:
            for (postion, cache) in enumerate(cls.temperature_cache_list):
                if cache is not None and cache.cache_index < min_cache_index:
                    min_cache_index = cache.cache_index
                    min_cache_position = postion
            cls.temperature_cache_list[min_cache_position].update(from_datetime, to_datetime, temps)
    @classmethod
    def get_cache(cls, from_datetime: datetime, to_datetime: datetime):
        for cache in cls.temperature_cache_list:
            if cache is not None and cache.check(from_datetime, to_datetime):
                return cache
        return None
    @classmethod
    def clear(cls, dt:datetime):
        for (postion, cache) in enumerate(cls.temperature_cache_list):
            if cache is not None and cache.from_date <= dt and cache.to_date >= dt:
                cls.temperature_cache_list[postion] = None
    @classmethod
    def clear_all(cls):
        cls.cache_index = 0
        cls.temperature_cache_list = []

def ConnectionDB():
    global use_mysql
    if use_mysql:
        connection = mysql.connector.connect(host="localhost", user="root", password="", database="vpgdb", port=3306)
    else:
        db = settings.DATABASES['default']
        connection = psycopg2.connect(database=db['NAME'], user=db['USER'], password=db['PASSWORD'], host=db['HOST'], port=db['PORT'])
    return connection, connection.cursor()

def ReadTemperatures(from_datetime: datetime, to_datetime: datetime):
    cache = CacheTemperatures.get_cache(from_datetime, to_datetime)
    if cache is not None:
        cache.update_index()
        print("Used cache for read temperature")
        return cache.temps
    temperature_list = []
    con, cur = ConnectionDB()
    sql = f"SELECT id, sensor, name, temp, date, guid, remarks FROM TempReadings WHERE date >= '{str(from_datetime)}' AND date <= '{str(to_datetime)}'"
    cur.execute(sql)
    # id = 0, sensor = 1, name = 2, temp = 3, date = 4, guid = 5, remarks = 6
    while True:
        data = cur.fetchone()
        if data is None:
            break
        temp = 0
        try:
            temp = int(data[3])
            temperature_list.append(temp)
        except:
            print("Failed to parse temperature:", data[3])
    con.close()
    CacheTemperatures.add_temp(from_datetime, to_datetime, temperature_list)
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
    con, cur = ConnectionDB()
    now = datetime.now()
    sql = "INSERT INTO TempReadings (sensor, name, temp, date, guid, remarks) VALUES (%s, %s, %s, %s, %s, %s)"
    cur.execute(sql, (sensor, name, temp, datetime.now(), str(guid), remarks))
    con.commit()
    con.close()
    CacheTemperatures.clear(now)

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