using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

namespace Win10BloatRemover.Utils;

static class RegistryUtils
{
    private const string DEFAULT_USER_HIVE_PATH = @"HKEY_USERS\_loaded_Default";
    private static RegistryKey? defaultUserHiveKey;
    private static RegistryKey? localMachine64BitView;

    extension(Registry)
    {
        public static RegistryKey LocalMachine64 =>
            localMachine64BitView ??= RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

        public static RegistryKey DefaultUser => defaultUserHiveKey ??= LoadDefaultUserHive();

        public static void SetForCurrentAndDefaultUser(string keyPath, string? valueName, object value)
        {
            Registry.SetValue($@"HKEY_CURRENT_USER\{keyPath}", valueName, value);
            using (RegistryKey key = Registry.DefaultUser.CreateSubKey(keyPath))
                key.SetValue(valueName, value);
        }
    }
    
    private static RegistryKey LoadDefaultUserHive()
    {
        var loadExitCode = OS.RunProcessBlocking(
            OS.SystemExecutablePath("reg"), $@"load ""{DEFAULT_USER_HIVE_PATH}"" ""C:\Users\Default\NTUSER.DAT"""
        );
        if (loadExitCode.IsNotSuccessful())
            throw new Exception("Unable to load Default user registry hive.");

        AppDomain.CurrentDomain.ProcessExit += (_, _) => UnloadDefaultUserHive();
        return Registry.Users.OpenSubKeyWritable("_loaded_Default");
    }

    private static void UnloadDefaultUserHive()
    {
        defaultUserHiveKey?.Close();
        OS.RunProcessBlocking(OS.SystemExecutablePath("reg"), $@"unload ""{DEFAULT_USER_HIVE_PATH}""");
    }

    extension(RegistryKey registryKey)
    {
        public bool HasValue(string valueName) => registryKey.GetValue(valueName) != null;

        public void DeleteSubKeyValue(string subkeyName, string valueName)
        {
            using RegistryKey? subKey = registryKey.OpenSubKey(subkeyName, writable: true);
            subKey?.DeleteValue(valueName, throwOnMissingValue: false);
        }

        public RegistryKey OpenSubKeyWritable(string subkeyName, RegistryRights? rights = null)
        {
            RegistryKey? subKey;
            if (rights == null)
                subKey = registryKey.OpenSubKey(subkeyName, RegistryKeyPermissionCheck.ReadWriteSubTree);
            else
                subKey = registryKey.OpenSubKey(subkeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, rights.Value);

            if (subKey == null)
                throw new KeyNotFoundException($"Subkey {subkeyName} not found.");

            return subKey;
        }

        // Obtaining the needed token privileges (TakeOwnership) for this operation is up to the caller
        public void GrantFullControlOnSubKey(string subkeyName)
        {
            using RegistryKey subKey = registryKey.OpenSubKeyWritable(subkeyName,
                RegistryRights.TakeOwnership | RegistryRights.ChangePermissions
            );
            RegistrySecurity accessRules = subKey.GetAccessControl();
            SecurityIdentifier currentUser = RetrieveCurrentUserIdentifier();
            accessRules.SetOwner(currentUser);
            accessRules.ResetAccessRule(
                new RegistryAccessRule(
                    currentUser,
                    RegistryRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow
                )
            );
            subKey.SetAccessControl(accessRules);
        }

        // Obtaining the needed privileges (TakeOwnership) for this operation is up to the caller
        public void TakeOwnershipOnSubKey(string subkeyName)
        {
            using RegistryKey subKey = registryKey.OpenSubKeyWritable(subkeyName, RegistryRights.TakeOwnership);
            RegistrySecurity accessRules = subKey.GetAccessControl();
            accessRules.SetOwner(RetrieveCurrentUserIdentifier());
            subKey.SetAccessControl(accessRules);
        }
    }

    private static SecurityIdentifier RetrieveCurrentUserIdentifier() 
        => WindowsIdentity.GetCurrent().User ?? throw new Exception("Unable to retrieve current user SID.");
}
