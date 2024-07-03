using Amazon.S3.Transfer;
using Amazon.S3;
using AvanzarBackEnd.Data;
using AvanzarBackEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AvanzarBackEnd.Services;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using Amazon.S3.Model;
using MercadoPago.Resource.Preference;

namespace AvanzarBackEnd.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ProductController(AppDbContext appDbContext, IConfiguration configuration, EmailService emailService, IAmazonS3 s3Client, ILogger<ProductController> logger, MercadoPagoService mercadoPagoService) : ControllerBase
    {
        public AppDbContext AppDbContext { get; set; } = appDbContext;
        private readonly IAmazonS3 _s3Client = s3Client;
        private readonly string _bucketName = configuration["AWS:BucketName"]!;
        private readonly EmailService _emailService = emailService;
        private readonly ILogger<ProductController> _logger =  logger;
        private readonly MercadoPagoService _mercadoPagoService = mercadoPagoService;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> GetList(bool isAdmin)
        {
            List<Product> list = new List<Product>();

            try
            {
                list = await this.AppDbContext.Products.ToListAsync();
                if (!isAdmin)
                {
                    foreach (Product product in list)
                    {
                        product.DataUrl = "";
                    }
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message, response = list });
            }
        }

        [HttpGet]
        [Route("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult> GetProductById(int id, bool isAdmin)
        {
            Product? dbProduct = new Product();

            try
            {
                dbProduct = await this.AppDbContext.Products.FindAsync(id);
                if (dbProduct == null) return StatusCode(StatusCodes.Status404NotFound, new { message = $"Product with id: {id} does not exists ", response = dbProduct });
                if (!isAdmin)
                {
                    dbProduct.DataUrl = "";                    
                }
                return Ok(dbProduct);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message, response = dbProduct });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Add([FromBody] Product model)
        {

            try
            {
                if (model != null )
                {
                    if ( model!.Name == null || model.Price == 0 || model.Description == null || model.DataUrl == null || model.ImageUrl == null ) return StatusCode(StatusCodes.Status500InternalServerError, new { message = "All fields are required" });
                    model.isActive = true;
                    AppDbContext.Products.Add(model);
                    await AppDbContext.SaveChangesAsync();
                    
                    return StatusCode(StatusCodes.Status200OK, new { message = "OK" });
                }
                else
                {
                    throw new NullReferenceException("Product was null");
                }

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("update")]
        public async Task<ActionResult> Update([FromBody] Product model)
        {

            try
            {
                Product dbModel = this.AppDbContext.Products.Find(model.Id)!;
                if (dbModel != null)
                {
                    dbModel.Name = model.Name is null ? dbModel.Name : model.Name;
                    dbModel.Price = model.Price == 0 ? dbModel.Price : model.Price;
                    dbModel.isActive = model.isActive is null ? dbModel.isActive : model.isActive;
                    dbModel.ImageUrl = model.ImageUrl is null ? dbModel.ImageUrl : model.ImageUrl;
                    dbModel.DataUrl = model.DataUrl is null ? dbModel.DataUrl : model.DataUrl;
                    dbModel.Description = model.Description is null ? dbModel.Description : model.Description;

                    this.AppDbContext.Products.Update(dbModel);
                    await this.AppDbContext.SaveChangesAsync();
                    return StatusCode(StatusCodes.Status200OK, new { message = "Service Updated", response = dbModel });
                }

                return StatusCode(StatusCodes.Status404NotFound, new { message = "NOT FOUND", response = model });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Uploads a file and an image.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="image">The image to upload.</param>
        /// <returns>The URLs of the uploaded file and image.</returns>
        [HttpPost("upload-file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file/*, [FromForm] IFormFile image*/)
        {
            try
            {
                if (file == null || file.Length == 0 /*|| image == null || image.Length == 0*/)
                    return BadRequest("File not selected");

                //var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var dataUrl = await UploadFileToS3(file, file.FileName);
                // var imageName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                //var imageUrl = await UploadFileToS3(file, fileName);


                _logger.LogInformation($"UploadFile endpoint ok: {dataUrl}");

                return Ok(new { dataUrl = dataUrl/*, imageUrl = imageUrl */});
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"UploadFile endpoint failed: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        private async Task<string> UploadFileToS3(IFormFile file, string fileName)
        {
            using (var newMemoryStream = new MemoryStream())
            {
                file.CopyTo(newMemoryStream);

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = newMemoryStream,
                    Key = fileName,
                    BucketName = _bucketName,
                };

                var fileTransferUtility = new TransferUtility(_s3Client);                
                await fileTransferUtility.UploadAsync(uploadRequest);

                return fileName;
            }
        }


        [HttpPost("purchase")]
        [AllowAnonymous]
        public async Task<IActionResult> PurchaseFile([FromForm]string preferenceId, [FromForm] string email)
        {
            try
            {
                var preference = await _mercadoPagoService.GetPreferenceAsync(preferenceId);
                if (preference == null) throw new Exception("preferenceId was null");
                string productName = preference.Items.First().Title;
                var dbProduct = await this.AppDbContext.Products.FirstOrDefaultAsync(x => x.Name!.Equals(productName));
                if (dbProduct == null)
                    return NotFound();

                var fileData = await DownloadFileFromS3(dbProduct.DataUrl!);
                if (fileData == null)
                    return NotFound("File not found in S3");

                

                var message = "Attached is your purchased file.";
                var mimeType = GetMimeType(dbProduct.DataUrl!);
                await _emailService.SendEmailAsync(email, "Your Purchased File", message, fileData, dbProduct.DataUrl, mimeType);
                _logger.LogInformation($"purchase endpoint ok: {message}");

                return Ok(new { message = "Email sent successfully" });
            }
            catch (Exception ex) {

                _logger.LogInformation($"purchase endpoint failed: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"ERROR DEVUELTO POR EL METODOS: {ex.Message}" });
            }
        }

        private async Task<byte[]> DownloadFileFromS3(string fileName)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };

            using (var response = await _s3Client.GetObjectAsync(request))
            using (var memoryStream = new MemoryStream())
            {
                await response.ResponseStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            switch (extension)
            {
                case ".pdf": return "application/pdf";
                case ".mp4": return "video/mp4";
                // Agregar más tipos MIME según sea necesario
                default: return "application/octet-stream";
            }
        }

    }
}
