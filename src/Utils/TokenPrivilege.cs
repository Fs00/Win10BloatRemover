using System.Runtime.InteropServices;
using System.Security;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace Win10BloatRemover.Utils;

/*
 *  Allows clients to obtain a Windows token privilege for a well-defined scope simply by "using" an instance of this class.
 */
class TokenPrivilege : IDisposable
{
    public static TokenPrivilege TakeOwnership => new TokenPrivilege("SeTakeOwnershipPrivilege");

    private readonly string privilegeName;

    private TokenPrivilege(string privilegeName)
    {
        this.privilegeName = privilegeName;
        TogglePrivilege(Action.Enable);
    }
    
    public void Dispose()
    {
        TogglePrivilege(Action.Disable);
    }
    
    private enum Action : uint
    {
        Disable = 0x0,
        Enable = 0x2
    }

    private void TogglePrivilege(Action privilegeAction)
    {
        using var currentProcessHandle = WinAPI.GetCurrentProcess_SafeHandle();
        WinAPI.OpenProcessToken(currentProcessHandle,
            TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, out var tokenHandle);
        WinAPI.LookupPrivilegeValue(null, privilegeName, out var privilegeLuid);
        UpdateTokenPrivileges(tokenHandle, privilegeLuid, privilegeAction);
    }

    private unsafe void UpdateTokenPrivileges(SafeHandle tokenHandle, LUID privilegeLuid, Action privilegeAction)
    {
        var updatedPrivileges = new TOKEN_PRIVILEGES {
            PrivilegeCount = 1,
            Privileges = new VariableLengthInlineArray<LUID_AND_ATTRIBUTES> {
                [0] = new LUID_AND_ATTRIBUTES { Luid = privilegeLuid, Attributes = (TOKEN_PRIVILEGES_ATTRIBUTES) privilegeAction }
            }
        };
        bool successful = WinAPI.AdjustTokenPrivileges(tokenHandle, false, &updatedPrivileges, null);
        if (!successful || Marshal.GetLastWin32Error() == (int) WIN32_ERROR.ERROR_NOT_ALL_ASSIGNED)
            throw new SecurityException($"Can't adjust token privilege {privilegeName}");
    }
}
