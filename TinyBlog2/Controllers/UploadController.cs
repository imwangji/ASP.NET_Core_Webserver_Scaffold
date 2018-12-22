using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TinyBlog2.Models;

namespace TinyBlog2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TinyBlog2Context dbContext;
        public UploadController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> UploadAsync() {
            var files = Request.Form.Files;

            List<Uri> uploadList = new List<Uri>();
            //TryParse后将这个变量填充
            CloudStorageAccount storageAccount = null;
            if (CloudStorageAccount.TryParse(_configuration.GetConnectionString("BlobStorageConectionString"), out storageAccount))
            {
                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference(_configuration.GetSection("StorageContainerName").Value);
                await container.CreateIfNotExistsAsync();

                foreach (var file in files)
                {
                    CloudBlockBlob blob = container.GetBlockBlobReference(file.FileName);
                    await blob.UploadFromStreamAsync(file.OpenReadStream());
                    uploadList.Add(blob.Uri);
                }
                //dbContext.Files.Add(new File() { Uri = blob.Uri.OriginalString });
                //dbContext.SaveChanges();
                return Ok(uploadList);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}