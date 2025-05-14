using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class CreateListeningModel
{
    [Required(ErrorMessage = "Link Youtube is required")]
    /*
    [RegularExpression(@"^https:\/\/youtu\.be\/[a-zA-Z0-9_-]{11}$", 
        ErrorMessage = "Link must be a valid YouTube URL starting with 'https://youtu.be/' and followed by a valid video ID.")]
    */
    public string LinkVideo { get; set; }
    
}

public class ResponseListeningModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Topic { get; set; }
    public string Content { get; set; }
    public string BandScore { get; set; }
    public string SrtSubtitles { get; set; }
}
