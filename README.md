# Morphée

Windows 11 portable app: double-click → PC goes to sleep. No install, no prompts, no console window.

A single tiny native exe (C#, `/target:winexe`), built with the C# compiler that ships with Windows — no SDK, Node or Electron needed.

On Modern Standby laptops it turns the display off (same as Start → Sleep, wakes from keyboard/touchpad); on classic S3 PCs it calls `SetSuspendState`. It never hibernates. Errors go to `%TEMP%\morphee-error.txt`.

## Build

```bash
build.cmd
```

Output: `release/Morphee.exe`

## Check without sleeping

`Morphee.exe --dry-run` only detects the sleep mode and returns it as the exit code: `1` Modern Standby, `2` S3 suspend, `3` none.
