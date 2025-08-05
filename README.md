Itenium.Forge.Core
==================

- Github > Profile > Settings > Developer settings
  - Personal access tokens > Tokens (classic)
  - Generate new token (classic)
    - Scopes: `read:packages`


```sh
dotnet nuget add source --username OWNER --password TOKEN --store-password-in-clear-text --name itenium "https://nuget.pkg.github.com/Itenium-Forge/index.json"
```


## Packages

- Itenium.Forge.Settings
