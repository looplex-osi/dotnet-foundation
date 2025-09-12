# Testes E2E Simples para Notejam SCIM API

$baseUrl = "http://localhost:7065"
$passedTests = 0
$failedTests = 0

Write-Host "=== Testes E2E - Notejam SCIM API ===" -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host ""

# Função para testar endpoint
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Endpoint
    )
    
    try {
        $uri = "$baseUrl$Endpoint"
        Write-Host "Testing: $Name" -ForegroundColor Cyan
        
        $response = Invoke-RestMethod -Uri $uri -Method $Method -ErrorAction Stop
        
        Write-Host "  PASSED" -ForegroundColor Green
        $script:passedTests++
        return $true
    }
    catch {
        Write-Host "  FAILED: $($_.Exception.Message)" -ForegroundColor Red
        $script:failedTests++
        return $false
    }
}

# Testes básicos
Write-Host "1. Testes de Conectividade:" -ForegroundColor Magenta
Test-Endpoint -Name "GET /pads" -Method "GET" -Endpoint "/pads"
Test-Endpoint -Name "GET /notes" -Method "GET" -Endpoint "/notes"

Write-Host "`n2. Testes de Filtros SCIM:" -ForegroundColor Magenta
Test-Endpoint -Name "GET /pads with filter" -Method "GET" -Endpoint "/pads?filter=active eq true"
Test-Endpoint -Name "GET /pads with status filter" -Method "GET" -Endpoint "/pads?filter=status eq 1"
Test-Endpoint -Name "GET /pads with name filter" -Method "GET" -Endpoint "/pads?filter=name co 'Teste'"

Write-Host "`n3. Testes de Paginação:" -ForegroundColor Magenta
Test-Endpoint -Name "GET /pads with pagination" -Method "GET" -Endpoint "/pads?startIndex=1&count=5"

Write-Host "`n4. Testes de Filtros Complexos:" -ForegroundColor Magenta
Test-Endpoint -Name "GET /pads with complex filter" -Method "GET" -Endpoint "/pads?filter=active eq true and status eq 1"

Write-Host "`n5. Testes de Notas:" -ForegroundColor Magenta
Test-Endpoint -Name "GET /notes with filter" -Method "GET" -Endpoint "/notes?filter=active eq true"

# Resumo
Write-Host "`n=== RESUMO ===" -ForegroundColor Magenta
Write-Host "Passados: $passedTests" -ForegroundColor Green
Write-Host "Falharam: $failedTests" -ForegroundColor Red
Write-Host "Total: $($passedTests + $failedTests)" -ForegroundColor Yellow

if ($failedTests -eq 0) {
    Write-Host "`nSUCCESS: Todos os testes passaram!" -ForegroundColor Green
} else {
    Write-Host "`nWARNING: Alguns testes falharam!" -ForegroundColor Red
}
