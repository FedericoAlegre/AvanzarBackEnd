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


var builder = WebApplication.CreateBuilder(args);

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
var awsOptions = builder.Configuration.GetAWSOptions();
var accessKey = Environment.GetEnvironmentVariable("AWSAccessKey");
var secretKeyAWS = Environment.GetEnvironmentVariable("AWSSecretKey");
var region = builder.Configuration["AWS:Region"];

// Configurar las credenciales de AWS manualmente
var awsCredentials = new BasicAWSCredentials(accessKey, secretKeyAWS);
var s3ClientConfig = new AmazonS3Config
{
    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
};

// Añadir el cliente S3 con las credenciales configuradas manualmente
builder.Services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(awsCredentials, s3ClientConfig));


// Configurar SendGrid manualmente
var sengrid = Environment.GetEnvironmentVariable("sendgrid");

if(sengrid != null) builder.Services.AddSingleton<ISendGridClient>(new SendGridClient(sengrid));
builder.Services.AddScoped<EmailService>();

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
