using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DrivingSchool.AI
{
    public class AIRecommendation
    {
        public double Score { get; set; }
        public string Level { get; set; }
        public string Recommendation { get; set; }
        public bool CanExam { get; set; }
        public List<string> Suggestions { get; set; }
    }

    public class StudentDataForAI
    {
        public int CompletedLessons { get; set; }
        public int TotalRequiredLessons { get; set; }
        public int MissedLessons { get; set; }
        public int MaxGapDays { get; set; }
        public double AvgGapDays { get; set; }
        public int LastGapDays { get; set; }      // <-- ДОБАВЛЕНО
        public int TheoryAttempts { get; set; }
        public int PracticeAttempts { get; set; }
    }

    public class AIClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public AIClient()
        {
            _httpClient = new HttpClient();
            _apiUrl = "https://functions.yandexcloud.net/d4e7u4e719lsupkkl0go";
        }

        public async Task<AIRecommendation> GetRecommendationAsync(StudentDataForAI data)
        {
            try
            {
                var request = new { data = data };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    Culture = System.Globalization.CultureInfo.InvariantCulture
                };

                return JsonConvert.DeserializeObject<AIRecommendation>(responseJson, settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI Error: {ex.Message}");
                return new AIRecommendation
                {
                    Score = 0,
                    Level = "Нет данных",
                    Recommendation = "Сервис ИИ недоступен",
                    CanExam = false,
                    Suggestions = new List<string>()
                };
            }
        }
    }
}