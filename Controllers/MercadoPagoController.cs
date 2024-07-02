using AvanzarBackEnd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AvanzarBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MercadoPagoController : ControllerBase
    {
        private readonly MercadoPagoService _mercadoPagoService;
        private readonly ILogger<MercadoPagoController> _logger;

        public MercadoPagoController(MercadoPagoService mercadoPagoService, ILogger<MercadoPagoController> logger)
        {
            _mercadoPagoService = mercadoPagoService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment(decimal amount, string paymentMethod, string description, string payerEmail)
        {
            try
            {
                var payment = await _mercadoPagoService.CreatePaymentAsync(amount, paymentMethod, description, payerEmail);
                return Ok(payment);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error occurred while creating payment.");
                return StatusCode(500, $"{ex.Message}::  {Environment.GetEnvironmentVariable("MercadoPagoTestAccessToken")}");
            }
        }
    }
}

