using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using StripeOnboardingSlice.Data;
using StripeOnboardingSlice.Features.StartOnboarding;
using StripeOnboardingSlice.Features.StripeWebhooks;
using StripeOnboardingSlice.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IWebhookQueue, WebhookQueue>();
builder.Services.AddHostedService<WebhookProcessorWorker>();
var awsOptions = new BasicAWSCredentials(
    builder.Configuration["AWS:AccessKey"],
    builder.Configuration["AWS:SecretKey"]
);
builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(awsOptions, Amazon.RegionEndpoint.EUNorth1));
builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>();
builder.Services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(awsOptions, Amazon.RegionEndpoint.EUNorth1));

builder.Services.AddSingleton<IWebhookQueue, SqsWebhookQueue>();



builder.Services.AddScoped<StartOnboardingHandler>();
builder.Services.AddScoped<PaymentSucceededHandler>();
builder.Services.AddScoped<SessionExpiredHandler>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IEmailService, MockConsoleEmailService>();
builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapControllers();

app.Run();