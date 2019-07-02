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
            ShellUtils.ExecuteWindowsCommand(
                "sc stop AppXSVC > nul && " +
                "echo Service AppXSVC stopped successfully."
            );
            ShellUtils.ExecuteWindowsCommand(
                "sc stop StateRepository > nul && " +
                "echo Service StateRepository stopped successfully."
            );
        }

        private void RestartAppXServices()
        {
            ConsoleUtils.WriteLine("\nRestarting AppX-related services...", ConsoleColor.Green);
            ShellUtils.ExecuteWindowsCommand("sc start AppXSVC > nul && echo Services restarted successfully.");
        }

        private void GrantPermissionsOnAppRepository()
        {
            ConsoleUtils.WriteLine("\nGranting needed permissions...", ConsoleColor.Green);
            PrivilegeUtils.GrantFullControlOnDirectory(@"C:\ProgramData\Microsoft\Windows\AppRepository");
            PrivilegeUtils.GrantFullControlOnFile(STATE_REPOSITORY_DB_PATH);
        }

        // If backup fails, the entire operation will stop
        private void BackupStateRepositoryDatabase()
        {
            ConsoleUtils.WriteLine("\nBacking up state repository database...", ConsoleColor.Green);

            string backupFilePath = $"./StateRepository-Machine_{DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss")}.srd";
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

            using (SqliteConnection dbConnection = new SqliteConnection($"Data Source={STATE_REPOSITORY_DB_PATH}"))
            {
                dbConnection.Open();

                string triggerCode = RetrieveAfterPackageUpdateTriggerCode(dbConnection);
                DeleteAfterPackageUpdateTrigger(dbConnection);
                EditPackageTable(dbConnection);
                if (triggerCode != null)
                    ReAddAfterPackageUpdateTrigger(triggerCode, dbConnection);
            }
        }

        private string RetrieveAfterPackageUpdateTriggerCode(SqliteConnection dbConnection)
        {
            SqliteCommand query = new SqliteCommand(
                $"SELECT sql FROM sqlite_master WHERE name='{AFTER_PACKAGE_UPDATE_TRIGGER_NAME}'",
                dbConnection
            );
            return (string) query.ExecuteScalar();
        }

        private void DeleteAfterPackageUpdateTrigger(SqliteConnection dbConnection)
        {
            SqliteCommand query = new SqliteCommand(
                $"DROP TRIGGER IF EXISTS {AFTER_PACKAGE_UPDATE_TRIGGER_NAME}",
                dbConnection
            );
            query.ExecuteNonQuery();
        }

        private void EditPackageTable(SqliteConnection dbConnection)
        {
            SqliteCommand query = new SqliteCommand(
                $"UPDATE Package SET IsInbox=0 WHERE IsInbox=1",
                dbConnection
            );
            int updatedRows = query.ExecuteNonQuery();
            Console.WriteLine($"Edited {updatedRows} rows.");
        }

        private void ReAddAfterPackageUpdateTrigger(string triggerCode, SqliteConnection dbConnection)
        {
            SqliteCommand query = new SqliteCommand(triggerCode, dbConnection);
            query.ExecuteNonQuery();
        }
    }
}
