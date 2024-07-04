using AvanzarBackEnd.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using SendGrid;
using AvanzarBackEnd.Services;
using System.Diagnostics;
using Amazon.S3;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Extensions.NETCore.Setup;
using Amazon;
using PayPalCheckoutSdk.Core;


var builder = WebApplication.CreateBuilder(args);

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Crear un logger factory
var loggerFactory = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddConsole();
});
var logger = loggerFactory.CreateLogger<Program>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

//Auth
builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddEnvironmentVariables();
var secretKey = builder.Configuration.GetSection("settings").GetSection("secretKey").ToString()!;
var keyBytes = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(config =>
{
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(config =>
{
    config.RequireHttpsMetadata = false;
    config.SaveToken = true;
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false

    };
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<MercadoPagoService>();
builder.Services.AddLogging(); // Add logging
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection String not found"));
});

builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Leer las credenciales de AWS desde la configuración

//var accessKey = Environment.GetEnvironmentVariable("AWSAccessKey");
//var secretKeyAWS = Environment.GetEnvironmentVariable("AWSSecretKey");
var accessKey = builder.Configuration["AWSAccessKey"];
var secretKeyAWS = builder.Configuration["AWSSecretKey"];
if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKeyAWS))
{
    logger.LogError("AWS Access Key or Secret Key is missing");
}
else
{
    logger.LogInformation("AWS Access Key and Secret Key loaded successfully");
}

var awsOptions = new AWSOptions
{
    Credentials = new BasicAWSCredentials(accessKey, secretKeyAWS),
    Region = RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"])
};

builder.Services.AddAWSService<IAmazonS3>(awsOptions);


// Configurar SendGrid manualmente
var sendgrid = Environment.GetEnvironmentVariable("sendgrid");
if (sendgrid.IsNullOrEmpty())
{
    logger.LogError("SendGrid API Key is missing");
}
else
{
    logger.LogInformation("SendGrid API Key loaded successfully");
}

if (sendgrid != null) builder.Services.AddSingleton<ISendGridClient>(new SendGridClient(sendgrid));
builder.Services.AddScoped<EmailService>();

// Configura PayPal
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var clientId = configuration["PayPal:ClientId"];
    var clientSecret = "EAw7TnRUnzst3C6Bj72nnc_fmUxLIk54PShFHpEGiApyPO5skAZkC3CIiP2pdTwFTNr4X9Z0Lb9xaPok";//builder.Configuration["PayPalSecretTest"];
    var environment = configuration["PayPal:Environment"];

    PayPalEnvironment payPalEnvironment = environment switch
    {
        "live" => new LiveEnvironment(clientId, clientSecret),
        _ => new SandboxEnvironment(clientId, clientSecret)
    };

    return new PayPalHttpClient(payPalEnvironment);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", async context =>
{
    var s3Client = context.RequestServices.GetRequiredService<IAmazonS3>();
    var buckets = await s3Client.ListBucketsAsync();
    await context.Response.WriteAsJsonAsync(buckets.Buckets);
});

app.UseCors("AllowAllOrigins");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
