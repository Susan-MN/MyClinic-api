$response = Invoke-RestMethod -Uri 'http://localhost:5000/WeatherForecast' -Method Get
Write-Output ($response | ConvertTo-Json -Depth 10)
