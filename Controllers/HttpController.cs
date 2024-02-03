﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TamagotchiBot.Controllers
{
    internal static class HttpController
    {
        private static string TOKEN;
        private static string ACCESS_KEY;
        private static string FILEPATH;

        internal static async Task<(HttpStatusCode Status, string Content)> StartBotStatChecking(string token, string accessKey, string filePath)
        {
            TOKEN = token;
            ACCESS_KEY = accessKey;
            FILEPATH = filePath;

            return await SendRequest();
        }
        internal static async Task<(HttpStatusCode Status, string Content)> StatusCheck(string checkId)
        {
            string apiUrl = $"https://api.botstat.io/status/{checkId}";

            using var httpClient = new HttpClient();

            // Send the request
            using var response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Status checked successfully.");
                return (response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            else
            {
                Console.WriteLine($"Error: {(int)response.StatusCode} - {response.ReasonPhrase}");
                return (response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }
        static async Task<(HttpStatusCode, string)> SendRequest()
        {
            string apiUrl = $"https://api.botstat.io/create/{TOKEN}/{ACCESS_KEY}";

            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();

            // Add JSON payload
            var jsonPayload = "{\"notify_id\": \"401250312\"}";
            var jsonContent = new StringContent(jsonPayload);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Add(jsonContent, "query");

            // Add file attachment
            byte[] fileBytes = System.IO.File.ReadAllBytes(FILEPATH);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", "blya.json");

            // Send the request
            using var response = await httpClient.PostAsync(apiUrl, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Request sent successfully.");
                return (response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            else
            {
                Console.WriteLine($"Error: {(int)response.StatusCode} - {response.ReasonPhrase}");
                return (response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }
    }
}