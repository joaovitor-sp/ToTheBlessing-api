using System.Text.Json.Serialization;
using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using ToTheBlessing.Models;
using ToTheBlessing.DTOs;
using ToTheBlessing.Middlewares;

var builder = WebApplication.CreateSlimBuilder(args);

// Configuração JSON
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// --- Firebase ---
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var serviceAccountKeyPath = config["Firebase:ServiceAccountKeyPath"];
    var projectId = config["Firebase:ProjectId"];

    if (string.IsNullOrEmpty(serviceAccountKeyPath) || string.IsNullOrEmpty(projectId))
        throw new InvalidOperationException("Firebase credentials are not configured in appsettings.json.");

    var absolutePath = Path.Combine(AppContext.BaseDirectory, serviceAccountKeyPath);

    if (!File.Exists(absolutePath))
        throw new FileNotFoundException($"Firebase service account key file not found at: {absolutePath}");

    if (FirebaseApp.DefaultInstance == null)
        return FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(absolutePath),
            ProjectId = projectId
        });

    return FirebaseApp.DefaultInstance;
});

builder.Services.AddSingleton(provider =>
{
    var projectId = provider.GetRequiredService<IConfiguration>()["Firebase:ProjectId"];
    return FirestoreDb.Create(projectId);
});

// --- Controllers e Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Cloudinary ---
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var cloudName = config["Cloudinary:CloudName"];
    var apiKey = config["Cloudinary:ApiKey"];
    var apiSecret = config["Cloudinary:ApiSecret"];

    if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        throw new InvalidOperationException("Cloudinary credentials are not configured in appsettings.json.");

    return new Cloudinary(new Account(cloudName, apiKey, apiSecret));
});

var app = builder.Build();

// --- Cloud Run PORT handling ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");
Console.WriteLine($"Listening on port {port}");

// --- Pipeline ---
app.Services.GetRequiredService<FirebaseApp>();

// if (app.Environment.IsDevelopment())
// {
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
// }

app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

// --- Records e Serializer Context ---
public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(Post))]
[JsonSerializable(typeof(Post[]))]
[JsonSerializable(typeof(PostCreateDto))]
[JsonSerializable(typeof(PostResponseDto))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
