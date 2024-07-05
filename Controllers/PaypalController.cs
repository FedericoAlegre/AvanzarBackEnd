using BraintreeHttp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;

namespace AvanzarBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaypalController(PayPalHttpClient payPalClient, IConfiguration configuration) : ControllerBase
    {
        private readonly PayPalHttpClient _payPalClient = payPalClient;


        [HttpPost("create-paypal-payment")]
        public async Task<IActionResult> CreatePayPalPayment(decimal unitPrice, string productName )
        {
            try
            {
                unitPrice = unitPrice / 1000;
                var orderRequest = new OrderRequest()
                {                
                    Intent = "CAPTURE",
                    PurchaseUnits = new List<PurchaseUnitRequest>()
                    {
                        new PurchaseUnitRequest()
                        {
                            Items = new List<Item>
                            {
                                new Item
                                {
                                    Quantity = "1",
                                    UnitAmount = new Money
                                    {
                                        CurrencyCode = "USD",
                                        Value = unitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                                    },                                
                                    Name = productName
                                }
                            },
                            Amount = new AmountWithBreakdown()
                            {
                                CurrencyCode = "USD",
                                Value = unitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                                Breakdown = new AmountBreakdown()
                                {
                                    ItemTotal = new Money
                                    {
                                        CurrencyCode = "USD",
                                        Value = unitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                                    }
                                }
                            }
                        }
                    },
                    ApplicationContext = new ApplicationContext()
                    {
                        //ReturnUrl = "http://localhost:3000/success",
                        
                        //CancelUrl = "http://localhost:3000/"
                        
                    }
                
                };

                var request = new OrdersCreateRequest();
                request.Headers.Add("prefer", "return=representation");
                request.RequestBody(orderRequest);

            
                var response = await _payPalClient.Execute(request);
                var result = response.Result<Order>();
                
                return Ok(new { id = result.Id, status = result.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{ex.Message} error::: {configuration["PayPalSecretTest"]}");
            }
        }
    }
}

