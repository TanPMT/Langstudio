using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using backend.Data;
using backend.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class WritingService : IWritingService
{
    private readonly MongoDbContext _mongoContext;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<WritingService> _logger;

    public WritingService(MongoDbContext mongoContext, IGeminiService geminiService, ILogger<WritingService> logger)
    {
        _mongoContext = mongoContext;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<ResponseWritingModel> SubmitEssayAsync(string userId, CreateWritingModel model)
    {
        try
        {
            var prompt = $@"Evaluate this IELTS writing essay and provide a band score (from 1.0 to 9.0, in increments of 0.5 or from A1 to C2) along with brief feedback. Ensure the response strictly follows the format below, with no extra text or deviations.

**Response Format:**
- **Band Score:** [score]
- **Feedback:** [brief feedback]

**Essay Details:**
Title: {model.Title}
Target Band Score: {model.TargetBandScore}
Essay:
{model.Content}";

            var response = await _geminiService.GenerateContentAsync(prompt);
            _logger.LogInformation("Gemini API response: {Response}", response); // Log the raw response

            // Parse the response to extract BandScore and Feedback
            var (bandScore, feedback) = ParseGeminiResponse(response);

            var essay = new WritingEssay
            {
                UserId = userId,
                Title = model.Title,
                Content = model.Content,
                TargetBandScore = model.TargetBandScore,
                BandScore = bandScore,
                Feedback = feedback,
                CreatedAt = DateTime.UtcNow
            };

            var collection = _mongoContext.GetCollection<WritingEssay>("Essays");
            await collection.InsertOneAsync(essay);

            return new ResponseWritingModel
            {
                Id = essay.Id,
                Title = essay.Title,
                TargetBandScore = essay.TargetBandScore,
                Content = essay.Content,
                BandScore = essay.BandScore,
                Feedback = essay.Feedback,
                CreatedAt = essay.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit essay for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<ResponseWritingModel>> GetEssayHistoryAsync(string userId, int page, int pageSize)
    {
        try
        {
            var collection = _mongoContext.GetCollection<WritingEssay>("Essays");

            var essays = await collection.Find(e => e.UserId == userId)
                .SortByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return essays.Select(e => new ResponseWritingModel
            {
                Id = e.Id,
                Title = e.Title,
                TargetBandScore = e.TargetBandScore,
                Content = e.Content,
                BandScore = e.BandScore,
                Feedback = e.Feedback,
                CreatedAt = e.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve essay history for user {UserId}", userId);
            throw;
        }
    }

    private (string BandScore, string Feedback) ParseGeminiResponse(string response)
    {
        try
        {
            // Match format with bold markdown: "- **Band Score:** [score]\n" and "- **Feedback:** [feedback]"
            var bandScoreMatch = Regex.Match(response, @"-\s*\*\*Band Score:\*\*\s*(.*?)(?:\n|$)", RegexOptions.IgnoreCase);
            var feedbackMatch = Regex.Match(response, @"-\s*\*\*Feedback:\*\*\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (bandScoreMatch.Success && feedbackMatch.Success)
            {
                var bandScore1 = bandScoreMatch.Groups[1].Value.Trim();
                var feedback1 = feedbackMatch.Groups[1].Value.Trim();
                return (bandScore1, feedback1);
            }

            // Fallback: Process line by line for more flexibility
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string bandScore = "Unknown";
            string feedback = response; // Default to raw response if parsing fails

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("- **Band Score:**", StringComparison.OrdinalIgnoreCase))
                {
                    bandScore = trimmedLine.Replace("- **Band Score:**", "", StringComparison.OrdinalIgnoreCase).Trim();
                }
                else if (trimmedLine.StartsWith("- **Feedback:**", StringComparison.OrdinalIgnoreCase))
                {
                    feedback = trimmedLine.Replace("- **Feedback:**", "", StringComparison.OrdinalIgnoreCase).Trim();
                }
            }

            if (bandScore == "Unknown")
            {
                _logger.LogWarning("Could not parse Band Score from response: {Response}", response);
            }

            return (bandScore, feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini API response: {Response}", response);
            return ("Unknown", response); // Return raw response as feedback if parsing fails
        }
    }
}