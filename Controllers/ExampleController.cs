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

        // GET: api/Example/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExampleItem([FromRoute] int id)
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

            return Ok(exampleItem);
        }

        // PUT: api/Example/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutExampleItem([FromRoute] int id, [FromBody] ExampleItem exampleItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != exampleItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(exampleItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExampleItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Example
        [HttpPost]
        public async Task<IActionResult> PostExampleItem([FromBody] ExampleItem exampleItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.ExampleItem.Add(exampleItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetExampleItem", new { id = exampleItem.Id }, exampleItem);
        }

        // DELETE: api/Example/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExampleItem([FromRoute] int id)
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

            return Ok(exampleItem);
        }

        private bool ExampleItemExists(int id)
        {
            return _context.ExampleItem.Any(e => e.Id == id);
        }

        // GET: api/Example/Tags
        [Route("tags")]
        [HttpGet]
        public async Task<List<string>> GetTags()
        {
            var examples = (from m in _context.ExampleItem
                         select m.Tags).Distinct();

            var returned = await examples.ToListAsync();

            return returned;
        }

        // GET: api/Meme/Tags

        [HttpGet]
        [Route("tag")]
        public async Task<List<ExampleItem>> GetTagsItem([FromQuery] string tags)
        {
            var examples = from m in _context.ExampleItem
                        select m; //get all the memes


            if (!String.IsNullOrEmpty(tags)) //make sure user gave a tag to search
            {
                examples = examples.Where(s => s.Tags.ToLower().Equals(tags.ToLower())); // find the entries with the search tag and reassign
            }

            var returned = await examples.ToListAsync(); //return the memes

            return returned;
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


    }
}