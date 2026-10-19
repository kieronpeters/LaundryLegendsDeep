[CmdletBinding()]
param(
    [switch]$PreCheckpoint
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$errors = New-Object 'System.Collections.Generic.List[string]'
$warnings = New-Object 'System.Collections.Generic.List[string]'
$requiredCount = 0
$jsonCount = 0
$taskCount = 0

function Add-Error {
    param([string]$Message)
    [void]$errors.Add($Message)
}

function Add-Warning {
    param([string]$Message)
    [void]$warnings.Add($Message)
}

function Require-Path {
    param([string]$RelativePath)
    $script:requiredCount++
    if (-not (Test-Path -LiteralPath (Join-Path $root $RelativePath))) {
        Add-Error "Missing required path: $RelativePath"
    }
}

Write-Host 'Laundry Legends - bootstrap validation'
Write-Host "Root: $root"
Write-Host ''

$required = @(
    'README.md',
    'AGENTS.md',
    'PROJECT_STATE.md',
    'MASTER_CONTEXT.md',
    'GAME_DESIGN.md',
    'ARCHITECTURE.md',
    'DEVELOPMENT_LOG.md',
    'ROADMAP.md',
    'ASSET_INVENTORY.md',
    'AGENT_GUIDE.md',
    'TESTING_GUIDE.md',
    'YOUTUBE_LOG.md',
    '.gitignore',
    '.opencode/opencode.json',
    '.orchestrator/state.json',
    '.orchestrator/budgets.json',
    '.orchestrator/GUARDRAILS.md',
    '.orchestrator/README.md',
    '.orchestrator/tasks/README.md',
    '.orchestrator/tasks/phase-00.json',
    '.orchestrator/tasks/phase-01.json',
    '.orchestrator/recovery/RESUME.md',
    '.orchestrator/decisions/0001-unity-version-and-render-pipeline.md',
    '.orchestrator/decisions/0002-orchestration-and-agent-model.md',
    '.orchestrator/decisions/0003-documentation-and-state-sources.md',
    '.orchestrator/decisions/0004-placeholder-and-asset-tool-policy.md',
    '.orchestrator/reports/2026-10-19-bootstrap.md',
    'tools/validate-bootstrap.ps1',
    'tools/status.ps1'
)

foreach ($path in $required) {
    Require-Path $path
}

$commands = @(
    'start-phase',
    'continue-phase',
    'status',
    'task-queue',
    'review-last-run',
    'retry-task',
    'resume',
    'run-phases',
    'stop-safe',
    'prepare-release'
)

foreach ($command in $commands) {
    $relative = ".opencode/commands/$command.md"
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative))) {
        Add-Error "Missing command: $relative"
    }
}

