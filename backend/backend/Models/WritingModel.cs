using System.ComponentModel.DataAnnotations;


namespace backend.Models;

public class CreateWritingModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Title must be between 5 and 100 characters")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Content is required")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Content must be between 50 and 5000 characters")]
    public string Content { get; set; }

    [Required(ErrorMessage = "BandScore is required")]
    [RegularExpression(@"^(A1|A2|B1|B2|C1|C2|0(\.0|\.5)?|[1-8](\.0|\.5)?|9(\.0)?)$", ErrorMessage = "BandScore must be A1-C2 or a number from 1.0 to 9.0")]
    public string TargetBandScore { get; set; }
}

public class ResponseWritingModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string TargetBandScore { get; set; }
    public string Content { get; set; }
    public string BandScore { get; set; }
    public string Feedback { get; set; }
    public DateTime CreatedAt { get; set; }
}
