using CheckResx.Services;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddScoped<ResxComparisonService>();

// Add logging for diagnostics
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // Show detailed errors in development
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

// Set default route to Index
app.MapGet("/", context =>
{
    context.Response.Redirect("/Index");
    return Task.CompletedTask; // Return Task for consistency, no async needed
});

app.MapRazorPages();

// Minimal API endpoint for file upload and comparison
app.MapPost("/api/compare-resx", async (IFormFileCollection files, ResxComparisonService service) =>
{
    if (files.Count < 2)
    {
        return Results.BadRequest("Please upload at least 2 .resx files.");
    }

    var results = new List<ComparisonResult>();
    var fileStreams = new Dictionary<string, Stream>();

    try
    {
        foreach (var file in files)
        {
            if (!file.FileName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("Only .resx files are supported.");
            }
            fileStreams[file.FileName] = file.OpenReadStream();
        }

        results = await service.CompareResxFilesAsync(fileStreams);
        return Results.Ok(results);
    }
    finally
    {
        foreach (var stream in fileStreams.Values)
        {
            stream.Dispose();
        }
    }
}).Accepts<IFormFileCollection>("multipart/form-data");

// Ensure the app runs until stopped
app.Run();