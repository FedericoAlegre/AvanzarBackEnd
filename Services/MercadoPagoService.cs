using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;


namespace AvanzarBackEnd.Services
{
    public class MercadoPagoService
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<MercadoPagoService> _logger;

        public MercadoPagoService(IConfiguration configuration, ILogger<MercadoPagoService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            MercadoPagoConfig.AccessToken = _configuration["MercadoPagoTestAccessToken"];
        }

        public async Task<Payment> CreatePaymentAsync(decimal amount, string paymentMethod, string description, string payerEmail)
        {
            try
            {
                var paymentRequest = new PaymentCreateRequest
                {
                    
                    TransactionAmount = amount,
                    Description = description,
                    PaymentMethodId = paymentMethod, // or another method
                    Payer = new PaymentPayerRequest
                    {
                        Email = payerEmail // Example email
                    }
                };

                var client = new PaymentClient();
                Payment payment = await client.CreateAsync(paymentRequest);
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment with MercadoPago.");
                throw new ApplicationException("An error occurred while processing the payment with MercadoPago.", ex);
            }
        }
    }
}