$agents = @('ll-unity-dev', 'll-designer', 'll-reviewer', 'll-qa')
foreach ($agent in $agents) {
    $relative = ".opencode/agents/$agent.md"
    $full = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $full)) {
        Add-Error "Missing agent: $relative"
        continue
    }
    $text = Get-Content -LiteralPath $full -Raw
    if ($text -notmatch '(?m)^description:\s*\S+') {
        Add-Error "Agent has no description: $relative"
    }
    if ($text -notmatch '(?m)^mode:\s*subagent\s*$') {
        Add-Error "Agent is not mode: subagent: $relative"
    }
    foreach ($deniedCommand in @('git add*', 'git commit*', 'git tag*')) {
        if ($text -notmatch "resource:\s*`"$([regex]::Escape($deniedCommand))`"") {
            Add-Error "Agent $relative is missing deny rule for $deniedCommand"
        }
    }
}

# Parse all project JSON documents.
$jsonFiles = @()
$jsonFiles += (Join-Path $root '.opencode/opencode.json')
$jsonFiles += (Get-ChildItem -LiteralPath (Join-Path $root '.orchestrator') -Recurse -File -Filter '*.json' | Select-Object -ExpandProperty FullName)

foreach ($file in ($jsonFiles | Sort-Object -Unique)) {
    $jsonCount++
    try {
        Get-Content -LiteralPath $file -Raw | ConvertFrom-Json | Out-Null
    }
    catch {
        Add-Error "Invalid JSON: $file ($($_.Exception.Message))"
    }
}

# Load task documents and validate IDs/dependencies.
$taskDocuments = @()
$knownTaskIds = @{}
$taskFiles = @(Get-ChildItem -LiteralPath (Join-Path $root '.orchestrator/tasks') -File -Filter 'phase-*.json' | Sort-Object Name)
$allowedTaskStatuses = @('planned', 'ready', 'in_progress', 'complete', 'failed', 'blocked', 'paused', 'pending', 'approval_pending')

foreach ($file in $taskFiles) {
    try {
        $doc = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        $taskDocuments += $doc
        foreach ($task in @($doc.tasks)) {
            if (-not $task.id) {
                Add-Error "Task without ID in $($file.Name)"
                continue
            }
            $taskCount++
            if ($knownTaskIds.ContainsKey($task.id)) {
                Add-Error "Duplicate task ID: $($task.id)"
            }
            else {
                $knownTaskIds[$task.id] = $task
            }
            foreach ($requiredField in @('title', 'status', 'owner', 'objective', 'acceptance')) {
                if (-not $task.PSObject.Properties[$requiredField]) {
                    Add-Error "Task $($task.id) missing field: $requiredField"
                }
            }
            if ($task.status -and $task.status -notin $allowedTaskStatuses) {
                Add-Error "Task $($task.id) has unknown status: $($task.status)"
            }
        }
    }
    catch {
        Add-Error "Could not load task file $($file.Name): $($_.Exception.Message)"
    }
}

foreach ($task in $knownTaskIds.Values) {
    foreach ($dependency in @($task.dependsOn)) {
        if ($dependency -and -not $knownTaskIds.ContainsKey($dependency)) {
            Add-Error "Task $($task.id) depends on unknown task: $dependency"
        }
    }
}

# Validate state references.
$statePath = Join-Path $root '.orchestrator/state.json'
$state = $null
try {
    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
}
catch {
    Add-Error "Cannot parse state.json: $($_.Exception.Message)"
}

if ($state) {
    if ($state.project.engineVersion -ne '6000.0.64f1') {
        Add-Error "Selected Unity version is not 6000.0.64f1: $($state.project.engineVersion)"
    }
    if ($state.git.branch -ne 'main') {
        Add-Error "Expected branch main, found: $($state.git.branch)"
    }
    if ($state.nextTask -and -not $knownTaskIds.ContainsKey($state.nextTask)) {
        Add-Error "state.nextTask does not exist: $($state.nextTask)"
    }
    if ($state.activeTask -and -not $knownTaskIds.ContainsKey($state.activeTask)) {
        Add-Error "state.activeTask does not exist: $($state.activeTask)"
    }
    if ($state.currentPhase.status -and $state.currentPhase.status -notin $allowedTaskStatuses) {
        Add-Error "state.currentPhase.status is not a documented status: $($state.currentPhase.status)"
    }
    if ($state.lastRun.status -and $state.lastRun.status -notin $allowedTaskStatuses) {
        Add-Error "state.lastRun.status is not a documented status: $($state.lastRun.status)"
    }
    if ($state.currentPhase.file) {
        $phaseFile = Join-Path $root $state.currentPhase.file
        if (-not (Test-Path -LiteralPath $phaseFile)) {
            Add-Error "state.currentPhase.file is missing: $($state.currentPhase.file)"
        }
    }
    if ($state.lastRun.report) {
        $reportFile = Join-Path $root $state.lastRun.report
        if (-not (Test-Path -LiteralPath $reportFile)) {
            Add-Error "state.lastRun.report is missing: $($state.lastRun.report)"
        }
    }
}

# Validate project config and model consistency.
$configPath = Join-Path $root '.opencode/opencode.json'
if (Test-Path -LiteralPath $configPath) {
    try {
        $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
        if (-not $config.'$schema') {
            Add-Warning 'Project opencode.json has no $schema field.'
        }
        if (-not $config.model) {
            Add-Error 'Project opencode.json has no model.'
        }
        if ($config.default_agent -ne 'build') {
            Add-Error "Project default_agent should be build, found: $($config.default_agent)"
        }
        if (-not $config.permissions -or @($config.permissions).Count -eq 0) {
            Add-Error 'Project opencode.json has no permissions rules.'
        }
        if ($state -and $config.model -ne $state.environment.primaryModel) {
            Add-Error "Config model ($($config.model)) does not match state primaryModel ($($state.environment.primaryModel))."
        }
        foreach ($builtInAgent in @('general', 'explore', 'plan')) {
            $agentProperty = $config.agents.PSObject.Properties[$builtInAgent]
            if (-not $agentProperty) {
                Add-Error "Project config does not restrict built-in agent: $builtInAgent"
                continue
            }
            $agentRules = @($agentProperty.Value.permissions)
            $hasCommitDeny = @($agentRules | Where-Object { $_.resource -eq 'git commit*' -and $_.effect -eq 'deny' }).Count -gt 0
            if (-not $hasCommitDeny) {
                Add-Error "Built-in agent $builtInAgent is not denied git commit."
            }
        }
    }
    catch {
        Add-Error "Cannot parse project opencode.json: $($_.Exception.Message)"
    }
}

# Validate command frontmatter.
foreach ($command in $commands) {
    $path = Join-Path $root ".opencode/commands/$command.md"
    if (Test-Path -LiteralPath $path) {
        $text = Get-Content -LiteralPath $path -Raw
        if ($text -notmatch '(?m)^description:\s*\S+') {
            Add-Error "Command has no description: $command.md"
        }
    }
}

# Ignore-rule checks.
$ignorePath = Join-Path $root '.gitignore'
if (Test-Path -LiteralPath $ignorePath) {
    $ignore = Get-Content -LiteralPath $ignorePath -Raw
    foreach ($pattern in @('[Ll]ibrary/', '[Tt]emp/', 'Builds/*', 'TestResults/*', '.env', '.orchestrator/worktrees/')) {
        if (-not $ignore.Contains($pattern)) {
            Add-Error "Missing .gitignore pattern: $pattern"
        }
    }
}

# Git hygiene.
if (-not (Test-Path -LiteralPath (Join-Path $root '.git'))) {
    Add-Error 'Git repository is missing.'
}
else {
    $branch = (git -C $root branch --show-current 2>$null)
    if ($branch -ne 'main') {
        Add-Error "Git branch is not main: $branch"
    }

    $tracked = @(git -C $root ls-files 2>$null)
    $forbiddenPaths = @(
        'Library/',
        'Temp/',
        'Obj/',
        'Logs/',
        'UserSettings/',
        'Builds/',
        'TestResults/',
        '.orchestrator/worktrees/'
    )

    foreach ($file in $tracked) {
        $normalized = $file -replace '\\', '/'
        foreach ($forbidden in $forbiddenPaths) {
            if ($normalized.StartsWith($forbidden) -and -not $normalized.EndsWith('.gitkeep')) {
                Add-Error "Tracked generated path: $normalized"
            }
        }
        $leaf = Split-Path $normalized -Leaf
        if ($leaf -match '^(\.env($|\.)|.*\.(pem|key|secret|secrets)$|id_rsa$|credentials\.json$|auth\.json$)') {
            if ($leaf -notmatch '\.env\.example$') {
                Add-Error "Tracked secret-like file: $normalized"
            }
        }
    }

    $commitCount = 0
    try {
        $commitCount = [int](git -C $root rev-list --count HEAD 2>$null)
    }
    catch {
        $commitCount = 0
    }
    if ($commitCount -eq 0) {
        Add-Warning 'Repository has no commits yet; run the checkpoint step after validation.'
    }

    if (-not $PreCheckpoint -and $state -and $state.currentPhase.status -eq 'complete') {
        if ($commitCount -eq 0) {
            Add-Error 'Phase 0 is marked complete, but the repository has no commit.'
        }
        if (-not $state.git.baselineCommit) {
            Add-Error 'Phase 0 is marked complete, but state.git.baselineCommit is empty.'
        }
        $tagCommit = $null
        try {
            $tagCommit = (git -C $root rev-list -n 1 bootstrap-v0.1 2>$null)
        }
        catch {
            $tagCommit = $null
        }
        if (-not $tagCommit) {
            Add-Error 'Phase 0 is marked complete, but tag bootstrap-v0.1 does not exist.'
        }
        else {
            $headCommit = (git -C $root rev-parse HEAD 2>$null)
            if ($headCommit -ne $tagCommit) {
                Add-Error "Tag bootstrap-v0.1 does not point at HEAD ($headCommit)."
            }
        }
        if ($state.lastRun.status -ne 'complete') {
            Add-Error "Phase 0 is marked complete, but state.lastRun.status is $($state.lastRun.status)."
        }
    }
}

# Semantic documentation sanity checks.
$stateMarkdownPath = Join-Path $root 'PROJECT_STATE.md'
if (Test-Path -LiteralPath $stateMarkdownPath) {
    $projectState = Get-Content -LiteralPath $stateMarkdownPath -Raw
    foreach ($phrase in @('Next recommended task', 'Current phase', 'Last successful development run')) {
        if ($projectState -notlike "*$phrase*") {
            Add-Error "PROJECT_STATE.md is missing expected section: $phrase"
        }
    }
}

$roadmapPath = Join-Path $root 'ROADMAP.md'
if (Test-Path -LiteralPath $roadmapPath) {
    $roadmap = Get-Content -LiteralPath $roadmapPath -Raw
    foreach ($phase in 0..16) {
        if ($roadmap -notmatch "(?m)^## Phase $phase\b") {
            Add-Error "ROADMAP.md is missing Phase $phase."
        }
    }
}

$assetPath = Join-Path $root 'ASSET_INVENTORY.md'
if (Test-Path -LiteralPath $assetPath) {
    $assets = Get-Content -LiteralPath $assetPath -Raw
    if ($assets -notlike '*No game assets*') {
        Add-Warning 'ASSET_INVENTORY.md no longer declares an empty asset baseline; verify it is intentional.'
    }
}

Write-Host "Required paths checked: $requiredCount"
Write-Host "JSON documents parsed:  $jsonCount"
Write-Host "Tasks checked:           $taskCount"
Write-Host "Commands checked:        $($commands.Count)"
Write-Host "Agents checked:          $($agents.Count)"
Write-Host ''

if ($warnings.Count -gt 0) {
    Write-Host 'Warnings:'
    foreach ($warning in $warnings) {
        Write-Host "  WARN  $warning"
    }
    Write-Host ''
}

if ($errors.Count -gt 0) {
    Write-Host 'Errors:'
    foreach ($errorItem in $errors) {
        Write-Host "  FAIL  $errorItem"
    }
    Write-Host ''
    Write-Host "Bootstrap validation FAILED ($($errors.Count) error(s))."
    exit 1
}

Write-Host 'Bootstrap validation PASSED.'
Write-Host 'No Unity compilation, gameplay test, playtest, or build was performed by this script.'
exit 0
