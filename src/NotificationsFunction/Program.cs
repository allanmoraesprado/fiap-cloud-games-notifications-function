using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationsFunction.Messaging;

var builder = FunctionsApplication.CreateBuilder(args);

// Simulated e-mail senders: they only write to the log (no SMTP, no real provider).
builder.Services.AddSingleton<WelcomeEmailNotifier>();
builder.Services.AddSingleton<PurchaseConfirmationNotifier>();

builder.Build().Run();
