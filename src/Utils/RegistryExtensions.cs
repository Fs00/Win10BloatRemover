using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

namespace Win10BloatRemover.Utils
{
    static class RegistryExtensions
    {
        public static void DeleteSubKeyValue(this RegistryKey registryKey, string subkeyName, string valueName)
        {
            using RegistryKey? subKey = registryKey.OpenSubKey(subkeyName, writable: true);
            subKey?.DeleteValue(valueName, throwOnMissingValue: false);
        }

        public static RegistryKey OpenSubKeyOrThrowIfMissing(this RegistryKey registryKey, string subkeyName, RegistryRights rights)
        {
            RegistryKey? subKey = registryKey.OpenSubKey(subkeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, rights);

            if (subKey == null)
                throw new KeyNotFoundException($"Subkey {subkeyName} not found.");

            return subKey;
        }

        // It's up to the caller to obtain the needed token privileges (TakeOwnership) for this operation
        public static void GrantFullControlOnSubKey(this RegistryKey registryKey, string subkeyName)
        {
            using RegistryKey subKey = registryKey.OpenSubKeyOrThrowIfMissing(subkeyName,
                RegistryRights.TakeOwnership | RegistryRights.ChangePermissions
            );
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

        // It's up to the caller to obtain the needed privileges (TakeOwnership) for this operation
        public static void TakeOwnershipOnSubKey(this RegistryKey registryKey, string subkeyName)
        {
            using RegistryKey subKey = registryKey.OpenSubKeyOrThrowIfMissing(subkeyName, RegistryRights.TakeOwnership);
            RegistrySecurity accessRules = subKey.GetAccessControl();
            accessRules.SetOwner(WindowsIdentity.GetCurrent().User);
            subKey.SetAccessControl(accessRules);
        }
    }
}
