using AquaCareClasses;
using AquaClasses;
using System;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace AquaCare.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<(User User, string RoleName, string PasswordHash)?> GetUserForLoginAsync(string username, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, r.RoleName
                FROM Users u
                JOIN UserRoles ur ON u.UserId = ur.UserId
                JOIN Roles r ON ur.RoleId = r.RoleId
                WHERE u.Username = @Name";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Name", username));

            await using var reader = await command.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
            {
                var user = new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                };
                string hash = reader.GetString(2);
                string role = reader.GetString(3);

                return (user, role, hash);
            }

            return null;
        }
        public async Task RegisterUserAsync(string username, string passwordHash, string roleName = "User", CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string sqlRole = "SELECT RoleId FROM Roles WHERE RoleName = @RoleName";
                int roleId;

                await using (var cmdRole = new SqlCommand(sqlRole, connection, transaction))
                {
                    cmdRole.Parameters.Add(new SqlParameter("@RoleName", roleName));
                    var result = await cmdRole.ExecuteScalarAsync(token);

                    if (result == null)
                        throw new Exception($"Роль '{roleName}' не знайдена в БД.");

                    roleId = (int)result;
                }

                string sqlUser = @"
            INSERT INTO Users (Username, PasswordHash) 
            VALUES (@Name, @Hash);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newUserId;

                await using (var cmdUser = new SqlCommand(sqlUser, connection, transaction))
                {
                    cmdUser.Parameters.Add(new SqlParameter("@Name", username));
                    cmdUser.Parameters.Add(new SqlParameter("@Hash", passwordHash));

                    newUserId = (int)await cmdUser.ExecuteScalarAsync(token);
                }
                string sqlUserRole = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UId, @RId)";

                await using (var cmdUR = new SqlCommand(sqlUserRole, connection, transaction))
                {
                    cmdUR.Parameters.Add(new SqlParameter("@UId", newUserId));
                    cmdUR.Parameters.Add(new SqlParameter("@RId", roleId));
                    await cmdUR.ExecuteNonQueryAsync(token);
                }

                await transaction.CommitAsync(token);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async Task<string?> GetUserRoleNameAsync(int userId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            string sql = @"
        SELECT r.RoleName 
        FROM UserRoles ur
        JOIN Roles r ON ur.RoleId = r.RoleId
        WHERE ur.UserId = @UserId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@UserId", userId));

            var result = await command.ExecuteScalarAsync();

            return result?.ToString();
        }
        public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "SELECT COUNT(1) FROM Users WHERE Username = @Username";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Username", username));

            int count = (int)await command.ExecuteScalarAsync(token);
            return count > 0;
        }
    }

}
