param(
    [string]$BaseUrl = 'http://localhost:5119',
    [string]$Symbol = 'sh600000',
    [string]$TaskId = '',
    [string]$OutputDir = '',
    [string]$ReportPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($TaskId)) {
    $TaskId = 'GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'tmp'
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$jsonPath = Join-Path $OutputDir ("mcp-full-request-response-$timestamp.json")
$mdPath = Join-Path $OutputDir ("mcp-full-request-response-$timestamp.md")
$filteredMdPath = Join-Path $OutputDir ("mcp-full-request-response-$timestamp.filtered.md")
$normalizedBaseUrl = $BaseUrl.TrimEnd('/')

function ConvertTo-OrderedPlainObject {
    param([object]$Value)

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [string] -or $Value -is [ValueType]) {
        return $Value
    }

    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [System.Collections.IDictionary]) -and -not ($Value -is [psobject])) {
        $items = @()
        foreach ($item in $Value) {
            $items += ,(ConvertTo-OrderedPlainObject -Value $item)
        }
        return $items
    }

    if ($Value -is [System.Collections.IDictionary]) {
        $map = [ordered]@{}
        foreach ($key in $Value.Keys) {
            $map[$key] = ConvertTo-OrderedPlainObject -Value $Value[$key]
        }
        return $map
    }

    $result = [ordered]@{}
    foreach ($prop in $Value.PSObject.Properties) {
        $result[$prop.Name] = ConvertTo-OrderedPlainObject -Value $prop.Value
    }
    return $result
}

function ConvertTo-HeaderMap {
    param([object]$Headers)

    $map = [ordered]@{}
    if ($null -eq $Headers) {
        return $map
    }

    foreach ($item in $Headers.GetEnumerator()) {
        $value = $item.Value
        if ($value -is [System.Array]) {
            $map[$item.Key] = [string]::Join(', ', $value)
        }
        else {
            $map[$item.Key] = [string]$value
        }
    }

    return $map
}

function ConvertTo-QueryString {
    param([hashtable]$Query)

    $pairs = foreach ($key in $Query.Keys) {
        $encodedKey = [System.Uri]::EscapeDataString([string]$key)
        $encodedValue = [System.Uri]::EscapeDataString([string]$Query[$key])
        "$encodedKey=$encodedValue"
    }

    return [string]::Join('&', $pairs)
}

function Get-ScalarTopLevelData {
    param([object]$Data)

    $result = [ordered]@{}
    if ($null -eq $Data) {
        return $result
    }

    foreach ($prop in $Data.PSObject.Properties) {
        $value = $prop.Value
        if ($null -eq $value -or $value -is [string] -or $value -is [ValueType]) {
            $result[$prop.Name] = $value
        }
    }

    return $result
}

function ConvertTo-MarkdownJson {
    param([object]$Value)

    $plain = ConvertTo-OrderedPlainObject -Value $Value
    return ($plain | ConvertTo-Json -Depth 30)
}

function Add-ListLines {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Label,
        [object]$Value
    )

    if ($null -eq $Value) {
        return
    }

    if ($Value -is [System.Array]) {
        if ($Value.Count -eq 0) {
            return
        }

        $Lines.Add(" - ${Label}:")
        foreach ($item in $Value) {
            $Lines.Add("   - $item")
        }
        return
    }

    $Lines.Add(" - ${Label}: ``$Value``")
}

$toolDefinitions = @(
    [ordered]@{ Tool = 'CompanyOverviewMcp'; Path = '/api/stocks/mcp/company-overview'; Query = [ordered]@{ symbol = $Symbol; taskId = $TaskId } },
    [ordered]@{ Tool = 'StockProductMcp'; Path = '/api/stocks/mcp/product'; Query = [ordered]@{ symbol = $Symbol; taskId = $TaskId } },
    [ordered]@{ Tool = 'StockFundamentalsMcp'; Path = '/api/stocks/mcp/fundamentals'; Query = [ordered]@{ symbol = $Symbol; taskId = $TaskId } },
    [ordered]@{ Tool = 'StockShareholderMcp'; Path = '/api/stocks/mcp/shareholder'; Query = [ordered]@{ symbol = $Symbol; taskId = $TaskId } },
    [ordered]@{ Tool = 'MarketContextMcp'; Path = '/api/stocks/mcp/market-context'; Query = [ordered]@{ symbol = $Symbol; taskId = $TaskId } },
    [ordered]@{ Tool = 'SocialSentimentMcp'; Path = '/api/stocks/mcp/social-sentiment'; Query = [ordered]@{ symbol = $Symbol; taskId = $TaskId } },
    [ordered]@{ Tool = 'StockKlineMcp'; Path = '/api/stocks/mcp/kline'; Query = [ordered]@{ symbol = $Symbol; interval = 'day'; count = '60'; taskId = $TaskId } },
    [ordered]@{ Tool = 'StockMinuteMcp'; Path = '/api/stocks/mcp/minute'; Query = [ordered]@{ symbol = $Symbol; taskId = $TaskId } },
    [ordered]@{ Tool = 'StockStrategyMcp'; Path = '/api/stocks/mcp/strategy'; Query = [ordered]@{ symbol = $Symbol; interval = 'day'; count = '60'; taskId = $TaskId } },
    [ordered]@{ Tool = 'StockNewsMcp'; Path = '/api/stocks/mcp/news'; Query = [ordered]@{ symbol = $Symbol; level = 'stock'; taskId = $TaskId } },
    [ordered]@{ Tool = 'StockSearchMcp'; Path = '/api/stocks/mcp/search'; Query = [ordered]@{ query = '浦发银行'; trustedOnly = 'true'; taskId = $TaskId } }
)

