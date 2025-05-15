using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using backend.Data;
using backend.Models;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class ListeningService : IListeningService
{
    private readonly MongoDbContext _mongoContext;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<ListeningService> _logger;

    public ListeningService(MongoDbContext mongoContext, IGeminiService geminiService, ILogger<ListeningService> logger)
    {
        _mongoContext = mongoContext;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<ResponseListeningModel> SubmitDictationAsync(CreateListeningModel model)
    {
        try
        {
            // Prepare prompt for Gemini API to generate content from YouTube link
            var prompt = $@"Analyze the YouTube video at {model.LinkVideo}.
Rewrite the subtitle of a YouTube video into a coherent paragraph while maintaining the default tone. The rewritten text should be suitable for printing, ensuring clarity and readability without altering the original message. 

- Focus on ensuring the paragraph flows naturally, connecting thoughts and ideas effectively.
- Avoid any informal language or excessive punctuation that might detract from a formal written print format.

# Output Format
- The output should be a single, properly structured paragraph ready for paper printing. 

# Examples
- If the subtitle is: ""How to cook a perfect steak in 5 minutes,"" the output should be: ""This video explains how to cook a perfect steak in just five minutes, providing tips and tricks for quick preparation and cooking methods that guarantee delicious results.""

- For a subtitle like: ""The top 10 places to visit in Europe,"" the output could be: ""In this video, we explore the top ten places to visit in Europe, highlighting must-see destinations that offer unique experiences and breathtaking views.""

# Notes
- Ensure to maintain the essence and primary message of the original subtitle while enhancing readability for print.
Ensure the response follows this format:

**Response Format:**
- **Title:** [Generated title]
- **Topic:** [Generated topic(named IELTS, Toefl, Other)]
- **Content:** [Generated content or description]
- **Band Score:** [Estimated band score, named A1, A2, B1, B2, C1, C2]
- **SrtSubtitles:** [Generated SRT subtitles]

**Video Link:** {model.LinkVideo}";

            var response = await _geminiService.GenerateContentAsync(prompt);
            _logger.LogInformation("Gemini API response: {Response}", response);

            // Parse Gemini response
            var (title, topic, content, bandScore, srtSubtitles) = ParseGeminiResponse(response);

            var listeningModel = new MongoListeningModel
            {
                Title = title,
                Topic = topic,
                Content = content,
                BandScore = bandScore,
                LinkVideo = model.LinkVideo,
                SrtSubtitles = srtSubtitles
            };

            var collection = _mongoContext.GetCollection<MongoListeningModel>("listening");
            await collection.InsertOneAsync(listeningModel);

            return new ResponseListeningModel
            {
                Id = listeningModel.Id,
                Title = listeningModel.Title,
                Topic = listeningModel.Topic,
                Content = listeningModel.Content,
                BandScore = listeningModel.BandScore,
                SrtSubtitles = listeningModel.SrtSubtitles
            };
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogWarning("YouTube link {LinkVideo} already exists.", model.LinkVideo);
            return await GetDictationAsync(model); // Return existing dictation if duplicate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit dictation for link {LinkVideo}", model.LinkVideo);
            throw;
        }
    }

    public async Task<ResponseListeningModel> GetDictationAsync(CreateListeningModel model)
    {
        try
        {
            var collection = _mongoContext.GetCollection<MongoListeningModel>("listening");
            var listeningModel = await collection.Find(l => l.LinkVideo == model.LinkVideo).FirstOrDefaultAsync();

            if (listeningModel == null)
            {
                return null;
            }

            return new ResponseListeningModel
            {
                Id = listeningModel.Id,
                Title = listeningModel.Title,
                Topic = listeningModel.Topic,
                Content = listeningModel.Content,
                BandScore = listeningModel.BandScore,
                SrtSubtitles = listeningModel.SrtSubtitles
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve dictation for link {LinkVideo}", model.LinkVideo);
            throw;
        }
    }

   

    public async Task<List<ResponseListeningModel>> GetTopicAsync(string topic, int page, int pageSize)
    {
        try
        {
            var collection = _mongoContext.GetCollection<MongoListeningModel>("listening");
            var listeningModels = await collection.Find(l => l.Topic == topic)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return listeningModels.Select(l => new ResponseListeningModel
            {
                Id = l.Id,
                Title = l.Title,
                Topic = l.Topic,
                Content = l.Content,
                BandScore = l.BandScore,
                SrtSubtitles = l.SrtSubtitles
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve listening topics for topic {Topic}", topic);
            throw;
        }
    }

    private (string Title, string Topic, string Content, string BandScore, string SrtSubtitles) ParseGeminiResponse(string response)
    {
        try
        {
            // Match format with bold markdown
            var titleMatch = Regex.Match(response, @"-\s*\*\*Title:\*\*\s*(.*?)(?:\n|$)", RegexOptions.IgnoreCase);
            var topicMatch = Regex.Match(response, @"-\s*\*\*Topic:\*\*\s*(.*?)(?:\n|$)", RegexOptions.IgnoreCase);
            var contentMatch = Regex.Match(response, @"-\s*\*\*Content:\*\*\s*(.*?)(?:\n|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var bandScoreMatch = Regex.Match(response, @"-\s*\*\*Band Score:\*\*\s*(.*?)(?:\n|$)", RegexOptions.IgnoreCase);
            var srtSubtitlesMatch = Regex.Match(response, @"-\s*\*\*SrtSubtitles:\*\*\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "Unknown";
            var topic = topicMatch.Success ? topicMatch.Groups[1].Value.Trim() : "Unknown";
            var content = contentMatch.Success ? contentMatch.Groups[1].Value.Trim() : response;
            var bandScore = bandScoreMatch.Success ? bandScoreMatch.Groups[1].Value.Trim() : "Unknown";
            var srtSubtitles = srtSubtitlesMatch.Success ? srtSubtitlesMatch.Groups[1].Value.Trim() : string.Empty;

            if (title == "Unknown" || topic == "Unknown" || bandScore == "Unknown")
            {
                _logger.LogWarning("Incomplete parsing of Gemini response: {Response}", response);
            }

            return (title, topic, content, bandScore, srtSubtitles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini API response: {Response}", response);
            return ("Unknown", "Unknown", response, "Unknown", string.Empty);
        }

    }
}