using System.Text.Json;
using Replay.Models;

namespace Replay.Data;

public class HandHistoryLoader(IWebHostEnvironment env)
{
    public async Task<HandHistoryFile?> LoadAsync()
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "dados.txt");
        var json = await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize<HandHistoryFile>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
