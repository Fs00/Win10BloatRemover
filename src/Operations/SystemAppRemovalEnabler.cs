using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class SystemAppRemovalEnabler : IOperation
    {
        private const string STATE_REPOSITORY_DB_PATH =
            @"C:\ProgramData\Microsoft\Windows\AppRepository\StateRepository-Machine.srd";
        private const string AFTER_PACKAGE_UPDATE_TRIGGER_NAME = "TRG_AFTER_UPDATE_Package_SRJournal";

        private SqliteConnection dbConnection;

        public void PerformTask()
        {
            StopAppXServices();
            GrantPermissionsOnAppRepository();
            BackupStateRepositoryDatabase();
            EditStateRepositoryDatabase();
            RestartAppXServices();
        }

        private void StopAppXServices()
        {
            ConsoleUtils.WriteLine("Stopping AppX-related services...", ConsoleColor.Green);

            SystemUtils.StopService("AppXSVC");
            Console.WriteLine("Service AppXSVC stopped successfully.");

            SystemUtils.StopService("StateRepository");
            Console.WriteLine("Service StateRepository stopped successfully.");
        }

        private void RestartAppXServices()
        {
            ConsoleUtils.WriteLine("\nRestarting AppX-related services...", ConsoleColor.Green);
            SystemUtils.StartService("AppXSVC");
            Console.WriteLine("Services restarted successfully.");
        }

        private void GrantPermissionsOnAppRepository()
        {
            ConsoleUtils.WriteLine("\nGranting needed permissions...", ConsoleColor.Green);
            PrivilegeUtils.GrantFullControlOnDirectory(@"C:\ProgramData\Microsoft\Windows\AppRepository");
            PrivilegeUtils.GrantFullControlOnFile(STATE_REPOSITORY_DB_PATH);
        }

        // Exceptions are not caught so if backup fails, the entire operation will stop
        private void BackupStateRepositoryDatabase()
        {
            ConsoleUtils.WriteLine("\nBacking up state repository database...", ConsoleColor.Green);

            string backupFilePath = $"./StateRepository-Machine_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.srd";
            File.Copy(STATE_REPOSITORY_DB_PATH, backupFilePath);
            Console.WriteLine($"Backup file written to {backupFilePath}.");
        }

        /*
         *  Before performing the actual edits, we backup and remove a trigger that runs after
         *  every UPDATE executed on the table we are going to edit, since it causes problems.
         *  After the modifications, the trigger is added back into the database.
         */
        private void EditStateRepositoryDatabase()
        {
            ConsoleUtils.WriteLine("\nEditing state repository database...", ConsoleColor.Green);

            using (dbConnection = new SqliteConnection($"Data Source={STATE_REPOSITORY_DB_PATH}"))
            {
                dbConnection.Open();

                string triggerCode = RetrieveAfterPackageUpdateTriggerCode();
                DeleteAfterPackageUpdateTrigger();
                EditPackageTable();
                if (triggerCode != null)
                    ReAddAfterPackageUpdateTrigger(triggerCode);
            }
        }

        private string RetrieveAfterPackageUpdateTriggerCode()
        {
            SqliteCommand query = new SqliteCommand(
                $"SELECT sql FROM sqlite_master WHERE name='{AFTER_PACKAGE_UPDATE_TRIGGER_NAME}'",
                dbConnection
            );
            return (string) query.ExecuteScalar();
        }

        private void DeleteAfterPackageUpdateTrigger()
        {
            SqliteCommand query = new SqliteCommand(
                $"DROP TRIGGER IF EXISTS {AFTER_PACKAGE_UPDATE_TRIGGER_NAME}",
                dbConnection
            );
            query.ExecuteNonQuery();
        }

        private void EditPackageTable()
        {
            SqliteCommand query = new SqliteCommand(
                $"UPDATE Package SET IsInbox=0 WHERE IsInbox=1",
                dbConnection
            );
            int updatedRows = query.ExecuteNonQuery();
            Console.WriteLine($"Edited {updatedRows} row(s).");
        }

        private void ReAddAfterPackageUpdateTrigger(string triggerCode)
        {
            SqliteCommand query = new SqliteCommand(triggerCode, dbConnection);
            query.ExecuteNonQuery();
        }
    }
}