$results = New-Object System.Collections.Generic.List[object]
$failures = New-Object System.Collections.Generic.List[string]

foreach ($definition in $toolDefinitions) {
    $queryString = ConvertTo-QueryString -Query $definition.Query
    $pathWithQuery = "$($definition.Path)?$queryString"
    $url = "$normalizedBaseUrl$pathWithQuery"

    $responseRecord = [ordered]@{
        statusCode = $null
        headers = [ordered]@{}
        bodyRaw = $null
        bodyJson = $null
    }
    $errorRecord = $null

    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -TimeoutSec 240
        $responseRecord.statusCode = [int]$response.StatusCode
        $responseRecord.headers = ConvertTo-HeaderMap -Headers $response.Headers
        $responseRecord.bodyRaw = [string]$response.Content
    }
    catch {
        $statusCode = $null
        $headers = [ordered]@{}
        $bodyRaw = $null

        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode.value__
            $headers = ConvertTo-HeaderMap -Headers $_.Exception.Response.Headers
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            try {
                $bodyRaw = $reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }
        }

        $responseRecord.statusCode = $statusCode
        $responseRecord.headers = $headers
        $responseRecord.bodyRaw = $bodyRaw
        $errorRecord = [ordered]@{
            message = $_.Exception.Message
            type = $_.Exception.GetType().FullName
        }
        $failures.Add("$($definition.Tool): $($_.Exception.Message)")
    }

    $results.Add([ordered]@{
        tool = $definition.Tool
        request = [ordered]@{
            method = 'GET'
            url = $url
            path = $pathWithQuery
            tool = $definition.Tool
            query = [ordered]@{} + $definition.Query
        }
        response = $responseRecord
        error = $errorRecord
    }) | Out-Null
}

$results | ConvertTo-Json -Depth 30 | Set-Content -Path $jsonPath -Encoding UTF8

$indexLines = New-Object System.Collections.Generic.List[string]
$indexLines.Add('# MCP 请求与返回（全量11个）') | Out-Null
$indexLines.Add('') | Out-Null
foreach ($item in $results) {
    $indexLines.Add("## $($item.tool)") | Out-Null
    $indexLines.Add("- request.url: ``$($item.request.url)``") | Out-Null
    $indexLines.Add("- response.statusCode: ``$($item.response.statusCode)``") | Out-Null
    $indexLines.Add('') | Out-Null
}
[System.IO.File]::WriteAllLines($mdPath, $indexLines, [System.Text.UTF8Encoding]::new($false))

$filteredLines = New-Object System.Collections.Generic.List[string]
$filteredLines.Add('# MCP 请求与返回（过滤版）') | Out-Null
$filteredLines.Add('') | Out-Null
$filteredLines.Add("> 来源：``$jsonPath``") | Out-Null
$filteredLines.Add('> 说明：保留请求参数、状态、traceId、关键指标与证据样本；省略超长 K 线/分时点位明细。') | Out-Null
$filteredLines.Add('') | Out-Null
$filteredLines.Add("总计工具数：**$($results.Count)**") | Out-Null
$filteredLines.Add('') | Out-Null

