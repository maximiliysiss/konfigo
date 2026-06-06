using Konfigo.Client.Extensions;
using Konfigo.Sample.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddRealtimeConfig();

builder.Services.AddRealtimeConfig();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/options", (IOptionsSnapshot<TestOptions> options) => Results.Ok(options.Value));

app.Run();
