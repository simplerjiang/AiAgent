param(
    [string[]]$Models = @('llama3.2:3b', 'gemma4:e2b', 'gemma4:latest'),
    [int[]]$NumCtxValues = @(1024, 2048, 4096),
    [int]$NumPredict = 160,
    [int]$RepeatCount = 1,
    [string]$BaseUrl = 'http://localhost:11434',
    [string]$OutputPath = '.automation/reports/ollama-local-benchmark-20260407.json'
)

$ErrorActionPreference = 'Stop'

function Get-ModelSummary {
    param([string]$Model)

    $summaryText = & ollama show $Model 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to inspect model '$Model': $summaryText"
    }

    $summary = [ordered]@{
        model = $Model
        architecture = $null
        parameters = $null
        contextLength = $null
        embeddingLength = $null
        quantization = $null
    }

    foreach ($line in ($summaryText -split "`r?`n")) {
        if ($line -match '^\s*architecture\s+(?<value>.+?)\s*$') {
            $summary.architecture = $Matches.value.Trim()
        }
        elseif ($line -match '^\s*parameters\s+(?<value>.+?)\s*$') {
            $summary.parameters = $Matches.value.Trim()
        }
        elseif ($line -match '^\s*context length\s+(?<value>.+?)\s*$') {
            $summary.contextLength = $Matches.value.Trim()
        }
        elseif ($line -match '^\s*embedding length\s+(?<value>.+?)\s*$') {
            $summary.embeddingLength = $Matches.value.Trim()
        }
        elseif ($line -match '^\s*quantization\s+(?<value>.+?)\s*$') {
            $summary.quantization = $Matches.value.Trim()
        }
    }

    return [pscustomobject]$summary
}

function Get-ProcessorSnapshot {
    param([string]$Model)

    $psText = & ollama ps 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        return ''
    }

    foreach ($line in ($psText -split "`r?`n")) {
        if ($line -match [regex]::Escape($Model)) {
            return ($line -replace '\s+', ' ').Trim()
        }
    }

    return ''
}

function Invoke-BenchmarkRun {
    param(
        [string]$Model,
        [int]$NumCtx,
        [int]$NumPredict,
        [int]$RunIndex,
        [string]$BaseUrl
    )

    $prompt = @"
Write exactly 5 short sentences explaining why seawater usually looks blue.
Then give 1 everyday analogy.
Finish with a final line that starts with Conclusion:.
"@

    $body = @{
        model = $Model
        messages = @(
            @{
                role = 'user'
                content = $prompt
            }
        )
        stream = $false
        think = $false
        keep_alive = -1
        options = @{
            num_ctx = $NumCtx
            num_predict = $NumPredict
            temperature = 0
            top_k = 64
            top_p = 0.95
        }
    } | ConvertTo-Json -Depth 8

    $startedAt = Get-Date
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/chat" -Method Post -Body $body -ContentType 'application/json'
    $endedAt = Get-Date

    $evalDurationSeconds = if ($response.eval_duration) { [double]$response.eval_duration / 1e9 } else { 0 }
    $promptEvalDurationSeconds = if ($response.prompt_eval_duration) { [double]$response.prompt_eval_duration / 1e9 } else { 0 }
    $loadDurationSeconds = if ($response.load_duration) { [double]$response.load_duration / 1e9 } else { 0 }
    $tokPerSecond = if ($evalDurationSeconds -gt 0 -and $response.eval_count) {
        [math]::Round(([double]$response.eval_count / $evalDurationSeconds), 2)
    }
    else {
        0
    }

    $responseContent = ''
    if ($null -ne $response.message -and $null -ne $response.message.content) {
        $responseContent = [string]$response.message.content
    }

    $normalizedPreview = ($responseContent -replace '\s+', ' ').Trim()
    if ($normalizedPreview.Length -gt 160) {
        $normalizedPreview = $normalizedPreview.Substring(0, 160)
    }

    return [pscustomobject]@{
        model = $Model
        num_ctx = $NumCtx
        num_predict = $NumPredict
        runIndex = $RunIndex
        startedAt = $startedAt.ToString('o')
        wallClockSeconds = [math]::Round((New-TimeSpan -Start $startedAt -End $endedAt).TotalSeconds, 2)
        prompt_eval_count = $response.prompt_eval_count
        prompt_eval_duration_s = [math]::Round($promptEvalDurationSeconds, 2)
        eval_count = $response.eval_count
        eval_duration_s = [math]::Round($evalDurationSeconds, 2)
        load_duration_s = [math]::Round($loadDurationSeconds, 2)
        tok_per_s = $tokPerSecond
        done_reason = $response.done_reason
        responsePreview = $normalizedPreview
        processorSnapshot = Get-ProcessorSnapshot -Model $Model
    }
}

$modelSummaries = @{}
foreach ($model in $Models) {
    $modelSummaries[$model] = Get-ModelSummary -Model $model
}

$results = New-Object System.Collections.Generic.List[object]

foreach ($model in $Models) {
    foreach ($numCtx in $NumCtxValues) {
        for ($runIndex = 1; $runIndex -le $RepeatCount; $runIndex++) {
            $results.Add((Invoke-BenchmarkRun -Model $model -NumCtx $numCtx -NumPredict $NumPredict -RunIndex $runIndex -BaseUrl $BaseUrl))
        }
    }
}

$grouped = $results |
    Group-Object model, num_ctx |
    ForEach-Object {
        $first = $_.Group[0]
        $summary = $modelSummaries[$first.model]
        [pscustomobject]@{
            model = $first.model
            architecture = $summary.architecture
            parameters = $summary.parameters
            quantization = $summary.quantization
            modelContextLength = $summary.contextLength
            num_ctx = $first.num_ctx
            avg_tok_per_s = [math]::Round((($_.Group | Measure-Object -Property tok_per_s -Average).Average), 2)
            avg_eval_duration_s = [math]::Round((($_.Group | Measure-Object -Property eval_duration_s -Average).Average), 2)
            avg_load_duration_s = [math]::Round((($_.Group | Measure-Object -Property load_duration_s -Average).Average), 2)
            sampleProcessorSnapshot = $first.processorSnapshot
            samplePreview = $first.responsePreview
        }
    } |
    Sort-Object avg_tok_per_s -Descending

$output = [pscustomobject]@{
    generatedAt = (Get-Date).ToString('o')
    baseUrl = $BaseUrl
    models = $Models
    numCtxValues = $NumCtxValues
    numPredict = $NumPredict
    repeatCount = $RepeatCount
    modelSummaries = $modelSummaries.Values
    groupedSummary = $grouped
    rawResults = $results
}

$outputDirectory = Split-Path -Path $OutputPath -Parent
if ($outputDirectory) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$output | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8

$grouped | Format-Table -AutoSize | Out-String | Write-Output
Write-Output "Saved benchmark report to $OutputPath"