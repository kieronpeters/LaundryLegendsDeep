param(
    [Parameter(Mandatory = $true)][ValidateSet('create', 'list', 'close', 'comment')][string]$Action,
    [string]$Title = "",
    [string]$Body = "",
    [string]$Labels = "",
    [int]$Number = 0
)

$ErrorActionPreference = 'Stop'

function Ensure-Gh {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI (gh) is not installed."
    }
}

Ensure-Gh

switch ($Action) {
    'create' {
        if ([string]::IsNullOrWhiteSpace($Title)) { throw "-Title is required." }
        $arguments = @('issue', 'create', '--title', $Title, '--body', $Body)
        if (-not [string]::IsNullOrWhiteSpace($Labels)) {
            $arguments += @('--label', $Labels)
        }
        & gh @arguments
    }
    'list' {
        & gh issue list --state open --json number,title,labels,url --limit 200
    }
    'close' {
        if ($Number -le 0) { throw "-Number is required." }
        & gh issue close $Number
    }
    'comment' {
        if ($Number -le 0) { throw "-Number is required." }
        & gh issue comment $Number --body $Body
    }
}
