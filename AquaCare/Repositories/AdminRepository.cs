using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace AquaCare.Repositories
{
    public class AdminRepository
    {
        private readonly string _connectionString;
        public AdminRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public class UserViewModel
        {
            public int UserId { get; set; }
            public string Login { get; set; }
            public string Role { get; set; }
            public bool IsSuperAdmin => string.Equals(Role, "Superadmin", StringComparison.OrdinalIgnoreCase);
            public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
            public bool IsUser => string.Equals(Role, "User", StringComparison.OrdinalIgnoreCase);

            public string RoleColor
            {
                get
                {
                    if (IsSuperAdmin) return "Purple";
                    if (IsAdmin) return "#D32F2F";
                    return "#424242";
                }
            }
            public bool CanPromote => IsUser;

            public bool CanDemote => IsAdmin;
            public bool CanDelete => !IsSuperAdmin;
        }
        public async Task<List<UserViewModel>> GetAllUsersAsync()
        {
            var users = new List<UserViewModel>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
        SELECT 
            u.UserId, 
            u.Username,
            STRING_AGG(r.RoleName, ', ') WITHIN GROUP (ORDER BY r.RoleName) as RolesList
        FROM Users u
        LEFT JOIN UserRoles ur ON u.UserId = ur.UserId
        LEFT JOIN Roles r ON ur.RoleId = r.RoleId
        WHERE u.Username != 'SuperAdmin'
        GROUP BY u.UserId, u.Username";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new UserViewModel
                {
                    UserId = reader.GetInt32(0),
                    Login = reader.GetString(1),
                    Role = reader.IsDBNull(2) ? "User" : reader.GetString(2)
                });
            }

            return users;
        }

        public async Task SetUserRoleAsync(int userId, string newRoleName)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var transaction = conn.BeginTransaction();

            try
            {
                string getRoleIdSql = "SELECT RoleId FROM Roles WHERE RoleName = @RoleName";
                await using var cmdGetId = new SqlCommand(getRoleIdSql, conn, transaction);
                cmdGetId.Parameters.AddWithValue("@RoleName", newRoleName);

                object roleIdObj = await cmdGetId.ExecuteScalarAsync();
                if (roleIdObj == null) throw new Exception($"Роль '{newRoleName}' не знайдена в БД!");
                int newRoleId = (int)roleIdObj;

                string deleteSql = "DELETE FROM UserRoles WHERE UserId = @UserId";
                await using var cmdDelete = new SqlCommand(deleteSql, conn, transaction);
                cmdDelete.Parameters.AddWithValue("@UserId", userId);
                await cmdDelete.ExecuteNonQueryAsync();

                string insertSql = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
                await using var cmdInsert = new SqlCommand(insertSql, conn, transaction);
                cmdInsert.Parameters.AddWithValue("@UserId", userId);
                cmdInsert.Parameters.AddWithValue("@RoleId", newRoleId);
                await cmdInsert.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task DeleteUserAsync(int userId)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var transaction = conn.BeginTransaction();

            try
            {
                var cmdRoles = new SqlCommand("DELETE FROM UserRoles WHERE UserId = @Id", conn, transaction);
                cmdRoles.Parameters.AddWithValue("@Id", userId);
                await cmdRoles.ExecuteNonQueryAsync();

                string cleanAquariumsSql = @"
            DELETE FROM Images WHERE DiseaseLogId IN (
            SELECT DiseaseLogId FROM DiseaseLogs WHERE 
            FishId IN (SELECT FishId FROM Fish WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id))
            OR 
            PlantId IN (SELECT PlantId FROM Plants WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id))
        );

            DELETE FROM Images WHERE WaterChangeId IN (
                SELECT WaterChangeId FROM WaterChanges WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id)
        );
        
            DELETE FROM Images WHERE BreedingLogId IN (
                SELECT BreedingLogId FROM BreedingLogs WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id)
        );
            DELETE FROM BreedingLogs WHERE MaleFishId IN (SELECT FishId FROM Fish WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id))
                                        OR FemaleFishId IN (SELECT FishId FROM Fish WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id));
            
            DELETE FROM DiseaseLogs WHERE FishId IN (SELECT FishId FROM Fish WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id));

            DELETE FROM Fish WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id);
            DELETE FROM Plants WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id);

            DELETE FROM WaterChanges WHERE AquariumId IN (SELECT AquariumId FROM Aquariums WHERE UserId = @Id);

            DELETE FROM Aquariums WHERE UserId = @Id;";

                var cmdCleanTanks = new SqlCommand(cleanAquariumsSql, conn, transaction);
                cmdCleanTanks.Parameters.AddWithValue("@Id", userId);
                await cmdCleanTanks.ExecuteNonQueryAsync();

                var cmdUser = new SqlCommand("DELETE FROM Users WHERE UserId = @Id", conn, transaction);
                cmdUser.Parameters.AddWithValue("@Id", userId);
                await cmdUser.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<(int TotalUsers, int TotalTanks, int TotalFish)> GetGlobalStatsAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
            SELECT COUNT(*) FROM Users;
            SELECT COUNT(*) FROM Aquariums;
            SELECT COUNT(*) FROM Fish WHERE IsAlive = 1;";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            await reader.ReadAsync();
            int users = reader.GetInt32(0);

            await reader.NextResultAsync();
            await reader.ReadAsync();
            int tanks = reader.GetInt32(0);

            await reader.NextResultAsync();
            await reader.ReadAsync();
            int fishes = reader.GetInt32(0);

            return (users, tanks, fishes);
        }

        public async Task<List<string>> GetTopFishSpeciesAsync()
        {
            var list = new List<string>();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
        SELECT TOP 3 s.SpeciesName, COUNT(f.FishId) as Count
        FROM Fish f
        JOIN FishSpecies s ON f.SpeciesId = s.FishSpecieId
        WHERE f.IsAlive = 1
        GROUP BY s.SpeciesName
        ORDER BY Count DESC";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add($"{reader.GetString(0)} ({reader.GetInt32(1)} шт.)");
            }
            return list;
        }

        public async Task<List<string>> GetTopPlantSpeciesAsync()
        {
            var list = new List<string>();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string sql = @"
        SELECT TOP 3 ps.SpeciesName, SUM(p.Quantity) as TotalCount
        FROM Plants p
        JOIN PlantSpecies ps ON p.SpeciesId = ps.PlantSpecieId
        GROUP BY ps.SpeciesName
        ORDER BY TotalCount DESC";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int count = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                list.Add($"{reader.GetString(0)} ({count} шт.)");
            }
            return list;
        }
        public async Task<(string Name, int Count)> GetTopDiseaseAsync()
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
        SELECT TOP 1 d.Name, COUNT(dl.DiseaseLogId) as CaseCount
        FROM DiseaseLogs dl
        JOIN Diseases d ON dl.DiseaseId = d.DiseaseId
        GROUP BY d.Name
        ORDER BY CaseCount DESC";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return (reader.GetString(0), reader.GetInt32(1));
            }
            return ("Немає хвороб", 0);
        }
        public async Task<(string Species, int TotalFry)> GetTopBreedingSpeciesAsync()
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
        SELECT TOP 1 s.SpeciesName, SUM(bl.OffspringCount) as TotalFry
        FROM BreedingLogs bl
        JOIN Fish f ON bl.FemaleFishId = f.FishId
        JOIN FishSpecies s ON f.SpeciesId = s.FishSpecieId
        WHERE bl.OffspringCount IS NOT NULL
        GROUP BY s.SpeciesName
        ORDER BY TotalFry DESC";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return (reader.GetString(0), reader.GetInt32(1));
            }
            return ("Немає даних", 0);
        }

        public async Task<int> GetTotalSystemVolumeAsync()
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT SUM(Volume) FROM Aquariums";
            await using var cmd = new SqlCommand(sql, conn);

            var result = await cmd.ExecuteScalarAsync();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }
        public async Task<List<CompatibilityViewModel>> GetAllRulesAsync()
        {
            var list = new List<CompatibilityViewModel>();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string sql = @"
        SELECT 
            r.CompatibilityRuleId, 
            s1.SpeciesName, 
            s2.SpeciesName, 
            r.CompatabilityLevel, 
            r.Notes
        FROM CompatabilityRules r
        JOIN FishSpecies s1 ON r.Species1Id = s1.FishSpecieId
        JOIN FishSpecies s2 ON r.Species2Id = s2.FishSpecieId";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new CompatibilityViewModel
                {
                    RuleId = reader.GetInt32(0),
                    Species1Name = reader.GetString(1),
                    Species2Name = reader.GetString(2),
                    Level = reader.GetInt32(3),
                    Notes = reader.IsDBNull(4) ? "" : reader.GetString(4)
                });
            }
            return list;
        }

        public async Task AddRuleAsync(int s1, int s2, int level, string note)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
        INSERT INTO CompatabilityRules (Species1Id, Species2Id, CompatabilityLevel, Notes) 
        VALUES (@S1, @S2, @Lvl, @Note)";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@S1", s1);
            cmd.Parameters.AddWithValue("@S2", s2);
            cmd.Parameters.AddWithValue("@Lvl", level);
            cmd.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task DeleteRuleAsync(int ruleId)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string sql = "DELETE FROM CompatabilityRules WHERE CompatibilityRuleId = @Id";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", ruleId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
