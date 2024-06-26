using Azure;
using Azure.Communication;
using Azure.Communication.Identity;
using Azure.Core;

namespace BizAssistWebApp.Controllers.Services;

public class CommunicationTokenService(string connectionString)
{
    private readonly CommunicationIdentityClient _identityClient = new(connectionString);

    public async Task<string> GenerateTokenAsync()
    {
        Response<CommunicationUserIdentifier>? user = await _identityClient.CreateUserAsync();
        Response<AccessToken>? tokenResponse = await _identityClient.GetTokenAsync(user, scopes: new[] { CommunicationTokenScope.VoIP });
        return tokenResponse.Value.Token;
    }
}