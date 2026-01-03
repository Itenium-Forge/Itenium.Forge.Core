param(
    [string]$BaseUrl = "http://localhost:17128",
    [string]$Username = "user",
    [string]$Password = "UserPassword123!"
)

# Or: admin / AdminPassword123!

$body = @{
    grant_type = "password"
    client_id = "forge-spa"
    username = $Username
    password = $Password
    scope = "openid profile email"
}

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/connect/token" -Method Post -Body $body

    Write-Host "`nToken received successfully!`n" -ForegroundColor Green
    Write-Host "Access Token:" -ForegroundColor Cyan
    Write-Host $response.access_token
    Write-Host "`nToken Type: $($response.token_type)"
    Write-Host "Expires In: $($response.expires_in) seconds"

    # Copy to clipboard
    $response.access_token | Set-Clipboard
    Write-Host "`nToken copied to clipboard!" -ForegroundColor Yellow
}
catch {
    Write-Host "Error getting token: $_" -ForegroundColor Red
}
