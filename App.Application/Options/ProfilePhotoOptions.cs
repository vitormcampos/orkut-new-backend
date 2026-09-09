namespace App.Application.Options;

public sealed class ProfilePhotoOptions
{
    public const string SectionName = "ProfilePhoto";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
