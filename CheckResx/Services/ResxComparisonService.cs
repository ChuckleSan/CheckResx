using System.Collections.Concurrent;
using System.Xml.Linq;

namespace CheckResx.Services
{
    public class ResxComparisonService
    {
        public async Task<List<ComparisonResult>> CompareResxFilesAsync(Dictionary<string, Stream> fileStreams)
        {
            var results = new List<ComparisonResult>();
            var allKeys = new ConcurrentBag<string>();
            var resourceSets = new Dictionary<string, Dictionary<string, string>>();

            // Parse each .resx file using XDocument
            foreach (var file in fileStreams)
            {
                var resourceSet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using var memoryStream = new MemoryStream();
                await file.Value.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using var reader = new StreamReader(memoryStream);
                var doc = XDocument.Load(reader);

                // Parse <data> elements
                foreach (var dataElement in doc.Root!.Elements("data"))
                {
                    var name = dataElement.Attribute("name")?.Value;
                    var value = dataElement.Element("value")?.Value;

                    if (!string.IsNullOrEmpty(name) && value != null)
                    {
                        resourceSet[name] = value;
                        allKeys.Add(name);
                    }
                }

                resourceSets[file.Key] = resourceSet;
            }

            // Find unique keys across all files
            var uniqueKeys = allKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Compare keys and values
            foreach (var key in uniqueKeys)
            {
                var result = new ComparisonResult { Key = key };
                foreach (var file in fileStreams.Keys)
                {
                    if (resourceSets[file].TryGetValue(key, out string? value))
                    {
                        result.Values[file] = value;
                    }
                    else
                    {
                        result.MissingFrom.Add(file);
                    }
                }
                results.Add(result);
            }

            return results;
        }
    }

    public class ComparisonResult
    {
        public string Key { get; set; } = string.Empty;
        public Dictionary<string, string> Values { get; } = new();
        public List<string> MissingFrom { get; } = new();
    }
}