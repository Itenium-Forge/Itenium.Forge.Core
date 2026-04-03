Itenium.Forge.ExampleCoachingService.Client
=====================================

```sh
dotnet add package Itenium.Forge.ExampleCoachingService.Client
```

Refit client for the CoachingService reference implementation.

## Usage

```csharp
// Program.cs
builder.AddForgeHttpClient<ICoachingServiceClient>("CoachingService");
```

```json
// appsettings.json
"ForgeConfiguration": {
  "HttpClients": {
    "CoachingService": {
      "BaseUrl": "http://localhost:5200"
    }
  }
}
```

## API

| Method | Endpoint | Returns |
|--------|----------|---------|
| `GetCoachesAsync()` | `GET /coaches` | `IReadOnlyList<Coach>` |
| `GetCoachAsync(id)` | `GET /coaches/{id}` | `Coach` |
