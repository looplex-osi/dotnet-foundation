# Testes E2E para Notejam SCIM API
# Baseado na collection: samples/Notejam/Notejam_SCIM_Collection.json

$baseUrl = "http://localhost:7065"
$testResults = @()
$passedTests = 0
$failedTests = 0

Write-Host "🚀 Iniciando Testes E2E - Notejam SCIM API" -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host ""

# Função para executar teste
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200
    )
    
    try {
        $uri = "$baseUrl$Endpoint"
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $Headers
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
            $params.Headers["Content-Type"] = "application/json"
        }
        
        $response = Invoke-RestMethod @params -ErrorAction Stop
        
        $statusCode = $response.StatusCode
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "✅ $Name - PASSED" -ForegroundColor Green
            $script:passedTests++
            return $true
        } else {
            Write-Host "❌ $Name - FAILED (Expected: $ExpectedStatus, Got: $statusCode)" -ForegroundColor Red
            $script:failedTests++
            return $false
        }
    }
    catch {
        Write-Host "❌ $Name - FAILED (Error: $($_.Exception.Message))" -ForegroundColor Red
        $script:failedTests++
        return $false
    }
}

# 1. Testes básicos de conectividade
Write-Host "📋 1. Testes de Conectividade" -ForegroundColor Cyan

Test-Endpoint -Name "GET /pads - Listar todos os pads" -Method "GET" -Endpoint "/pads"
Test-Endpoint -Name "GET /notes - Listar todas as notas" -Method "GET" -Endpoint "/notes"

# 2. Testes de filtros SCIM
Write-Host "`n📋 2. Testes de Filtros SCIM" -ForegroundColor Cyan

Test-Endpoint -Name "GET /pads?filter=active eq true" -Method "GET" -Endpoint "/pads?filter=active eq true"
Test-Endpoint -Name "GET /pads?filter=status eq 1" -Method "GET" -Endpoint "/pads?filter=status eq 1"
Test-Endpoint -Name "GET /pads?filter=name co 'Teste'" -Method "GET" -Endpoint "/pads?filter=name co 'Teste'"
Test-Endpoint -Name "GET /pads?filter=active eq true and status eq 1" -Method "GET" -Endpoint "/pads?filter=active eq true and status eq 1"

# 3. Testes de paginação
Write-Host "`n📋 3. Testes de Paginação" -ForegroundColor Cyan

Test-Endpoint -Name "GET /pads?startIndex=1 and count=5" -Method "GET" -Endpoint "/pads?startIndex=1`&count=5"
Test-Endpoint -Name "GET /pads?startIndex=6 and count=5" -Method "GET" -Endpoint "/pads?startIndex=6`&count=5"

# 4. Testes de filtros complexos
Write-Host "`n📋 4. Testes de Filtros Complexos" -ForegroundColor Cyan

Test-Endpoint -Name "GET /pads?filter=(active eq true) and (status eq 1)" -Method "GET" -Endpoint "/pads?filter=(active eq true) and (status eq 1)"
Test-Endpoint -Name "GET /pads?filter=active eq true or status eq 2" -Method "GET" -Endpoint "/pads?filter=active eq true or status eq 2"
Test-Endpoint -Name "GET /pads?filter=not (active eq false)" -Method "GET" -Endpoint "/pads?filter=not (active eq false)"

# 5. Testes de filtros de data
Write-Host "`n📋 5. Testes de Filtros de Data" -ForegroundColor Cyan

$today = Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ"
Test-Endpoint -Name "GET /pads?filter=created gt '$today'" -Method "GET" -Endpoint "/pads?filter=created gt '$today'"

# 6. Testes de filtros de texto
Write-Host "`n📋 6. Testes de Filtros de Texto" -ForegroundColor Cyan

Test-Endpoint -Name "GET /pads?filter=name sw 'Pad'" -Method "GET" -Endpoint "/pads?filter=name sw 'Pad'"
Test-Endpoint -Name "GET /pads?filter=name ew 'E2E'" -Method "GET" -Endpoint "/pads?filter=name ew 'E2E'"

# 7. Testes de filtros de notas
Write-Host "`n📋 7. Testes de Filtros de Notas" -ForegroundColor Cyan

Test-Endpoint -Name "GET /notes?filter=active eq true" -Method "GET" -Endpoint "/notes?filter=active eq true"
Test-Endpoint -Name "GET /notes?filter=name co 'Teste'" -Method "GET" -Endpoint "/notes?filter=name co 'Teste'"

# 8. Testes de filtros + paginação
Write-Host "`n📋 8. Testes de Filtros + Paginação" -ForegroundColor Cyan

Test-Endpoint -Name "GET /pads?filter=active eq true and startIndex=1 and count=3" -Method "GET" -Endpoint "/pads?filter=active eq true`&startIndex=1`&count=3"

# Resumo dos resultados
Write-Host "`n📊 RESUMO DOS TESTES E2E" -ForegroundColor Magenta
Write-Host "================================" -ForegroundColor Magenta
Write-Host "✅ Testes Passados: $passedTests" -ForegroundColor Green
Write-Host "❌ Testes Falharam: $failedTests" -ForegroundColor Red
Write-Host "📈 Total de Testes: $($passedTests + $failedTests)" -ForegroundColor Yellow

if ($failedTests -eq 0) {
    Write-Host "`n🎉 TODOS OS TESTES E2E PASSARAM!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n⚠️  ALGUNS TESTES E2E FALHARAM!" -ForegroundColor Red
    exit 1
}
