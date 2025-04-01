using System.Text;

using AutoDarkModeApp.Core.Contracts.Services;

using Newtonsoft.Json;

namespace AutoDarkModeApp.Core.Services;

public class FileService : IFileService
{
    public T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        return default;
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonConvert.SerializeObject(content, Formatting.Indented);
        WriteAllTextWithRetry(Path.Combine(folderPath, fileName), fileContent);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }

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
