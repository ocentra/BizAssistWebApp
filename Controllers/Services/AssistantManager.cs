using System.Text.Json;

namespace BizAssistWebApp.Controllers.Services;

public class AssistantManager
{
    private readonly Dictionary<string, string>? _assistants;

    public AssistantManager(string assistantsJson)
    {
        _assistants = ParseAssistantsJson(assistantsJson);
    }

    private Dictionary<string, string> ParseAssistantsJson(string json)
    {
        Dictionary<string, string> assistants = new Dictionary<string, string>();
        List<AssistantInfo>? assistantsArray = JsonSerializer.Deserialize<List<AssistantInfo>>(json);

        if (assistantsArray != null)
        {
            foreach (AssistantInfo assistant in assistantsArray)
            {
                assistants.Add(assistant.name, assistant.id);
            }
        }


        return assistants;
    }

    public string GetAssistantId(string assistantName)
    {
        if (_assistants != null && _assistants.TryGetValue(assistantName, out string? assistantId))
        {
            return assistantId;
        }
        else
        {
            throw new Exception($"Assistant ID for {assistantName} not found.");
        }
    }

    public string GetFirstOrDefaultAssistantId()
    {
        if (_assistants is { Count: > 0 })
        {
            return _assistants.Values.First();
        }
        else
        {
            throw new Exception("No assistants found.");
        }
    }
}

public class AssistantInfo
{
    public string name { get; set; }
    public string id { get; set; }
}