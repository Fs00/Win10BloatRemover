using System.Runtime.InteropServices;
using System.Security;

namespace Win10BloatRemover.Utils;

/*
 *  Allows clients to obtain a Windows token privilege for a well-defined scope simply by "using" an instance of this class.
 */
class TokenPrivilege : IDisposable
{
    private enum PrivilegeAction : uint
    {
        Disable = 0x0,
        Enable = 0x2
    }

    public static TokenPrivilege TakeOwnership => new TokenPrivilege("SeTakeOwnershipPrivilege");

    private readonly string privilegeName;

    private TokenPrivilege(string privilegeName)
    {
        this.privilegeName = privilegeName;
        Apply(PrivilegeAction.Enable);
    }

    private void Apply(PrivilegeAction action)
    {
        OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle);
        LookupPrivilegeValue(null, privilegeName, out Luid luid);
        var tokenPrivilege = new TokenPrivileges(luid, (uint) action);
        UpdateTokenPrivileges(tokenHandle, tokenPrivilege);
    }

    private void UpdateTokenPrivileges(IntPtr tokenHandle, TokenPrivileges privilegeInfo)
    {
        bool successful = AdjustTokenPrivileges(tokenHandle, false, ref privilegeInfo, 0, IntPtr.Zero, IntPtr.Zero);
        if (!successful || Marshal.GetLastWin32Error() == ERROR_NOT_ALL_ASSIGNED)
            throw new SecurityException($"Can't adjust token privilege {privilegeName}");
    }

    public void Dispose()
    {
        Apply(PrivilegeAction.Disable);
    }

    #region P/Invoke structs and methods
    private const int ERROR_NOT_ALL_ASSIGNED = 1300;

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenPrivileges(Luid luid, uint attributes)
    {
        // We can use this struct only with one privilege since CLR doesn't support marshalling dynamic-sized arrays
        private uint Count = 1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        private LuidAndAttributes[] Privileges = [new LuidAndAttributes(luid, attributes)];
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct LuidAndAttributes(Luid luid, uint attributes)
    {
        private readonly Luid Luid = luid;
        private readonly uint Attributes = attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Luid
    {
        private readonly uint LowPart;
        private readonly int HighPart;
    }

    private const int TOKEN_QUERY = 0x8;
    private const int TOKEN_ADJUST_PRIVILEGES = 0x20;

    [DllImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
                                                     bool disableAllPrivileges,
                                                     ref TokenPrivileges newState,
                                                     int bufferLength,
                                                     IntPtr previousState,
                                                     IntPtr returnLength);

    [DllImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool OpenProcessToken(IntPtr processHandle, int desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool LookupPrivilegeValue(string? systemName, string privilegeName, out Luid privilegeLuid);
    #endregion
}
