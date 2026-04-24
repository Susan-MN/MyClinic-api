$body = @{
    KeycloakId = 'test-123'
    Username = 'testuser'
    Email = 'test@example.com'
    Role = 'Doctor'
} | ConvertTo-Json -Depth 10

$response = Invoke-RestMethod -Uri 'http://localhost:5000/api/profile/sync' -Method Post -Body $body -ContentType 'application/json'
Write-Output ($response | ConvertTo-Json -Depth 10)
