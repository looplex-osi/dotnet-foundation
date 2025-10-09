# Script simples para gerar token JWT válido
Write-Host "Gerando token JWT..." -ForegroundColor Green

# Configurações
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

Write-Host "Token JWT gerado:" -ForegroundColor Yellow
Write-Host $jwtToken -ForegroundColor White
Write-Host ""
Write-Host "Testando endpoints..." -ForegroundColor Cyan

# Testar endpoints
$headers = @{ "Authorization" = "Bearer $jwtToken" }

# Teste 1: Health check (publico)
Write-Host "1. Testando /health (publico)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7065/health"
    Write-Host "OK - Health check funcionando" -ForegroundColor Green
} catch {
    Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
}

# Teste 2: ServiceProviderConfig (protegido)
Write-Host "2. Testando /ServiceProviderConfig (protegido)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7065/ServiceProviderConfig" -Headers $headers
    Write-Host "OK - ServiceProviderConfig funcionando" -ForegroundColor Green
} catch {
    Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
}

# Teste 3: Notes (protegido)
Write-Host "3. Testando /notes (protegido)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7065/notes" -Headers $headers
    Write-Host "OK - Notes funcionando" -ForegroundColor Green
} catch {
    Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
}

# Teste 4: Sem autenticação (deve falhar)
Write-Host "4. Testando /notes sem autenticacao (deve falhar)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7065/notes"
    Write-Host "ERRO: Endpoint deveria estar protegido!" -ForegroundColor Red
} catch {
    Write-Host "OK - Endpoint protegido corretamente" -ForegroundColor Green
}

Write-Host ""
Write-Host "Testes concluidos!" -ForegroundColor Magenta
