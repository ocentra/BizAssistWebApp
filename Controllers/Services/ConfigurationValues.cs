using System.Text.Json;

namespace BizAssistWebApp.Controllers.Services
{
    public class ConfigurationValues
    {
        public string CommunicationServicesConnectionString { get; }
        public string SpeechKey { get; }
        public string SpeechRegion { get; }
        public string OpenAIEndpoint { get; }
        public string OpenAIKey { get; }
        public string AssistantIds { get; }

        private readonly IConfiguration _configuration;

        public ConfigurationValues(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;

            // Perform the validation based on the environment
            if (environment.IsDevelopment())
            {
                ValidateConfigurationVariables();
            }
            else
            {
                ValidateEnvironmentVariables();
            }

            CommunicationServicesConnectionString = environment.IsDevelopment() ? GetConfigurationVariable("Azure:CommunicationServicesConnectionString") : GetEnvironmentVariable("AZURE_COMMUNICATION_SERVICES_CONNECTION_STRING");
            SpeechKey = environment.IsDevelopment() ? GetConfigurationVariable("Azure:SpeechKey") : GetEnvironmentVariable("AZURE_SPEECH_KEY");
            SpeechRegion = environment.IsDevelopment() ? GetConfigurationVariable("Azure:SpeechRegion") : GetEnvironmentVariable("AZURE_SPEECH_REGION");
            OpenAIEndpoint = environment.IsDevelopment() ? GetConfigurationVariable("Azure:OpenAI:Endpoint") : GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            OpenAIKey = environment.IsDevelopment() ? GetConfigurationVariable("Azure:OpenAI:ApiKey") : GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            AssistantIds = environment.IsDevelopment() ? GetConfigurationVariable("Azure:OpenAI:AssistantIds") : GetEnvironmentVariable("AZURE_OPENAI_ASSISTANT_IDS");
        }

        private string GetEnvironmentVariable(string key)
        {
            string? envValue = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(envValue))
            {
                throw new InvalidOperationException($"Configuration variable '{key}' or environment variable '{key}' is not set.");
            }
            return envValue;
        }

        private string GetConfigurationVariable(string key)
        {
            var section = _configuration.GetSection(key);

            // Check if the section is an array
            if (section.GetChildren().Any())
            {
                // Serialize the section to JSON string
                return JsonSerializer.Serialize(section.GetChildren().ToDictionary(x => x.Key, x => x.Value));
            }

            // If it's not an array, just get the value directly
            string? value = section.Value;
            if (string.IsNullOrEmpty(value))
            {
                // Try to get the value from environment variables
                value = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException($"Configuration variable '{key}' not found in appsettings or environment variables.");
                }
            }
            return value;
        }

        void ValidateConfigurationVariables()
        {
            string[] requiredVariables = new[]
            {
                "Azure:CommunicationServicesConnectionString",
                "Azure:SpeechKey",
                "Azure:SpeechRegion",
                "Azure:OpenAI:ApiKey",
                "Azure:OpenAI:Endpoint",
                "Azure:OpenAI:AssistantIds"
            };

            foreach (string variable in requiredVariables)
            {
                if (string.IsNullOrEmpty(GetConfigurationVariable(variable)))
                {
                    throw new InvalidOperationException($"Configuration variable '{variable}' is not set.");
                }
            }
        }

        void ValidateEnvironmentVariables()
        {
            string[] requiredVariables = new[]
            {
                "AZURE_COMMUNICATION_SERVICES_CONNECTION_STRING",
                "AZURE_SPEECH_KEY",
                "AZURE_SPEECH_REGION",
                "AZURE_OPENAI_API_KEY",
                "AZURE_OPENAI_ENDPOINT",
                "AZURE_OPENAI_ASSISTANT_IDS"
            };

            foreach (string variable in requiredVariables)
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
                {
                    throw new InvalidOperationException($"Environment variable '{variable}' is not set.");
                }
            }
        }
    }
}
