using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
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
using System.IO;

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
            try
            {


                //实例化一个azure视觉服务
                ComputerVisionClient computerVision = new ComputerVisionClient(
                    new ApiKeyServiceClientCredentials(_configuration.GetConnectionString("SubscriptionKey")),
                    new System.Net.Http.DelegatingHandler[] { });
                //设置azure视觉服务的位置，必须要要和订阅位置一样
                computerVision.Endpoint = "https://eastasia.api.cognitive.microsoft.com";
                //用于返回
                List<UploadFile> uploadList = new List<UploadFile>();
                //获取当前用户
                TinyBlog2User currentUser = await _userManager.GetUserAsync(Request.HttpContext.User);
                //实例化一个MD5类
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

                //TryParse后将这个变量填充
                CloudStorageAccount storageAccount = null;
                //尝试连接azure Storage
                if (CloudStorageAccount.TryParse(_configuration.GetConnectionString("BlobStorageConectionString"), out storageAccount))
                {
                    //创建azureStorage存储账户实例
                    var client = storageAccount.CreateCloudBlobClient();
                    //获得azureStorage容器实例
                    var container = client.GetContainerReference(_configuration.GetSection("StorageContainerName").Value);
                    //检查container是否存在 或 新建
                    await container.CreateIfNotExistsAsync();
                    //遍历所有上传来的文件
                    foreach (var file in uploadFiles)
                    {
                        //计算文件MD5值
                        byte[] md5Value = md5.ComputeHash(file.OpenReadStream());
                        //根据md5查询看是否已经存在
                        UploadFile exsitingFile = _dbContext.uploadFiles.FirstOrDefault(f => f.md5 == md5Value);
                        //不存在则进入上传流程
                        if (exsitingFile == null)
                        {
                            //根据文件名创建一个blob文件
                            CloudBlockBlob fileBlob = container.GetBlockBlobReference(file.FileName);
                            //根据文件名创建一个缩略图的blob文件
                            CloudBlockBlob thumbnailBlob = container.GetBlockBlobReference("thumb_" + file.FileName);
                            //创建一个文件缩略图的流
                            Stream thumbnailStream = await computerVision.GenerateThumbnailInStreamAsync(100, 100, file.OpenReadStream(), true);
                            //上传文件
                            await fileBlob.UploadFromStreamAsync(file.OpenReadStream());
                            //上传缩略图
                            await thumbnailBlob.UploadFromStreamAsync(thumbnailStream);
                            // 创建DTO
                            UploadFile uploadedFile = new UploadFile()
                            {
                                md5 = md5Value,
                                Uri = fileBlob.Uri.ToString(),
                                User = currentUser,
                                ThumbnialUri = thumbnailBlob.Uri.ToString()
                            };
                            //将结果写入数据库
                            _dbContext.uploadFiles.Add(uploadedFile);
                            //存入数据库
                            _dbContext.SaveChanges();
                            //加入返回列表
                            uploadList.Add(uploadedFile);
                        }
                        //存在则直接插入数据库
                        else
                        {
                            // 创建DTO
                            UploadFile uploadedFile = new UploadFile()
                            {
                                md5 = exsitingFile.md5,
                                Uri = exsitingFile.Uri,
                                User = currentUser,
                                ThumbnialUri = exsitingFile.ThumbnialUri
                            };
                            // 入库
                            _dbContext.uploadFiles.Add(uploadedFile);
                            _dbContext.SaveChanges();
                            uploadList.Add(uploadedFile);
                        }
                    }
                    return Ok(uploadList);
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }catch(Exception e)
            {
                throw new ApplicationException(e.Message);
            }
        }
    }
}