## Html

Doctype Declaration:
> The new HTML code begins with the `<!DOCTYPE html>` declaration. This is an instruction to the web browser about the version of HTML the page is written in. It helps the browser to render the page correctly.

HTML Structure: 
> The HTML code is now organized into sections with the `<html>`, `<head>`, and `<body>` tags, following the standard structure of HTML documents. This helps to improve the readability and maintainability of the HTML code.

Styling: 
> The new HTML code uses internal CSS for styling, which is placed within the `<style>` tags in the `<head>` section.

Table Styling: 
> The styling of the table has been significantly improved.

Button Styling: 
> The 'remove' buttons have been styled for a better user experience.

Animation: 
> The 'Fatal' status blinking effect is now achieved using a CSS keyframes animation.

JavaScript Changes: 
> The JavaScript code has been updated for better robustness and maintainability.

## .Net code
Database Connection: 
> Fixed a memory leak issue by ensuring the database connection was properly closed after use.

Data Reading: 
> Updated the code to only read and store data within the specified date range, preventing unnecessary data loading.

Data Parsing: 
> Fixed crashes that would occur if the temperature data from the database wasn't a valid integer. Now, invalid data is properly handled and a warning message is outputted.

## Python code
Data Reading: 
> Fixed the issue where the code was attempting to read all data from the database at once, which could lead to performance issues for large data sets. The code now only reads and stores data within the specified date range.

Data Parsing: 
> Fixed crashes that would occur if the temperature data from the database wasn't a valid integer. Now, invalid data is properly handled and a warning message is printed out.

Error Handling: 
> Fixed the issue where the code would crash if no temperature data was available within the specified date range. Now, the code checks if the data is empty and exits early instead of attempting to calculate statistics on empty data.

## Common Improved part (.Net, Python)
Database Connection: 
> Added switch logic between PostgreSQL/SQL Server and MySQL (To test it using my local db configuration)

Data Reading: 
> The ReadTemperatures function was modified to cache the temperature readings, improving performance by avoiding unnecessary database queries.

Data Calculations: 
> A new function CalculateStatistics was introduced to calculate the statistical measures (mean, median, minimum, maximum, standard deviation) of the temperature readings.

Data Insertion: 
> The AddTemperatureReading function was added to insert new temperature data into the database. After inserting new data, the temperature cache is cleared to ensure the updated data is used in subsequent operations.

Test Data Generation: 
> Random temperature data is generated and inserted into the database using the AddTemperatureReading function. This data is then used to test the CalculateStatistics and ReadTemperatures functions.

Error Handling: 
> The code now handles the scenario where the temperature table might be empty.