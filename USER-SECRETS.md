# Using .NET User Secrets (Recommended for Development)

Instead of storing your API credentials in `appsettings.json`, you can use .NET User Secrets for better security during development.

## Setup User Secrets

1. **Initialize User Secrets** (run in project directory):
   ```bash
   dotnet user-secrets init
   ```

2. **Add Your Spotify Credentials**:
   ```bash
   dotnet user-secrets set "Spotify:ClientId" "your-spotify-client-id"
   dotnet user-secrets set "Spotify:ClientSecret" "your-spotify-client-secret"
   ```

3. **Add Your Gemini API Key** (optional):
   ```bash
   dotnet user-secrets set "Gemini:ApiKey" "your-gemini-api-key"
   ```

## View Your Secrets

```bash
dotnet user-secrets list
```

## Remove a Secret

```bash
dotnet user-secrets remove "Spotify:ClientId"
```

## Clear All Secrets

```bash
dotnet user-secrets clear
```

## Benefits of User Secrets

? Credentials are stored outside your project folder
? Won't be accidentally committed to Git
? Separate from production configuration
? Works automatically in development environment

## For Production

User Secrets only work in Development environment. For production, use:

- **Environment Variables**
- **Azure Key Vault**
- **AWS Secrets Manager**
- **Docker Secrets**
- **Kubernetes Secrets**

## Where Are Secrets Stored?

**Windows:**
```
%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json
```

**macOS/Linux:**
```
~/.microsoft/usersecrets/<user_secrets_id>/secrets.json
```

The `<user_secrets_id>` is defined in your `.csproj` file after running `dotnet user-secrets init`.
