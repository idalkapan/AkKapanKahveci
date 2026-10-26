interface IDocumentStorage
{
    string? DetectExtension(byte[] content);
    Task SaveAsync(string id, string extension, byte[] content);
    string GetPath(string id, string extension);
}

sealed class LocalDocumentStorage : IDocumentStorage
{
    private const string DirectoryPath = "data/documents";

    public string? DetectExtension(byte[] content)
    {
        if (content.Length < 8)
            return null;
        if (content.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            return ".pdf";
        if (content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ".png";
        if (content[0] == 255 && content[1] == 216 && content[2] == 255)
            return ".jpg";
        return null;
    }

    public Task SaveAsync(string id, string extension, byte[] content) =>
        File.WriteAllBytesAsync(GetPath(id, extension), content);

    public string GetPath(string id, string extension) =>
        Path.GetFullPath(Path.Combine(DirectoryPath, id + extension));
}
