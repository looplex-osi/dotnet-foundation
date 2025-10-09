# Simple script to generate valid JWT token
Write-Host "Generating JWT token..." -ForegroundColor Green

# Configuration
$issuer = "https://localhost:7065"
$audience = "notejam-api"
$secretKey = "your-256-bit-secret-key-for-notejam-development-change-in-production"

# Header
$header = '{"alg":"HS256","typ":"JWT"}'
$headerB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+', '-').Replace('/', '_')

# Payload
$currentTime = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$expTime = $currentTime + 3600  # 1 hora
$payload = @{
    sub = "user123"
    name = "Test User"
    email = "test@notejam.com"
    iat = $currentTime
    exp = $expTime
    iss = $issuer
    aud = $audience
} | ConvertTo-Json -Compress

$payloadB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_')

# Signature
$signatureInput = "$headerB64.$payloadB64"
$hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($secretKey))
$signature = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signatureInput))
$signatureB64 = [Convert]::ToBase64String($signature).TrimEnd('=').Replace('+', '-').Replace('/', '_')

# Token final
$jwtToken = "$headerB64.$payloadB64.$signatureB64"

Write-Host "JWT token generated:" -ForegroundColor Yellow
Write-Host $jwtToken -ForegroundColor White
Write-Host ""
Write-Host "Testing endpoints..." -ForegroundColor Cyan

# Test endpoints
$headers = @{ "Authorization" = "Bearer $jwtToken" }

# Test 1: Health check (public)
Write-Host "1. Testing /health (public)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7065/health" -Headers $headers
    Write-Host "OK - Health check protected and working" -ForegroundColor Green
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: ServiceProviderConfig (protected)
Write-Host "2. Testing /ServiceProviderConfig (protected)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7065/ServiceProviderConfig" -Headers $headers
    Write-Host "OK - ServiceProviderConfig working" -ForegroundColor Green
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Notes (protected)
Write-Host "3. Testing /notes (protected)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7065/notes" -Headers $headers
    Write-Host "OK - Notes working" -ForegroundColor Green
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Without authentication (should fail)
Write-Host "4. Testing /notes without authentication (should fail)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7065/notes"
    Write-Host "ERROR: Endpoint should be protected!" -ForegroundColor Red
} catch {
    if ($_.Exception.Message -like "*401*") {
        Write-Host "Failed without authentication (401): OK - Endpoint protected correctly!" -ForegroundColor Green
    } else {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Tests completed!" -ForegroundColor Magenta
