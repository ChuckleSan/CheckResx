using CheckResx.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CheckResx.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ResxComparisonService _resxComparisonService;

        public IndexModel(ResxComparisonService resxComparisonService)
        {
            _resxComparisonService = resxComparisonService;
        }

        [BindProperty]
        public List<IFormFile> Files { get; set; } = new();

        public List<ComparisonResult>? Results { get; set; } // Changed to nullable
        public List<string> FileNames { get; set; } = new();

        public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> files)
        {
            if (files == null || files.Count < 2)
            {
                ModelState.AddModelError(string.Empty, "Please upload at least 2 .resx files.");
                return Page();
            }

            var fileStreams = files.ToDictionary(f => f.FileName, f => f.OpenReadStream());
            Results = await _resxComparisonService.CompareResxFilesAsync(fileStreams);

            FileNames = files.Select(f => f.FileName).ToList();

            // Dispose streams after processing
            foreach (var stream in fileStreams.Values)
            {
                stream.Dispose();
            }

            return Page();
        }
    }
}