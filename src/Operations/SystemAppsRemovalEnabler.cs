using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class SystemAppsRemovalEnabler : IOperation
    {
        private enum DatabaseEditingOutcome
        {
            NoRowsUpdated,
            SomeRowsUpdated
        }

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
                var (databaseBackupCopy, databaseCopyForEditing) = CreateStateRepositoryDatabaseCopies();
                var outcome = EditStateRepositoryDatabase(databaseCopyForEditing);
                if (outcome == DatabaseEditingOutcome.SomeRowsUpdated)
                    ReplaceStateRepositoryDatabaseWith(databaseCopyForEditing);
                else
                {
                    ConsoleUtils.WriteLine("\nOriginal database doesn't need to be replaced: nothing has changed.", ConsoleColor.Cyan);
                    DeleteDatabaseCopies(databaseBackupCopy, databaseCopyForEditing);
                }
            }
        }

        private void EnsureAppXServicesAreStopped()
        {
            Console.WriteLine("Making sure AppX-related services are stopped before proceeding...");
            SystemUtils.StopServiceAndItsDependents("StateRepository");
        }

        private (string, string) CreateStateRepositoryDatabaseCopies()
        {
            EnsureAppXServicesAreStopped();
            string databaseBackupCopy = BackupStateRepositoryDatabase();
            string databaseCopyForEditing = CopyStateRepositoryDatabaseTo($"{STATE_REPOSITORY_DB_NAME}.tmp");
            return (databaseBackupCopy, databaseCopyForEditing);
        }

        private string BackupStateRepositoryDatabase()
        {
            ConsoleUtils.WriteLine("\nBacking up state repository database...", ConsoleColor.Green);
            string backupCopyFileName = $"StateRepository-Machine_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.srd.bak";
            string backupCopyPath = CopyStateRepositoryDatabaseTo(backupCopyFileName);
            Console.WriteLine($"Backup copy written to {backupCopyPath}.");
            return backupCopyPath;
        }

        private string CopyStateRepositoryDatabaseTo(string databaseCopyPath)
        {
            var database = new FileInfo(STATE_REPOSITORY_DB_PATH);
            FileInfo copiedDatabase = database.CopyTo(databaseCopyPath, overwrite: true);
            return copiedDatabase.FullName;
        }

        private DatabaseEditingOutcome EditStateRepositoryDatabase(string databaseCopyPath)
        {
            ConsoleUtils.WriteLine("\nEditing a temporary copy of state repository database...", ConsoleColor.Green);
            using (dbConnection = new SqliteConnection($"Data Source={databaseCopyPath}"))
            {
                dbConnection.Open();

                // There's an AFTER UPDATE trigger on the table we want to edit that causes problems,
                // so we must prevent it from being run
                string? createTriggerCode = RetrieveAfterPackageUpdateTriggerCode();
                DeleteAfterPackageUpdateTrigger();
                int updatedRows = EditPackageTable();
                if (createTriggerCode != null)
                    ReAddAfterPackageUpdateTrigger(createTriggerCode);

                return updatedRows == 0 ? DatabaseEditingOutcome.NoRowsUpdated : DatabaseEditingOutcome.SomeRowsUpdated;
            }
        }

        private string? RetrieveAfterPackageUpdateTriggerCode()
        {
            using var query = new SqliteCommand(
                $"SELECT sql FROM sqlite_master WHERE name='{AFTER_PACKAGE_UPDATE_TRIGGER_NAME}'",
                dbConnection
            );
            return (string?) query.ExecuteScalar();
        }

        private void DeleteAfterPackageUpdateTrigger()
        {
            using var query = new SqliteCommand(
                $"DROP TRIGGER IF EXISTS {AFTER_PACKAGE_UPDATE_TRIGGER_NAME}",
                dbConnection
            );
            query.ExecuteNonQuery();
        }

        private int EditPackageTable()
        {
            using var query = new SqliteCommand(
                "UPDATE Package SET IsInbox=0 WHERE IsInbox=1",
                dbConnection
            );
            int updatedRows = query.ExecuteNonQuery();
            Console.WriteLine($"Edited {updatedRows} row(s).");
            return updatedRows;
        }

        private void ReAddAfterPackageUpdateTrigger(string createTriggerQuery)
        {
            using var query = new SqliteCommand(createTriggerQuery, dbConnection);
            query.ExecuteNonQuery();
        }

        private void ReplaceStateRepositoryDatabaseWith(string databaseCopyPath)
        {
            ConsoleUtils.WriteLine("\nReplacing original state repository database with the edited copy...", ConsoleColor.Green);
            EnsureAppXServicesAreStopped();
            try
            {
                // File.Copy can't be used to replace the file because it fails with access denied
                // even though we have Restore privilege, so we need to use File.Move instead
                File.Move(databaseCopyPath, STATE_REPOSITORY_DB_PATH, overwrite: true);
                Console.WriteLine("Replacement successful.");
            }
            catch
            {
                File.Delete(databaseCopyPath);
                throw;
            }
        }

        private void DeleteDatabaseCopies(params string[] databaseCopiesPaths)
        {
            foreach (string databaseCopy in databaseCopiesPaths)
                File.Delete(databaseCopy);
            
            ConsoleUtils.WriteLine("Database backup copy was unnecessary and therefore has been removed.", ConsoleColor.Cyan);
        }
    }
}
