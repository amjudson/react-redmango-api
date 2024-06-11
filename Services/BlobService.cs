using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RedMango_API.Services;
public class BlobService : IBlobService
{
	private readonly BlobServiceClient blobClientService;

	public BlobService(BlobServiceClient blobServiceClient)
	{
		blobClientService = blobServiceClient;
	}

	public async Task<string> GetBlob(string blobName, string containerName)
	{
		var blobContainerClient = blobClientService.GetBlobContainerClient(containerName);
		var blobClient = blobContainerClient.GetBlobClient(blobName);
		return blobClient.Uri.AbsoluteUri;
	}

	public async Task<bool> DeleteBlob(string blobName, string containerName)
	{
		var blobContainerClient = blobClientService.GetBlobContainerClient(containerName);
		var blobClient = blobContainerClient.GetBlobClient(blobName);
		return await blobClient.DeleteIfExistsAsync();
	}

	public async Task<string> UploadBlob(string blobName, string containerName, IFormFile file)
	{
		var blobContainerClient = blobClientService.GetBlobContainerClient(containerName);
		var blobClient = blobContainerClient.GetBlobClient(blobName);
		var httpHeader = new BlobHttpHeaders
		{
			ContentType = file.ContentType
		};

		var result = await blobClient.UploadAsync(file.OpenReadStream(), httpHeader);
		if (result != null)
		{
			return await GetBlob(blobName, containerName);
		}

		return string.Empty;
	}
}
