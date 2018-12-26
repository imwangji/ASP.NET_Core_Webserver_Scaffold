using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TinyBlog2.Areas.Identity.Data;
using TinyBlog2.Models;

namespace TinyBlog2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TinyBlog2Context _dbContext;
        private readonly UserManager<TinyBlog2User> _userManager;
        public UploadController(IConfiguration configuration, UserManager<TinyBlog2User> userManager,TinyBlog2Context dbContext)
        {
            _configuration = configuration;
            _userManager = userManager;
            _dbContext = dbContext;
        }
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> UploadAsync(IFormFileCollection uploadFiles) {
            List<string> uploadList = new List<string>();
            TinyBlog2User currentUser = await _userManager.GetUserAsync(Request.HttpContext.User);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

            //TryParse后将这个变量填充
            CloudStorageAccount storageAccount = null;
            if (CloudStorageAccount.TryParse(_configuration.GetConnectionString("BlobStorageConectionString"), out storageAccount))
            {
                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference(_configuration.GetSection("StorageContainerName").Value);
                await container.CreateIfNotExistsAsync();

                foreach (var file in uploadFiles)
                {
                    byte[] md5Value = md5.ComputeHash(file.OpenReadStream());
                    UploadFile selectedFile = _dbContext.uploadFiles.FirstOrDefault(f => f.md5 == md5Value);
                    if(selectedFile == null)
                    {
                        CloudBlockBlob blob = container.GetBlockBlobReference(file.FileName);
                        await blob.UploadFromStreamAsync(file.OpenReadStream());
                        _dbContext.uploadFiles.Add(new UploadFile() { md5 = md5Value, Uri = blob.Uri.ToString(), User = currentUser });
                        _dbContext.SaveChanges();
                        uploadList.Add(blob.Uri.ToString());
                    }
                    else
                    {
                        _dbContext.uploadFiles.Add(new UploadFile() {md5 = selectedFile.md5,Uri = selectedFile.Uri,User = currentUser });
                        _dbContext.SaveChanges();
                        uploadList.Add(selectedFile.Uri);
                    }
                }
                return Ok(uploadList);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}