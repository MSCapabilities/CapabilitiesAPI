using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EYExampleAPI.Models;
using EYExampleAPI.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace EYExampleAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExampleController : ControllerBase
    {
        private readonly EYExampleAPIContext _context;
        private IConfiguration _configuration;

        public ExampleController(EYExampleAPIContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Example
        [HttpGet]
        public IEnumerable<ExampleItem> GetExampleItem()
        {
            return _context.ExampleItem;
        }




        private bool ExampleItemExists(int id)
        {
            return _context.ExampleItem.Any(e => e.Id == id);
        }

       

        [HttpPost, Route("upload")]
        public async Task<IActionResult> UploadFile([FromForm]ExampleImageItem example)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            try
            {
                using (var stream = example.Image.OpenReadStream())
                {
                    var cloudBlock = await UploadToBlob(example.Image.FileName, null, stream);
                    //// Retrieve the filename of the file you have uploaded
                    //var filename = provider.FileData.FirstOrDefault()?.LocalFileName;
                    if (string.IsNullOrEmpty(cloudBlock.StorageUri.ToString()))
                    {
                        return BadRequest("An error has occured while uploading your file. Please try again.");
                    }

                    ExampleItem exampleItem = new ExampleItem();
                    exampleItem.Title = example.Title;
                    exampleItem.Tags = example.Tags;

                    System.Drawing.Image image = System.Drawing.Image.FromStream(stream);
                    exampleItem.Height = image.Height.ToString();
                    exampleItem.Width = image.Width.ToString();
                    exampleItem.Url = cloudBlock.SnapshotQualifiedUri.AbsoluteUri;
                    exampleItem.Uploaded = DateTime.Now.ToString();

                    _context.ExampleItem.Add(exampleItem);
                    await _context.SaveChangesAsync();

                    return Ok($"File: {example.Title} has successfully uploaded");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"An error has occured. Details: {ex.Message}");
            }


        }

        private async Task<CloudBlockBlob> UploadToBlob(string filename, byte[] imageBuffer = null, System.IO.Stream stream = null)
        {

            var accountName = _configuration["AzureBlob:name"];
            var accountKey = _configuration["AzureBlob:key"]; ;
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer imagesContainer = blobClient.GetContainerReference("blobcontainer");

            string storageConnectionString = _configuration["AzureBlob:connectionString"];

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Generate a new filename for every new blob
                    var fileName = Guid.NewGuid().ToString();
                    fileName += GetFileExtention(filename);

                    // Get a reference to the blob address, then upload the file to the blob.
                    CloudBlockBlob cloudBlockBlob = imagesContainer.GetBlockBlobReference(fileName);

                    if (stream != null)
                    {
                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                    }
                    else
                    {
                        return new CloudBlockBlob(new Uri(""));
                    }

                    return cloudBlockBlob;
                }
                catch (StorageException ex)
                {
                    return new CloudBlockBlob(new Uri(""));
                }
            }
            else
            {
                return new CloudBlockBlob(new Uri(""));
            }

        }

        private string GetFileExtention(string fileName)
        {
            if (!fileName.Contains("."))
                return ""; //no extension
            else
            {
                var extentionList = fileName.Split('.');
                return "." + extentionList.Last(); //assumes last item is the extension 
            }
        }




        // DELETE: api/Example/5
        [HttpDelete("{blobName}/{id}")]
        public async Task<IActionResult> DeleteExampleItemfromBoth([FromRoute] string blobName, [FromRoute] int id)
        {
            var accountName = _configuration["AzureBlob:name"];
            var accountKey = _configuration["AzureBlob:key"]; ;
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer imagesContainer = blobClient.GetContainerReference("blobcontainer");

            string storageConnectionString = _configuration["AzureBlob:connectionString"];

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
          
                var blob = imagesContainer.GetBlobReference(blobName);
                if (await blob.DeleteIfExistsAsync() == true)
                {
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    var exampleItem = await _context.ExampleItem.FindAsync(id);

                    if (exampleItem == null)
                    {
                        return NotFound();
                    }

                    _context.ExampleItem.Remove(exampleItem);
                    await _context.SaveChangesAsync();
                    return Ok($"File: {blobName} has successfully deleted");

                } else
                {
                    return BadRequest($"An error has occured.");
                }

            } else
            {
                return BadRequest($"An error has occured.");
            }
            
        }

        //// DELETE: api/Audio/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteExampleItem([FromRoute] int id)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var exampleItem = await _context.ExampleItem.FindAsync(id);
        //    if (exampleItem == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.ExampleItem.Remove(exampleItem);
        //    await _context.SaveChangesAsync();

        //    return Ok(exampleItem);
        //}


        //// DELETE: api/Example/5
        //[HttpDelete("{blobName}")]
        //public async Task<IActionResult> DeleteExampleItemfromBlob([FromRoute] string blobName)
        //{
        //    var accountName = _configuration["AzureBlob:name"];
        //    var accountKey = _configuration["AzureBlob:key"]; ;
        //    var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
        //    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

        //    CloudBlobContainer imagesContainer = blobClient.GetContainerReference("blobcontainer");

        //    string storageConnectionString = _configuration["AzureBlob:connectionString"];

        //    // Check whether the connection string can be parsed.
        //    if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
        //    {

        //        var blob = imagesContainer.GetBlobReference(blobName);
        //        if (await blob.DeleteIfExistsAsync() == true)
        //        {
        //            return Ok($"File: {blobName} has successfully deleted");

        //        }
        //        else
        //        {
        //            return BadRequest($"An error has occured.");
        //        }

        //    }
        //    else
        //    {
        //        return BadRequest($"An error has occured.");
        //    }

        //}


    }
}