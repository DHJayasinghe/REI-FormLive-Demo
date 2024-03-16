using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System.Diagnostics;
using System.IO.Compression;

internal class Program
{
    private static void Main(string[] args)
    {
        string sampleImagePath = "C:\\Users\\DhanukaJayasinghe\\Downloads\\leaves-6975462_1280.webp";
        string destinationImageFolderPath = Path.GetTempPath() + "\\Sample\\";
        string destinationZipFolderPath = Path.GetTempPath() + "\\Zip\\";

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddSingleton<BlobHelper>();

        var app = builder.Build();

        var stopwatch = new Stopwatch();

        app.MapGet("", () => "Fast uploader test API running");
        app.MapGet("/test/zip", async (BlobHelper blobHelper) =>
        {
            string[] imageFiles = Directory.GetFiles(destinationImageFolderPath);

            stopwatch.Reset();
            stopwatch.Start();

            using (var memoryStream = new MemoryStream())
            {
                ZipFile.CreateFromDirectory(destinationImageFolderPath, memoryStream);

                try
                {
                    memoryStream.Position = 0;
                    var blobClient = blobHelper.GetBlobClient("single-zip.zip");
                    var r = await blobClient.UploadAsync(memoryStream, false);
                }
                catch (RequestFailedException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

            stopwatch.Stop();

            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            return "Images uploaded in " + elapsedSeconds + " secs";
        });
        app.MapGet("/test/zip/chunk", async (BlobHelper blobHelper) =>
        {
            string[] imageFiles = Directory.GetFiles(destinationImageFolderPath);

            stopwatch.Reset();
            stopwatch.Start();

            var filename = Path.GetFileName(destinationImageFolderPath);
            string destinationZipFilePath = destinationZipFolderPath + "sample.zip";

            //ZipFile.CreateFromDirectory(destinationFolderPath, destinationZipFilePath);

            try
            {
                var blockBlobClient = blobHelper.GetBlockBlobClient("sample123.zip");
                const int chunkSize = 20 * 1024 * 1024; // MB

                using FileStream fs = File.OpenRead(destinationZipFilePath);
                long fileSize = fs.Length;
                long offset = 0;
                var tasks = new List<Task<Response<BlockInfo>>>();
                var blockIds = new List<string>();
                while (offset < fileSize)
                {
                    long remainingLength = fileSize - offset;
                    int length = (int)Math.Min(remainingLength, chunkSize);

                    // Upload the chunk
                    byte[] buffer = new byte[length];
                    fs.Seek(offset, SeekOrigin.Begin);
                    await fs.ReadAsync(buffer.AsMemory(0, length));
                    using (var ms = new MemoryStream(buffer))
                    {
                        var base64BlockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                        await blockBlobClient.StageBlockAsync(base64BlockId, ms);
                        blockIds.Add(base64BlockId);
                    }

                    offset += length;
                }

                Console.WriteLine(blockIds.Count + " blocks");
                await Task.WhenAll(tasks);

                // Commit the uploaded chunks
                await blockBlobClient.CommitBlockListAsync(blockIds);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            stopwatch.Stop();

            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            return "Images uploaded in " + elapsedSeconds + " secs";
        });
        app.MapGet("/test/direct/batch", async (BlobHelper blobHelper) =>
        {
            //CreateSampleImages(sampleImagePath, destinationImageFolderPath, 1000);
            //await UploadBatchAsync(1, 10, 100, blobHelper);
            //await UploadBatchAsync(2, 20, 50, blobHelper);
            //await UploadBatchAsync(3, 50, 20, blobHelper);
            //await UploadBatchAsync(4, 100, 10, blobHelper);
            //await UploadBatchAsync(5, 200, 5, blobHelper);
            await UploadBatchAsync(6, 1000, 1, blobHelper);

            return "Test completed";
        });
        app.MapGet("/test/zip/batch", async (BlobHelper blobHelper) =>
        {
            //CreateSampleImages(sampleImagePath, destinationImageFolderPath, 1000);
            await ZipAndUploadSampleAsync(1, 10, blobHelper);
            await ZipAndUploadSampleAsync(2, 20, blobHelper);
            await ZipAndUploadSampleAsync(3, 50, blobHelper);
            await ZipAndUploadSampleAsync(4, 100, blobHelper);

            return "Test completed";
        });

        app.Run();

        async Task ZipAndUploadSampleAsync(int id, int filesPerZip, BlobHelper blobHelper)
        {
            var destinationZipFolder = $"{destinationZipFolderPath}{filesPerZip}\\";

            await ZipImagesAsync(destinationImageFolderPath, destinationZipFolder, filesPerZip);

            string[] zipFiles = Directory.GetFiles(destinationZipFolder);

            stopwatch.Reset();
            stopwatch.Start();

            var tasks = zipFiles.Select(path => UploadToBlobStorageAsync(path, blobHelper));
            await Task.WhenAll(tasks);

            stopwatch.Stop();

            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine($"Sample {id}: Uploaded {tasks.Count()} zips, {filesPerZip} files per zip -> Time taken: {elapsedSeconds} secs");

            //Directory.Delete(destinationZipFolder, recursive: true);
        }

        async Task UploadBatchAsync(int id, int take, int batchSize, BlobHelper blobHelper)
        {
            string[] imageFiles = Directory.GetFiles(destinationImageFolderPath);
            stopwatch.Reset();
            stopwatch.Start();

            foreach (var batch in Enumerable.Range(0, batchSize))
            {
                var tasks = imageFiles.Take(new Range(batch, batch + take)).Select(path => UploadToBlobStorageAsync(path, blobHelper));
                //Console.WriteLine("Uploading " + tasks.Count() + " images");
                await Task.WhenAll(tasks);
            }

            stopwatch.Stop();

            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine($"Sample {id}: Uploaded {batchSize} batches, {take} files per batch -> Time taken: {elapsedSeconds} secs");
        }
    }

    private static void CreateSampleImages(string sampleImagePath, string destinationImageFolderPath, int sampleSize)
    {
        Directory.CreateDirectory(destinationImageFolderPath);
        for (int i = 1; i <= sampleSize; i++)
        {
            string destinationImagePath = Path.Combine(destinationImageFolderPath, $"image_{i}.jpg");
            File.Copy(sampleImagePath, destinationImagePath, true);
            //Console.WriteLine($"Copied image {i}");
        }
    }

    private static async Task UploadToBlobStorageAsync(string filePath, BlobHelper blobHelper)
    {
        string fileName = Path.GetFileName(filePath);
        //Console.WriteLine("Uploading file " + fileName);


        BlobClient blobClient = blobHelper.GetBlobClient(fileName);
        try
        {
            FileStream fs = File.OpenRead(filePath);
            await blobClient.UploadAsync(fs, true);
            fs.Dispose();
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine("Error uploading image " + filePath);
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task ZipImagesAsync(string sourceFolderPath, string destinationZipFolderPath, int batchSize)
    {
        try
        {
            // Ensure the destination directory exists
            Directory.CreateDirectory(destinationZipFolderPath);

            string[] imageFiles = Directory.GetFiles(sourceFolderPath); // You can adjust the file extension as needed

            var batches = imageFiles.Select((file, index) => new { file, index })
                                    .GroupBy(x => x.index / batchSize)
                                    .Select(group => group.Select(x => x.file).ToList())
                                    .ToList();

            await Task.WhenAll(batches.Select((batch, index) => CreateZipAsync(batch, destinationZipFolderPath, index + 1)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error zipping images: {ex.Message}");
        }
    }

    private static async Task CreateZipAsync(List<string> imageFiles, string destinationZipFolderPath, int zipIndex)
    {
        string zipFilePath = Path.Combine(destinationZipFolderPath, $"batch_{zipIndex}.zip");

        using var zipToOpen = new FileStream(zipFilePath, FileMode.Create);
        using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
        foreach (string imageFile in imageFiles)
        {
            string entryName = Path.GetFileName(imageFile);
            await Task.Run(() => archive.CreateEntryFromFile(imageFile, entryName));
        }

        //Console.WriteLine($"Batch {zipIndex} zipped successfully.");
    }
}

internal class BlobHelper
{
    private readonly string connectionString;
    private readonly string containerName = "fastupload2";
    private readonly BlobClientOptions options;

    public BlobHelper(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("StorageAccount");
        options = new BlobClientOptions();
        options.Retry.MaxRetries = 3;
        options.Retry.NetworkTimeout = TimeSpan.FromMinutes(5);
    }

    public BlobClient GetBlobClient(string name)
    {
        var blobServiceClient = new BlobServiceClient(connectionString, options);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        return containerClient.GetBlobClient(name);
    }

    public BlockBlobClient GetBlockBlobClient(string name)
    {
        return new BlockBlobClient(connectionString, containerName, name, options);
    }
}