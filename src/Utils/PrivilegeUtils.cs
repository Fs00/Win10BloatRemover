using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Win10BloatRemover.Utils
{
    static class PrivilegeUtils
    {
        // It's up to the caller to obtain the needed token privileges (TakeOwnership, Restore) for this operation
        public static void GrantFullControlOnSubKey(this RegistryKey registryKey, string subkeyName)
        {
            using (RegistryKey subKey = registryKey.OpenSubKeyOrThrowIfMissing(subkeyName,
                RegistryRights.TakeOwnership | RegistryRights.ChangePermissions
            ))
            {
                RegistrySecurity accessRules = subKey.GetAccessControl();
                accessRules.SetOwner(WindowsIdentity.GetCurrent().User);
                accessRules.ResetAccessRule(
                    new RegistryAccessRule(
                        WindowsIdentity.GetCurrent().User,
                        RegistryRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow
                    )
                );
                subKey.SetAccessControl(accessRules);
            }
        }

        // It's up to the caller to obtain the needed privileges (TakeOwnership) for this operation
        public static void TakeOwnershipOnSubKey(this RegistryKey registryKey, string subkeyName)
        {
            using (RegistryKey subKey = registryKey.OpenSubKeyOrThrowIfMissing(subkeyName, RegistryRights.TakeOwnership))
            {
                RegistrySecurity accessRules = subKey.GetAccessControl();
                accessRules.SetOwner(WindowsIdentity.GetCurrent().User);
                subKey.SetAccessControl(accessRules);
            }
        }

        public const string BACKUP_TOKEN_PRIVILEGE = "SeBackupPrivilege";
        public const string RESTORE_TOKEN_PRIVILEGE = "SeRestorePrivilege";
        public const string TAKE_OWNERSHIP_TOKEN_PRIVILEGE = "SeTakeOwnershipPrivilege";

        public static void GrantTokenPrivilege(string privilege)
        {
            OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle);
            SingleTokenPrivilege tokenPrivilege = new SingleTokenPrivilege {
                Count = 1,
                Luid = 0,
                Attributes = SE_PRIVILEGE_ENABLED
            };
            LookupPrivilegeValue(null, privilege, out tokenPrivilege.Luid);
            bool successful = AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivilege, 0, IntPtr.Zero, IntPtr.Zero);
            if (!successful || Marshal.GetLastWin32Error() == ERROR_NOT_ALL_ASSIGNED)
                throw new SecurityException($"Can't grant token privilege {privilege}");
        }

        public static void RevokeTokenPrivilege(string privilege)
        {
            OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle);
            SingleTokenPrivilege tokenPrivilege = new SingleTokenPrivilege {
                Count = 1,
                Luid = 0,
                Attributes = SE_PRIVILEGE_DISABLED
            };
            LookupPrivilegeValue(null, privilege, out tokenPrivilege.Luid);
            bool successful = AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivilege, 0, IntPtr.Zero, IntPtr.Zero);
            if (!successful || Marshal.GetLastWin32Error() == ERROR_NOT_ALL_ASSIGNED)
                throw new SecurityException($"Can't revoke token privilege {privilege}");
        }

        #region P/Invoke methods and constants
        private const int ERROR_NOT_ALL_ASSIGNED = 1300;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SingleTokenPrivilege
        {
            public int Count;
            public long Luid;
            public int Attributes;
        }

        private const int SE_PRIVILEGE_DISABLED = 0x00000000;
        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int TOKEN_QUERY = 0x00000008;
        private const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
                                                         bool disableAllPrivileges,
                                                         ref SingleTokenPrivilege newState,
                                                         int bufferLength,
                                                         IntPtr previousState,
                                                         IntPtr returnLength);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, int desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string systemName, string privilegeName, out long privilegeLUID);
        #endregion
    }
}
