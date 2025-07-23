using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoDarkModeApp.Contracts.Services;

namespace AutoDarkModeApp.Services;

public class FileService : IFileService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public T? Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON from file: {path}", ex);
        }
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        //if (!Directory.Exists(folderPath))
        //{
            Directory.CreateDirectory(folderPath);
        //}

        var fileContent = JsonSerializer.Serialize(content, _jsonOptions);
        WriteAllTextWithRetry(Path.Combine(folderPath, fileName), fileContent);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }

    // Question: Why do we need to retry writing the file? And why create a function for it? When it's only used here? [Jay]
    private static void WriteAllTextWithRetry(string filePath, string content, int maxRetries = 5, int delayMs = 500)
    {
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                File.WriteAllText(filePath, content, Encoding.UTF8);
                return;
            }
            catch (IOException)
            {
                if (attempt == maxRetries - 1)
                    throw;
                Thread.Sleep(delayMs);
            }
        }
    }
}
