using Minio.DataModel.Args;
using Minio.Exceptions;
using Minio;
using System.Net;
using OverflowBackend.Models.Response;

namespace OverflowBackend.Services.Implementantion
{
    public class BlobStorageService
    {
        private IMinioClient minio;
        //private string bucketName = "servicii-and-pictures";

        public BlobStorageService()
        {
            SetupMinio();
        }

        private void SetupMinio()
        {
            var ip = Environment.GetEnvironmentVariable("DB_IP");
            var endpoint = $"{ip}:9000";
            var accessKey = "minioadmin";
            var secretKey = Environment.GetEnvironmentVariable("MINIO_PASS");
            try
            {
                this.minio = new MinioClient()
                                    .WithEndpoint(endpoint)
                                    .WithCredentials(accessKey, secretKey)
                                    .Build();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task DeleteImage(string fileName, string bucketName)
        {
            var objectStat = await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucketName).WithObject(fileName));
            if (objectStat.ContentType == null)
            {
                throw new ObjectNotFoundException();
            }
            var args = new RemoveObjectArgs().WithBucket(bucketName).WithObject(fileName);
            await minio.RemoveObjectAsync(args);
        }

        public async Task<Image> DownloadImage(string fileName, string bucketName)
        {
            var memoryStream = new MemoryStream();

            // Create GetObjectArgs with the bucket name, object name, and callback stream
            var getObj = new GetObjectArgs().WithBucket(bucketName).WithObject(fileName).WithCallbackStream((stream) =>
            {
                stream.CopyTo(memoryStream);
            });
            var objectStat = await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucketName).WithObject(fileName));
            // Download object asynchronously and await for the completion
            await minio.GetObjectAsync(getObj);

            // Convert the memory stream to a base64 string and return
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            var imagine = new Image() { FileName = fileName, FileContentBase64 = $"data:{objectStat.ContentType};base64," + base64, FileType = objectStat.ContentType };

            return imagine;

        }

        public async Task<Image> DownloadImages(string fileName, string bucketName)
        {
            var memoryStream = new MemoryStream();

            // Create GetObjectArgs with the bucket name, object name, and callback stream
            var getObj = new GetObjectArgs().WithBucket(bucketName).WithObject(fileName).WithCallbackStream((stream) =>
            {
                stream.CopyTo(memoryStream);
            });
            var objectStat = await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucketName).WithObject(fileName));
            // Download object asynchronously and await for the completion
            await minio.GetObjectAsync(getObj);

            // Convert the memory stream to a base64 string and return
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            var imagine = new Image() { FileContentBase64 = $"data:{objectStat.ContentType};base64," + base64, FileType = objectStat.ContentType };

            return imagine;

        }


        public async Task UploadImage(string fileName, string fileType, string fileContentBase64, string bucketName)
        {

            //var location = "us-east-1";
            var objectName = fileName;

            var contentType = fileType;

            try
            {
                // Make a bucket on the server, if not already present.
                var beArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);
                bool found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
                if (!found)
                {
                    var mbArgs = new MakeBucketArgs()
                        .WithBucket(bucketName);
                    await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
                }
                // Upload a file to bucket.
                string base64Data = fileContentBase64.Split(',')[1];

                byte[] base64Bytes = Convert.FromBase64String(base64Data);
                using (var stream = new MemoryStream(base64Bytes))
                {
                    var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType(contentType);
                    await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                }

                /*var getObj = new GetObjectArgs().WithBucket(bucketName).WithObject(objectName).WithCallbackStream((stream) =>
                {
                    stream.CopyTo(Console.OpenStandardOutput());
                }); ;
                var download =  minio.GetObjectAsync(getObj);
                var x = 1;*/

                Console.WriteLine("Successfully uploaded " + objectName);
            }
            catch (MinioException e)
            {
                Console.WriteLine("File Upload Error: {0}", e.Message);
                throw;
            }
        
         }
    }
}
