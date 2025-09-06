﻿using DiscordBot.Interfaces;
using DiscordBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace DiscordBot.Services
{
    public class OpenWeatherMapService(IHttpService httpService, IConfiguration configuration) : IOpenWeatherMapService
    {
        /// <summary>
        ///     Api Key to access OpenWeatherMap Api
        /// </summary>
        private string? _openWeatherMapApiKey;

        /// <summary>
        ///     Url to the OpenWeatherMap Api
        /// </summary>
        private string? _openWeatherMapUrl;

        public async Task<(bool Success, string Message, WeatherData? weatherData)> GetWeatherAsync(string city)
        {
            // Retrieve the url and the apikey from the configuration
            _openWeatherMapUrl = configuration["OpenWeatherMap:ApiUrl"] ?? string.Empty;
            _openWeatherMapApiKey = configuration["OpenWeatherMap:ApiKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(_openWeatherMapApiKey) || string.IsNullOrEmpty(_openWeatherMapUrl))
            {
                const string errorMessage =
                    "No OpenWeatherMap Api Key/Url was provided, please contact the Developer to add a valid Api Key/Url!";
                Program.Log($"{nameof(GetWeatherAsync)}: " + errorMessage, LogLevel.Error);
                return (false, errorMessage,
                    null);
            }

            HttpResponse response = await httpService.GetResponseFromUrl(
                $"{_openWeatherMapUrl}{Uri.EscapeDataString(city)}&units=metric&appid={_openWeatherMapApiKey}",
                Method.Post,
                $"{nameof(GetWeatherAsync)}: Failed to fetch weather data for city '{city}'.");

            if (!response.IsSuccessStatusCode)
            {
                return (false, response.Content ?? "", null);
            }

            JObject json = JObject.Parse(response.Content ?? "");

            WeatherData weather = new()
            {
                City = json["name"]?.Value<string>(),
                Description = json["weather"]?[0]?["description"]?.Value<string>(),
                Temperature = json["main"]?["temp"]?.Value<double>(),
                Humidity = json["main"]?["humidity"]?.Value<int>(),
                WindSpeed = json["wind"]?["speed"]?.Value<double>()
            };

            string message =
                $"In {weather.City}, the weather currently: {weather.Description}. The temperature is {weather.Temperature:F2}°C. " +
                $"The humidity is {weather.Humidity}% and the wind speed is {weather.WindSpeed} m/s.";

            Program.Log($"{nameof(GetWeatherAsync)}: Weather data fetched successfully. Response: " + message);

            return (true, message, weather);
        }
    }
}