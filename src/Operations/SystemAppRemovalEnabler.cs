using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class SystemAppRemovalEnabler : IOperation
    {
        private const string STATE_REPOSITORY_DB_NAME = "StateRepository-Machine.srd";
        private const string STATE_REPOSITORY_DB_PATH =
            @"C:\ProgramData\Microsoft\Windows\AppRepository\" + STATE_REPOSITORY_DB_NAME;
        private const string AFTER_PACKAGE_UPDATE_TRIGGER_NAME = "TRG_AFTER_UPDATE_Package_SRJournal";

        #nullable disable warnings
        private /*lateinit*/ SqliteConnection dbConnection;
        #nullable restore warnings

        public void PerformTask()
        {
            using (TokenPrivilege.Backup)
            using (TokenPrivilege.Restore)
            {
                StopAppXServices();
                BackupStateRepositoryDatabase();
                string databaseCopyPath = CopyStateRepositoryDatabase();
                EditStateRepositoryDatabase(databaseCopyPath);
                ReplaceStateRepositoryDatabaseWith(databaseCopyPath);
                RestartAppXServices();
            }
        }

        private void StopAppXServices()
        {
            ConsoleUtils.WriteLine("Stopping AppX-related services...", ConsoleColor.Green);
            SystemUtils.StopServiceAndItsDependents("StateRepository");
            Console.WriteLine("AppX-related services stopped successfully.");
        }

        private void RestartAppXServices()
        {
            ConsoleUtils.WriteLine("\nRestarting AppX-related services...", ConsoleColor.Green);
            // AppXSVC depends on StateRepository, so starting the first will automatically start the latter too
            SystemUtils.StartService("AppXSVC");
            Console.WriteLine("Services restarted successfully.");
        }

        private void BackupStateRepositoryDatabase()
        {
            ConsoleUtils.WriteLine("\nBacking up state repository database...", ConsoleColor.Green);
            string backupFilePath = $"./StateRepository-Machine_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.srd.bak";
            File.Copy(STATE_REPOSITORY_DB_PATH, backupFilePath);
            Console.WriteLine($"Backup file written to {backupFilePath}.");
        }

        private string CopyStateRepositoryDatabase()
        {
            var stateRepositoryDatabase = new FileInfo(STATE_REPOSITORY_DB_PATH);
            FileInfo copiedDatabase = stateRepositoryDatabase.CopyTo(STATE_REPOSITORY_DB_NAME, overwrite: true);
            return copiedDatabase.FullName;
        }

        private void EditStateRepositoryDatabase(string databaseCopyPath)
        {
            ConsoleUtils.WriteLine("\nEditing state repository database...", ConsoleColor.Green);
            using (dbConnection = new SqliteConnection($"Data Source={databaseCopyPath}"))
            {
                dbConnection.Open();

                // Before performing the actual edits, we need to "temporary disable" a trigger that runs after
                // every UPDATE executed on the table we are going to edit, since it causes problems.
                string triggerCode = RetrieveAfterPackageUpdateTriggerCode();
                DeleteAfterPackageUpdateTrigger();
                EditPackageTable();
                if (triggerCode != null)
                    ReAddAfterPackageUpdateTrigger(triggerCode);
            }
        }

        private string RetrieveAfterPackageUpdateTriggerCode()
        {
            using var query = new SqliteCommand(
                $"SELECT sql FROM sqlite_master WHERE name='{AFTER_PACKAGE_UPDATE_TRIGGER_NAME}'",
                dbConnection
            );
            return (string) query.ExecuteScalar();
        }

        private void DeleteAfterPackageUpdateTrigger()
        {
            using var query = new SqliteCommand(
                $"DROP TRIGGER IF EXISTS {AFTER_PACKAGE_UPDATE_TRIGGER_NAME}",
                dbConnection
            );
            query.ExecuteNonQuery();
        }

        private void EditPackageTable()
        {
            using var query = new SqliteCommand(
                "UPDATE Package SET IsInbox=0 WHERE IsInbox=1",
                dbConnection
            );
            int updatedRows = query.ExecuteNonQuery();
            Console.WriteLine($"Edited {updatedRows} row(s).");
        }

        private void ReAddAfterPackageUpdateTrigger(string triggerCode)
        {
            using var query = new SqliteCommand(triggerCode, dbConnection);
            query.ExecuteNonQuery();
        }

        private void ReplaceStateRepositoryDatabaseWith(string databaseCopyPath)
        {
            File.Move(databaseCopyPath, STATE_REPOSITORY_DB_PATH, overwrite: true);
        }
    }
}
