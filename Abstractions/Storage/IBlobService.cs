namespace Platform.Application.Abstractions.Storage;

public interface IBlobService
{
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string containerName);

    Task<(string BlobName, string ContainerName)> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    string GenerateReadSasUrl(string container, string blobName, int expireMinutes = 5);

    List<string> GenerateReadSasUrlsAsync(string container, IEnumerable<string> blobNames, int expireMinutes = 5);

    Task<string> MakePublicAndGetUrl(string container, string blobName);
}
