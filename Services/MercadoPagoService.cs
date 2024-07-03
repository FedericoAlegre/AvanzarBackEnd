using MercadoPago.Client.CardToken;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Http;
using MercadoPago.Resource.CardToken;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.Preference;
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

        public async Task<Preference> CreatePreferenceAsync(decimal amount, string productName)
        {
            
            try
            {
                // Crea el objeto de request de la preference
                var request = new PreferenceRequest
                {
                    AutoReturn = "approved",
                    BackUrls = new PreferenceBackUrlsRequest
                    {
                        Success = "http://localhost:3000/success"
                    }, 
                    Items = new List<PreferenceItemRequest>
                    {
                        new PreferenceItemRequest
                        {                            
                            Title = productName,
                            Quantity = 1,
                            CurrencyId = "ARS",
                            UnitPrice = amount,                             
                        }
                    }
                    
                };
                
                // Crea la preferencia usando el client
                var client = new PreferenceClient();
                Preference preference = await client.CreateAsync(request);
                return preference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment with MercadoPago.");
                throw new ApplicationException("An error occurred while processing the payment with MercadoPago.", ex);
            }
        }

        public async Task<Preference> GetPreferenceAsync(string preferenceId)
        {

            try
            {
                var client = new PreferenceClient();
                Preference preference = await client.GetAsync(preferenceId);
                
                return preference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preference with MercadoPago.");
                throw new ApplicationException("An error occurred while getting the preference with MercadoPago.", ex);
            }
        }

    }
}
