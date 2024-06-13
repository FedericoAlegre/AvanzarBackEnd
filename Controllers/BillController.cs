using AvanzarBackEnd.Data;
using AvanzarBackEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AvanzarBackEnd.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class BillController(AppDbContext appDbContext) : ControllerBase
    {
        public AppDbContext AppDbContext { get; set; } = appDbContext;

        // GET: api/<BillController>
        [HttpGet]
        public async Task<ActionResult> GetList()
        {
            List<Bill> list = new List<Bill>();

            try
            {
                list = await this.AppDbContext.Bills.ToListAsync();
                /*foreach (Bill item in list)
                {
                    item.Client = await this.AppDbContext.Clients.FindAsync(item.ClientId);
                    item.Client!.Bills = null;
                    item.Product = await this.AppDbContext.Products.FindAsync(item.ProductId);
                    item.Product!.Bills = null;
                }*/

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message, response = list });
            }
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<ActionResult> GetBillById(int id)
        {
            Bill? dbBill = new Bill();

            try
            {
                dbBill = await this.AppDbContext.Bills.FindAsync(id);

                if (dbBill == null) return StatusCode(StatusCodes.Status404NotFound, new { message = $"Bill with id: {id} does not exists ", response = dbBill });

                return Ok(dbBill);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message, response = dbBill });
            }
        }

        // POST api/<BillController>
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Add([FromBody] Bill model)
        {

            try
            {
                if (model == null) return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Bill was null" });
                if (model.Client == null) return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Client was null" });
                if (model.ProductId == 0) return StatusCode(StatusCodes.Status500InternalServerError, new { message = "ProductId was null" });

                var dbClient = this.AppDbContext.Clients.FirstOrDefaultAsync(x => x.Email!.Equals(model.Client.Email));
                if (dbClient.Result != null)
                {
                    model.Client = null;
                    model.ClientId = dbClient.Result!.Id;
                }
                Product? dbProduct = await this.AppDbContext.Products.FindAsync(model.ProductId);
                if (dbProduct == null) return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Product with id: {model.ProductId} does not exist" });
                model.Price = dbProduct.Price;
                model.Date = DateTime.Now;
                AppDbContext.Bills.Add(model);
                await AppDbContext.SaveChangesAsync();
                return StatusCode(StatusCodes.Status200OK, new { message = "OK" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("update")]
        [Authorize]
        public async Task<ActionResult> Update([FromBody] Bill model)
        {

            try
            {
                if (model == null) return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Bill was null" });
                var dbModel = this.AppDbContext.Bills.Find(model.Id);
                if (dbModel is null) return StatusCode(StatusCodes.Status404NotFound, new { message = "NOT FOUND", response = model });

                dbModel.ClientId = model.ClientId == 0 ? dbModel.ClientId : model.ClientId;
                dbModel.ProductId = model.ProductId == 0 ? dbModel.ProductId : model.ProductId;
                dbModel.Price = model.Price == 0 ? dbModel.Price : model.Price;
                this.AppDbContext.Bills.Update(dbModel);

                await this.AppDbContext.SaveChangesAsync();
                return StatusCode(StatusCodes.Status200OK, new { message = "Appointment Updated", response = model });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete]
        [Authorize]
        [Route("delete/{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                Bill dbBill = this.AppDbContext.Bills.Find(id)!;
                if (dbBill != null)
                {

                    this.AppDbContext.Bills.Remove(dbBill);
                    await this.AppDbContext.SaveChangesAsync();

                    return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = dbBill });
                }

                return StatusCode(StatusCodes.Status404NotFound, new { message = "NOT FOUND", response = id });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
