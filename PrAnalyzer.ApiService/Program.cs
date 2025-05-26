using PrAnalyzer.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add OpenAPI/Swagger with enhanced documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PR Review Analyzer API",
        Version = "v1",
        Description = "API for extracting and analyzing pull request review comments from GitHub repositories",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "PR Review Analyzer",
            Url = new Uri("https://github.com/your-org/pr-analyzer")
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add Redis caching
builder.AddRedisClient("cache");

// Add distributed memory cache
builder.Services.AddDistributedMemoryCache();

// Add HTTP client for GitHub API
builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "PR-Review-Analyzer/1.0");
});

// Add HTTP client for Hugging Face
builder.Services.AddHttpClient<HuggingFaceService>();

// Add services
builder.Services.AddScoped<GitHubService>();
builder.Services.AddScoped<HuggingFaceService>();
builder.Services.AddScoped<CommentClassificationService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Add Scalar API Documentation
    //app.MapScalarUi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();