foreach ($item in $results) {
    $filteredLines.Add("## $($item.tool)") | Out-Null
    $filteredLines.Add('') | Out-Null
    $filteredLines.Add('### 请求') | Out-Null
    $filteredLines.Add(" - method: ``$($item.request.method)``") | Out-Null
    $filteredLines.Add(" - url: ``$($item.request.url)``") | Out-Null
    $filteredLines.Add(" - query: ``$(($item.request.query | ConvertTo-Json -Compress))``") | Out-Null
    $filteredLines.Add('') | Out-Null
    $filteredLines.Add('### 返回') | Out-Null
    $filteredLines.Add(" - statusCode: ``$($item.response.statusCode)``") | Out-Null

    $body = $null
    if (-not [string]::IsNullOrWhiteSpace([string]$item.response.bodyRaw)) {
        try {
            $body = $item.response.bodyRaw | ConvertFrom-Json
        }
        catch {
            $body = $null
        }
    }

    if ($null -ne $body) {
        Add-ListLines -Lines $filteredLines -Label 'traceId' -Value $body.traceId
        Add-ListLines -Lines $filteredLines -Label 'taskId' -Value $body.taskId
        Add-ListLines -Lines $filteredLines -Label 'latencyMs' -Value $body.latencyMs
        Add-ListLines -Lines $filteredLines -Label 'errorCode' -Value $body.errorCode
        Add-ListLines -Lines $filteredLines -Label 'freshnessTag' -Value $body.freshnessTag
        Add-ListLines -Lines $filteredLines -Label 'sourceTier' -Value $body.sourceTier
        if ($null -ne $body.cache) {
            Add-ListLines -Lines $filteredLines -Label 'cache.hit' -Value $body.cache.hit
            Add-ListLines -Lines $filteredLines -Label 'cache.source' -Value $body.cache.source
        }
        if ($null -ne $body.warnings -and @($body.warnings).Count -gt 0) {
            Add-ListLines -Lines $filteredLines -Label 'warnings' -Value @($body.warnings)
        }
        if ($null -ne $body.degradedFlags -and @($body.degradedFlags).Count -gt 0) {
            Add-ListLines -Lines $filteredLines -Label 'degradedFlags' -Value @($body.degradedFlags)
        }
    }
    elseif ($null -ne $item.error) {
        Add-ListLines -Lines $filteredLines -Label 'error.message' -Value $item.error.message
        Add-ListLines -Lines $filteredLines -Label 'error.type' -Value $item.error.type
    }

    $filteredLines.Add('') | Out-Null
    $filteredLines.Add('### 关键数据（过滤）') | Out-Null
    if ($null -ne $body -and $null -ne $body.data) {
        $scalarData = Get-ScalarTopLevelData -Data $body.data
        $filteredLines.Add('```json') | Out-Null
        $filteredLines.Add((ConvertTo-MarkdownJson -Value $scalarData)) | Out-Null
        $filteredLines.Add('```') | Out-Null
    }
    else {
        $filteredLines.Add('```json') | Out-Null
        $filteredLines.Add('{}') | Out-Null
        $filteredLines.Add('```') | Out-Null
    }

    $filteredLines.Add('') | Out-Null
    if ($null -ne $body -and $null -ne $body.evidence -and @($body.evidence).Count -gt 0) {
        $filteredLines.Add('### 证据样本（最多 3 条）') | Out-Null
        $sampleEvidence = @($body.evidence) | Select-Object -First 3
        foreach ($evidence in $sampleEvidence) {
            $title = if ([string]::IsNullOrWhiteSpace([string]$evidence.title)) { [string]$evidence.point } else { [string]$evidence.title }
            $source = [string]$evidence.source
            $publishedAt = [string]$evidence.publishedAt
            $summary = if (-not [string]::IsNullOrWhiteSpace([string]$evidence.summary)) { [string]$evidence.summary } elseif (-not [string]::IsNullOrWhiteSpace([string]$evidence.excerpt)) { [string]$evidence.excerpt } else { [string]$evidence.point }
            $filteredLines.Add("1. ``$title`` | 来源：$source | 时间：$publishedAt") | Out-Null
            $filteredLines.Add("   - 摘要：$summary") | Out-Null
        }
        $filteredLines.Add('') | Out-Null
    }

    $filteredLines.Add('---') | Out-Null
    $filteredLines.Add('') | Out-Null
}

[System.IO.File]::WriteAllLines($filteredMdPath, $filteredLines, [System.Text.UTF8Encoding]::new($false))

if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    Copy-Item -Path $filteredMdPath -Destination $ReportPath -Force
}

[ordered]@{
    TaskId = $TaskId
    JsonPath = $jsonPath
    MdPath = $mdPath
    FilteredMdPath = $filteredMdPath
    ReportPath = $ReportPath
    FailureCount = $failures.Count
    Failures = @($failures)
} | ConvertTo-Json -Depth 10