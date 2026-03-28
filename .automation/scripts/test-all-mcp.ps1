$ErrorActionPreference = "Continue"
$base = "http://localhost:5119/api/stocks"
$sym = "sh600519"
$tid = "pm-mcp-audit-20260327"
$results = @()

function Test-McpEndpoint {
    param([string]$Name, [string]$Url)
    Write-Host "`n========== $Name ==========" -ForegroundColor Cyan
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $r = Invoke-RestMethod -Uri $Url -TimeoutSec 60
        $sw.Stop()
        
        $dataJson = ""
        if ($r.data) { $dataJson = ($r.data | ConvertTo-Json -Depth 4 -Compress) }
        $evidJson = ""
        if ($r.evidence -and $r.evidence.Count -gt 0) { 
            $first = $r.evidence[0] | ConvertTo-Json -Depth 3 -Compress
            $evidJson = $first
        }
        
        $result = [PSCustomObject]@{
            Name = $Name
            Status = "OK"
            TraceId = $r.traceId
            ToolName = $r.toolName
            LatencyMs = $r.latencyMs
            CacheHit = $r.cache.hit
            CacheSource = $r.cache.source
            WarningCount = if($r.warnings){$r.warnings.Count}else{0}
            DegradedCount = if($r.degradedFlags){$r.degradedFlags.Count}else{0}
            EvidenceCount = if($r.evidence){$r.evidence.Count}else{0}
            DataJson = $dataJson
            FirstEvidence = $evidJson
            Warnings = if($r.warnings){$r.warnings -join "; "}else{""}
            DegradedFlags = if($r.degradedFlags){$r.degradedFlags -join "; "}else{""}
        }
        
        Write-Host "  Status: OK | TraceId: $($r.traceId)" -ForegroundColor Green
        Write-Host "  LatencyMs: $($r.latencyMs) | CacheHit: $($r.cache.hit) | CacheSource: $($r.cache.source)"
        Write-Host "  EvidenceCount: $($result.EvidenceCount) | Warnings: $($result.WarningCount) | Degraded: $($result.DegradedCount)"
        if ($r.warnings -and $r.warnings.Count -gt 0) { Write-Host "  Warnings: $($r.warnings -join '; ')" -ForegroundColor Yellow }
        if ($r.degradedFlags -and $r.degradedFlags.Count -gt 0) { Write-Host "  DegradedFlags: $($r.degradedFlags -join '; ')" -ForegroundColor Yellow }
        $truncData = if($dataJson.Length -gt 300) { $dataJson.Substring(0,300) + "..." } else { $dataJson }
        Write-Host "  Data(300c): $truncData"
        
        return $result
    }
    catch {
        Write-Host "  Status: FAIL" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        return [PSCustomObject]@{
            Name = $Name; Status = "FAIL"; TraceId = ""; ToolName = ""; LatencyMs = 0
            CacheHit = $false; CacheSource = ""; WarningCount = 0; DegradedCount = 0
            EvidenceCount = 0; DataJson = ""; FirstEvidence = ""; Warnings = $_.Exception.Message; DegradedFlags = ""
        }
    }
}

Write-Host "=== MCP Endpoint Audit - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" -ForegroundColor Yellow
Write-Host "Symbol: $sym | Backend: $base" -ForegroundColor Yellow

# Test all 11 MCP endpoints
$results += Test-McpEndpoint "CompanyOverviewMcp" "$base/mcp/company-overview?symbol=$sym&taskId=$tid"
$results += Test-McpEndpoint "StockProductMcp" "$base/mcp/product?symbol=$sym&taskId=$tid"
$results += Test-McpEndpoint "StockFundamentalsMcp" "$base/mcp/fundamentals?symbol=$sym&taskId=$tid"
$results += Test-McpEndpoint "StockShareholderMcp" "$base/mcp/shareholder?symbol=$sym&taskId=$tid"
$results += Test-McpEndpoint "MarketContextMcp" "$base/mcp/market-context?symbol=$sym&taskId=$tid"
$results += Test-McpEndpoint "SocialSentimentMcp" "$base/mcp/social-sentiment?symbol=$sym&taskId=$tid"
$results += Test-McpEndpoint "StockKlineMcp" "$base/mcp/kline?symbol=$sym&taskId=$tid&interval=day&count=30"
$results += Test-McpEndpoint "StockMinuteMcp" "$base/mcp/minute?symbol=$sym&taskId=$tid"
$results += Test-McpEndpoint "StockStrategyMcp" "$base/mcp/strategy?symbol=$sym&taskId=$tid&interval=day&count=30"
$results += Test-McpEndpoint "StockNewsMcp" "$base/mcp/news?symbol=$sym&taskId=$tid&level=stock"
$results += Test-McpEndpoint "StockSearchMcp" "$base/mcp/search?query=%E8%B4%B5%E5%B7%9E%E8%8C%85%E5%8F%B0&taskId=$tid&trustedOnly=true"

Write-Host "`n`n=== SUMMARY ===" -ForegroundColor Yellow
$results | Format-Table Name, Status, LatencyMs, CacheHit, EvidenceCount, WarningCount, DegradedCount -AutoSize

# Output full JSON results for report generation
$outputPath = "c:\Users\kong\AiAgent\.automation\scripts\mcp-audit-results.json"
$results | ConvertTo-Json -Depth 5 | Out-File -FilePath $outputPath -Encoding utf8
Write-Host "`nFull results saved to: $outputPath" -ForegroundColor Green

# Output detailed data per endpoint
Write-Host "`n`n=== DETAILED DATA PER ENDPOINT ===" -ForegroundColor Yellow
foreach ($r in $results) {
    Write-Host "`n--- $($r.Name) ---" -ForegroundColor Cyan
    Write-Host "Status: $($r.Status) | Latency: $($r.LatencyMs)ms | Cache: $($r.CacheHit)/$($r.CacheSource)"
    Write-Host "Evidence: $($r.EvidenceCount) | Warnings: $($r.WarningCount) | Degraded: $($r.DegradedCount)"
    if ($r.DegradedFlags) { Write-Host "DegradedFlags: $($r.DegradedFlags)" -ForegroundColor Yellow }
    if ($r.Warnings -and $r.Status -eq "FAIL") { Write-Host "Error: $($r.Warnings)" -ForegroundColor Red }
    $truncData = if($r.DataJson.Length -gt 500) { $r.DataJson.Substring(0,500) + "..." } else { $r.DataJson }
    Write-Host "Data: $truncData"
}
