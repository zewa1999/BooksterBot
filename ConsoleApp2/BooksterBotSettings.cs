using System.ComponentModel.DataAnnotations;

namespace BooksterBot;

internal sealed class BooksterBotSettings
{
    public const string SectionName = "BooksterBotSettings";

    [Required, EmailAddress]
    public string BooksterEmail { get; set; } = null!;

    [Required]
    public string BooksterPassword { get; set; } = null!;

    [Required, Url]
    public string BooksterBookLink { get; set; } = null!;

    [Required, EmailAddress]
    public string GmailEmail { get; set; } = null!;

    [Required]
    public string GmailPassword { get; set; } = null!;
}