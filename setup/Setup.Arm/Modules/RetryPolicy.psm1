function Invoke-WithRetries
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$True)][scriptblock]$ScriptBlock
    )
    PROCESS
    {
        $retryCountMax = 5
        $retryDelaySeconds = 5
        $retryCount = 0
        $success = $false
        while (-not $success) {
            try {
                $result = . $ScriptBlock
                $success = $true
            }
            catch {
                if ($retryCount -ge $retryCountMax) {
                    Write-Verbose("Operation failed the maximum number of {0} times." -f $retryCountMax)
                    throw
                } else {
                    Write-Verbose("Operation failed. Retrying in {0} seconds." -f $retryDelaySeconds)
                    Start-Sleep $retryDelaySeconds
                    $retryCount++
                }
            }
        }

        return $result
    }
}

Export-ModuleMember Invoke-WithRetries