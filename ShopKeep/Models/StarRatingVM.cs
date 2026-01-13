namespace ShopKeep.Models;

public class StarRatingVM
{
    public double Average { get; set; }
    public int RatingCount { get; set; }

    // Optional styling helpers
    public string? CssClass { get; set; }
    public bool ShowNumericValue { get; set; } = true;
    public bool ShowCount { get; set; } = true;
}
