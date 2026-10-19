[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$statePath = Join-Path $root '.orchestrator/state.json'
if (-not (Test-Path -LiteralPath $statePath)) {
    Write-Host 'No orchestration state found.'
    exit 1
}

$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$latestReport = Get-ChildItem -LiteralPath (Join-Path $root '.orchestrator/reports') -File -Filter '*.md' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

Write-Host 'Laundry Legends - status'
Write-Host "Updated:        $($state.updatedAt)"
Write-Host "Milestone:      $($state.currentMilestone.id) - $($state.currentMilestone.name) ($($state.currentMilestone.status))"
Write-Host "Phase:          $($state.currentPhase.number) - $($state.currentPhase.name) ($($state.currentPhase.status))"
Write-Host "Active task:    $(if ($state.activeTask) { $state.activeTask } else { 'none' })"
Write-Host "Next task:      $($state.nextTask)"
Write-Host "Branch:         $($state.git.branch)"
Write-Host "Checkpoint:     $($state.git.checkpointTag)"
Write-Host "Baseline:       $(if ($state.git.baselineCommit) { $state.git.baselineCommit } else { 'not recorded' })"
Write-Host "Last run:       $($state.lastRun.id) - $($state.lastRun.status)"
if ($latestReport) {
    Write-Host "Latest report:  $($latestReport.FullName)"
}
if (@($state.blockers).Count -gt 0) {
    Write-Host 'Blockers:'
    foreach ($blocker in @($state.blockers)) {
        Write-Host "  - $blocker"
    }
}
else {
    Write-Host 'Blockers:       none recorded'
}
if (@($state.approvals.pending).Count -gt 0) {
    Write-Host 'Pending approvals:'
    foreach ($approval in @($state.approvals.pending)) {
        Write-Host "  - $approval"
    }
}
else {
    Write-Host 'Approvals:      none pending'
}
Write-Host ''
Write-Host 'Git status:'
git -C $root status --short --branch
Write-Host ''
Write-Host 'Run tools/validate-bootstrap.ps1 for structural validation.'
