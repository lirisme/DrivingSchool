using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;

namespace DrivingSchool.Services
{
    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _backupFolder;

        public BackupService(string connectionString)
        {
            _connectionString = connectionString;
            _backupFolder = @"C:\DrivingSchoolBackups";

            if (!Directory.Exists(_backupFolder))
                Directory.CreateDirectory(_backupFolder);
        }

        public string CreateBackup()
        {
            try
            {
                var backupName = $"DrivingSchool_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var backupPath = Path.Combine(_backupFolder, backupName);

                var builder = new SqlConnectionStringBuilder(_connectionString);
                string databaseName = builder.InitialCatalog;

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupPath}' WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                DeleteOldBackups(30);
                return backupPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания бэкапа: {ex.Message}");
            }
        }

        private void DeleteOldBackups(int daysToKeep)
        {
            try
            {
                var files = Directory.GetFiles(_backupFolder, "DrivingSchool_Backup_*.bak");
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        fileInfo.Delete();
                    }
                }
            }
            catch { }
        }

        public string GetBackupFolder() => _backupFolder;

        public string RestoreBackup(string backupPath)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                string databaseName = builder.InitialCatalog;

                builder.InitialCatalog = "master";

                using (var conn = new SqlConnection(builder.ConnectionString))
                {
                    conn.Open();

                    string sql = $@"
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                
                RESTORE DATABASE [{databaseName}] 
                FROM DISK = '{backupPath}' 
                WITH REPLACE;
                
                ALTER DATABASE [{databaseName}] SET MULTI_USER;";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                return $"База данных восстановлена из {backupPath}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка восстановления: {ex.Message}");
            }
        }

        public List<string> GetBackupFiles()
        {
            try
            {
                var files = Directory.GetFiles(_backupFolder, "DrivingSchool_Backup_*.bak");
                return files.OrderByDescending(f => f).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}