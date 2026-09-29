using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Double-click → PC sleeps. Built as /target:winexe, so no console or window ever appears.
///
/// Modern Standby laptops (e.g. Lenovo IdeaPad Slim 5) have no S1–S3. There,
/// SetSuspendState / NtInitiatePowerAction(Sleep) fall through to hibernate, which only
/// the power button wakes. Start → Sleep on those machines just turns the console display
/// off, which enters Modern Standby and wakes from keyboard/touchpad — so do the same.
///
/// Classic S3 machines get SetSuspendState(sleep, not forced, wake events enabled).
/// If no sleep state exists at all, log and do nothing rather than hibernate.
///
/// Errors go to %TEMP%\morphee-error.txt.
/// `Morphee.exe --dry-run` only detects the mode and returns it as the exit code.
/// </summary>
static class Morphee
{
    const int ModeModernStandby = 1;
    const int ModeSuspend = 2;
    const int ModeNone = 3;

    // SYSTEM_POWER_CAPABILITIES byte offsets
    const int CapSystemS1 = 3;
    const int CapSystemS2 = 4;
    const int CapSystemS3 = 5;
    const int CapAoAc = 20;

    const int SE_SHUTDOWN_PRIVILEGE = 19;
    static readonly IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;
    const uint WM_SYSCOMMAND = 0x0112;
    const int SC_MONITORPOWER = 0xF170;
    const int MONITOR_OFF = 2;

    [DllImport("powrprof.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    static extern bool GetPwrCapabilities([Out] byte[] caps);

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.U1)] bool hibernate,
        [MarshalAs(UnmanagedType.U1)] bool forceCritical,
        [MarshalAs(UnmanagedType.U1)] bool disableWakeEvent);

    [DllImport("ntdll.dll")]
    static extern int RtlAdjustPrivilege(
        int privilege,
        [MarshalAs(UnmanagedType.U1)] bool enable,
        [MarshalAs(UnmanagedType.U1)] bool currentThread,
        [MarshalAs(UnmanagedType.U1)] out bool wasEnabled);

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "--dry-run")
                return DetectMode();

            // Let the user's click/keypress settle, so it doesn't wake the display
            Thread.Sleep(1000);
            Sleep();
            return 0;
        }
        catch (Exception e)
        {
            try
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "morphee-error.txt"), e.ToString());
            }
            catch
            {
                // ignore
            }
            return -1;
        }
    }

    static int DetectMode()
    {
        byte[] caps = new byte[128];
        if (!GetPwrCapabilities(caps))
            throw new InvalidOperationException("GetPwrCapabilities failed");
        if (caps[CapAoAc] != 0)
            return ModeModernStandby;
        if (caps[CapSystemS1] != 0 || caps[CapSystemS2] != 0 || caps[CapSystemS3] != 0)
            return ModeSuspend;
        return ModeNone;
    }

    static void Sleep()
    {
        switch (DetectMode())
        {
            case ModeModernStandby:
                PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
                break;
            case ModeSuspend:
                bool wasEnabled;
                RtlAdjustPrivilege(SE_SHUTDOWN_PRIVILEGE, true, false, out wasEnabled);
                if (!SetSuspendState(false, false, false))
                    throw new InvalidOperationException("SetSuspendState failed: " + Marshal.GetLastWin32Error());
                break;
            default:
                throw new InvalidOperationException("No sleep state available; refusing to hibernate");
        }
    }
}
