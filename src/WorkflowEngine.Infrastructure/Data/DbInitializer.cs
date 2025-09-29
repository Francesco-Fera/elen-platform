using WorkflowEngine.Core.Entities;

namespace WorkflowEngine.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedCredentialTypesAsync(WorkflowEngineDbContext context)
    {
        if (context.CredentialTypes.Any())
            return;

        var credentialTypes = new[]
        {
            new CredentialType
            {
                Name = "API Key",
                Key = "api_key",
                AuthType = "api_key",
                Description = "Simple API key authentication",
                FieldsJson = """
                [
                    {"name":"api_key","displayName":"API Key","type":"password","required":true}
                ]
                """,
                IsActive = true
            },
            new CredentialType
            {
                Name = "Basic Auth",
                Key = "basic_auth",
                AuthType = "basic_auth",
                Description = "Username and password authentication",
                FieldsJson = """
                [
                    {"name":"username","displayName":"Username","type":"string","required":true},
                    {"name":"password","displayName":"Password","type":"password","required":true}
                ]
                """,
                IsActive = true
            },
            new CredentialType
            {
                Name = "Bearer Token",
                Key = "bearer_token",
                AuthType = "bearer_token",
                Description = "Bearer token authentication",
                FieldsJson = """
                [
                    {"name":"token","displayName":"Token","type":"password","required":true}
                ]
                """,
                IsActive = true
            },
            new CredentialType
            {
                Name = "Google OAuth2",
                Key = "google_oauth2",
                AuthType = "oauth2",
                Description = "Google OAuth2 authentication",
                FieldsJson = """
                [
                    {"name":"client_id","displayName":"Client ID","type":"string","required":true},
                    {"name":"client_secret","displayName":"Client Secret","type":"password","required":true},
                    {"name":"access_token","displayName":"Access Token","type":"password","required":false},
                    {"name":"refresh_token","displayName":"Refresh Token","type":"password","required":false}
                ]
                """,
                IsActive = true
            }
        };

        await context.CredentialTypes.AddRangeAsync(credentialTypes);
        await context.SaveChangesAsync();
    }
}