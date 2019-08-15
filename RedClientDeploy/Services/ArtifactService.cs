using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedClientDeploy.Services
{
    public class ArtifactService
    {
        public async Task<string> GetScriptsAsync()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            string container = Environment.GetEnvironmentVariable("StorageContainer");
            string folder = Environment.GetEnvironmentVariable("ArtifactFolder");


            bool ret = await ExtractArtifactAsync(connectionString, container, folder, tempPath);

            return Path.Combine(tempPath, "a");
        }
        private static async Task<bool> ExtractArtifactAsync(string connectionString, string containerName, string directoryName, string extractDirectory)
        {

            string storageConnectionString = connectionString;

            try
            {
                if (CloudStorageAccount.TryParse(storageConnectionString, out CloudStorageAccount sa))
                {
                    CloudBlobClient client = sa.CreateCloudBlobClient();

                    CloudBlobContainer container = client.GetContainerReference(containerName);
                    CloudBlobDirectory directory = container.GetDirectoryReference(directoryName);

                    BlobResultSegment result = await directory.ListBlobsSegmentedAsync(true, BlobListingDetails.None, 500, null, null, null);

                    CloudBlockBlob blob = (CloudBlockBlob)result.Results.OrderByDescending(i => i.Container.Properties.LastModified).Last();

                    MemoryStream ms = new MemoryStream();
                    await blob.DownloadToStreamAsync(ms);
                    ZipArchive za = new ZipArchive(ms);

                    Directory.CreateDirectory(extractDirectory);

                    // string extractDirectory = Path.Combine(Path.GetTempPath(), tempPath.ToString());

                    za.ExtractToDirectory(extractDirectory);

                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }


            return true;
        }
    }
}
