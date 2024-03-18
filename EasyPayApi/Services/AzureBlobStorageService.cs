using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EasyPayApi.Services
{
    public class AzureBlobStorageService
    {
        private readonly CloudBlobContainer _blobContainer;

        public AzureBlobStorageService()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(@"");
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            _blobContainer = blobClient.GetContainerReference("photos");
        }

        /// <summary>
        /// TBD for deleting blob storage photos.
        /// </summary>
        /// <param name="photoUri"></param>
        /// <returns></returns>
        public async Task DeletePhotoAsync(string photoUri)
        {
            // Get the blob reference from the photoUri
            CloudBlockBlob blobToDelete = new CloudBlockBlob(new Uri(photoUri));

            // Delete the blob
            await blobToDelete.DeleteAsync(); 
        }

        /// <summary>
        /// Get a list of all blob storage image urls.
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllPhotoUrlsAsync()
        {
            var results = new List<string>();

            BlobContinuationToken continuationToken = null;
            do
            {
                var response = await _blobContainer.ListBlobsSegmentedAsync(null, continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (IListBlobItem item in response.Results)
                {
                    if (item is CloudBlockBlob blob)
                    {
                        results.Add(blob.Uri.ToString());
                    }
                }
            } while (continuationToken != null);

            return results;
        }

        /// <summary>
        /// Main function for uploading photo image files to blob storage account.
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<string> UploadPhotoAsync(Stream fileStream, string fileName)
        {
            CloudBlockBlob blockBlob = _blobContainer.GetBlockBlobReference(fileName);

            // Set blob to be publicly readable
            var blobPermissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            };

            await _blobContainer.SetPermissionsAsync(blobPermissions);

            // Set the content type to "image/jpeg"
            blockBlob.Properties.ContentType = "image/jpeg";

            await blockBlob.UploadFromStreamAsync(fileStream);

            return blockBlob.Uri.ToString();
        }

    }
}
