// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace MCPServer.Tools;

/// <summary>
/// A collection of utility methods for working with weather.
/// </summary>
internal sealed class WeatherUtils
{
    /// <summary>
    /// Gets the current weather for the specified city.
    /// </summary>
    /// <param name="cityName">The name of the city.</param>
    /// <param name="currentDateTimeInUtc">The current date time in UTC.</param>
    /// <param name="logger">Logger for error and audit logging.</param>
    /// <returns>The current weather for the specified city.</returns>
    [KernelFunction, Description("Gets the current weather for the specified city and specified date time.")]
    public static string GetWeatherForCity(string cityName, string currentDateTimeInUtc, [FromKernelServices] ILogger<WeatherUtils> logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                logger.LogWarning("Weather request attempted with empty or null city name");
                return "Weather information unavailable: City name is required";
            }

            logger.LogInformation("Getting weather for city: {CityName} at {DateTime}", cityName, currentDateTimeInUtc);

            var weather = cityName switch
            {
                "Boston" => "61 and rainy",
                "London" => "55 and cloudy",
                "Miami" => "80 and sunny",
                "Paris" => "60 and rainy",
                "Tokyo" => "50 and sunny",
                "Sydney" => "75 and sunny",
                "Tel Aviv" => "80 and sunny",
                _ => "31 and snowing",
            };

            logger.LogDebug("Weather information retrieved for {CityName}: {Weather}", cityName, weather);
            return weather;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get weather for city: {CityName}", cityName);
            return "Weather information unavailable: An error occurred while retrieving weather data";
        }
    }
}
