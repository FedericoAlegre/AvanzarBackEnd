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

namespace AvanzarBackEnd.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class ProductController(AppDbContext appDbContext, IConfiguration configuration, EmailService emailService) : ControllerBase
    {
        public AppDbContext AppDbContext { get; set; } = appDbContext;
        private readonly string _bucketName = configuration["AWS:BucketName"]!;
        private readonly EmailService _emailService = emailService;

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

    }
}
