const { app } = require('electron');
const { spawn } = require('child_process');

function forceSleep() {
  // Suspend (not hibernate), force apps that block sleep, allow wake events.
  const script = `
Add-Type -TypeDefinition @"
using System.Runtime.InteropServices;
public static class Power {
  [DllImport("powrprof.dll", SetLastError = true)]
  public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
}
"@
[void][Power]::SetSuspendState($false, $true, $false)
`.trim();

  spawn(
    'powershell.exe',
    ['-NoProfile', '-NonInteractive', '-WindowStyle', 'Hidden', '-Command', script],
    {
      windowsHide: true,
      detached: true,
      stdio: 'ignore',
    },
  ).unref();
}

app.whenReady().then(() => {
  forceSleep();
  app.quit();
});
