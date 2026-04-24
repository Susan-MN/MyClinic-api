Write-Host 'Executing all API requests...' -ForegroundColor Green

Write-Host '1. Get WeatherForecast' -ForegroundColor Yellow
& ./scripts/Get-Weather.ps1

Write-Host '2. Sync Profile' -ForegroundColor Yellow
& ./scripts/Sync-Profile.ps1

Write-Host 'All requests completed!' -ForegroundColor Green
