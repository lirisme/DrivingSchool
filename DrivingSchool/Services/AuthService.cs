using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DrivingSchool.Models;

namespace DrivingSchool.Services
{
    public class AuthService
    {
        private readonly string _connectionString;

        public AuthService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public async Task<bool> HasAnyUsers()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var sql = "SELECT COUNT(*) FROM Users";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    return (int)await cmd.ExecuteScalarAsync() > 0;
                }
            }
        }

        public async Task<User> Authenticate(string login, string password)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var sql = "SELECT Id, Login, Role, FullName, PasswordHash FROM Users WHERE Login = @Login AND IsActive = 1";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Login", login);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var storedHash = reader.GetString(4);
                            var inputHash = HashPassword(password);

                            if (inputHash == storedHash)
                            {
                                return new User
                                {
                                    Id = reader.GetInt32(0),
                                    Login = reader.GetString(1),
                                    Role = reader.GetString(2),
                                    FullName = reader.GetString(3)
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }

        public async Task<(bool Success, string Message)> CreateUser(string login, string password, string fullName, string role)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var checkSql = "SELECT COUNT(*) FROM Users WHERE Login = @Login";
                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Login", login);
                    var exists = (int)await checkCmd.ExecuteScalarAsync();
                    if (exists > 0)
                        return (false, "Логин уже существует");
                }

                var passwordHash = HashPassword(password);

                var sql = @"INSERT INTO Users (Login, PasswordHash, Salt, FullName, Role, IsActive, CreatedDate) 
                            VALUES (@Login, @Hash, '', @FullName, @Role, 1, @Date)";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Login", login);
                    cmd.Parameters.AddWithValue("@Hash", passwordHash);
                    cmd.Parameters.AddWithValue("@FullName", fullName);
                    cmd.Parameters.AddWithValue("@Role", role);
                    cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return (true, "Пользователь создан");
        }
    }
}