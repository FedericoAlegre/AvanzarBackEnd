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
        public async Task<IActionResult> CreatePreference(decimal amount, string productName)
        {
            try
            {
                if(amount<=0 ||  productName is null ) throw new Exception("All fields are required");
                var preference = await _mercadoPagoService.CreatePreferenceAsync(amount, productName);
                return Ok(preference);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Error occurred while creating payment.");
                return StatusCode(500, $"{ex.Message}::  {Environment.GetEnvironmentVariable("MercadoPagoTestAccessToken")}");
            }
        }
    }
}

