using AquaCareClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using DbFish = AquaCareClasses.Fish;

namespace AquaCare.Repositories
{
    public class BreedingRepository
    {
        private readonly string _connectionString;

        public BreedingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<List<ParentDto>> GetPotentialParentsAsync(string gender, int userId, CancellationToken token = default)
        {
            var list = new List<ParentDto>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
                SELECT f.FishId, f.FishName, ISNULL(a.Name, 'Без акваріума') as AquariumName, f.SpeciesId
                FROM Fish f
                LEFT JOIN Aquariums a ON f.AquariumId = a.AquariumId
                WHERE f.Gender LIKE @Gender + '%' AND f.IsAlive = 1 AND a.UserId = @UserId
                ORDER BY f.FishName";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Gender", gender);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                list.Add(new ParentDto
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    AquariumName = reader.GetString(2),
                    SpeciesId = reader.GetInt32(3)
                });
            }
            return list;
        }

        public async Task<int> AddLogAsync(BreedingLog log, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
                INSERT INTO BreedingLogs (
                    AquariumId, 
                    MaleFishId, 
                    FemaleFishId, 
                    StartDate, 
                    Outcome, 
                    OffspringCount
                )
                VALUES (
                    @AqId, 
                    @MaleId, 
                    @FemaleId, 
                    @Start, 
                    @Outcome, 
                    @Count
                )
                SELECT SCOPE_IDENTITY();";

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@AqId", log.AquariumId);
            command.Parameters.AddWithValue("@MaleId", log.MaleFishId);
            command.Parameters.AddWithValue("@FemaleId", log.FemaleFishId);
            command.Parameters.AddWithValue("@Start", log.StartDate);

            command.Parameters.AddWithValue("@Outcome", (object)log.Outcome ?? DBNull.Value);

            command.Parameters.AddWithValue("@Count", (object)log.OffspringCount ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync(token);

            return Convert.ToInt32(result);
        }

        public async Task DeleteLogAsync(int logId, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string deleteImagesSql = "DELETE FROM Images WHERE BreedingLogId = @Id";
                await using (var cmdImg = new SqlCommand(deleteImagesSql, connection, transaction))
                {
                    cmdImg.Parameters.AddWithValue("@Id", logId);
                    await cmdImg.ExecuteNonQueryAsync(token);
                }

                string deleteLogSql = "DELETE FROM BreedingLogs WHERE BreedingLogId = @Id";
                await using (var cmdLog = new SqlCommand(deleteLogSql, connection, transaction))
                {
                    cmdLog.Parameters.AddWithValue("@Id", logId);
                    await cmdLog.ExecuteNonQueryAsync(token);
                }

                await transaction.CommitAsync(token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async Task<List<BreedingLog>> GetLogsByAquariumIdAsync(int aquariumId, CancellationToken token = default)
        {
            var list = new List<BreedingLog>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
                SELECT 
                    b.BreedingLogId, b.StartDate, b.Outcome, b.OffspringCount,
                    b.MaleFishId, m.FishName AS MaleName,
                    b.FemaleFishId, f.FishName AS FemaleName
                FROM BreedingLogs b
                LEFT JOIN Fish m ON b.MaleFishId = m.FishId
                LEFT JOIN Fish f ON b.FemaleFishId = f.FishId
                WHERE b.AquariumId = @AqId
                ORDER BY b.StartDate DESC";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AqId", aquariumId);

            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                var log = new BreedingLog
                {
                    BreedingLogId = reader.GetInt32(0),
                    StartDate = reader.GetDateTime(1),

                    Outcome = reader.IsDBNull(2) ? null : reader.GetString(2),
                    OffspringCount = reader.IsDBNull(3) ? null : reader.GetInt32(3),

                    MaleFishId = reader.IsDBNull(4) ? null : reader.GetInt32(4),

                    MaleFish = new AquaCareClasses.Fish
                    {
                        FishName = reader.IsDBNull(5) ? "Невідомо (Видалено)" : reader.GetString(5)
                    },

                    FemaleFishId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    FemaleFish = new AquaCareClasses.Fish
                    {
                        FishName = reader.IsDBNull(7) ? "Невідомо (Видалено)" : reader.GetString(7)
                    }
                };
                list.Add(log);
            }
            return list;
        }
        public async Task UpdateLogAsync(BreedingLog log, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        UPDATE BreedingLogs 
        SET 
            StartDate = @Start,
            MaleFishId = @MaleId,
            FemaleFishId = @FemaleId,
            OffspringCount = @Count,
            Outcome = @Outcome
        WHERE BreedingLogId = @Id";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", log.BreedingLogId);
            command.Parameters.AddWithValue("@Start", log.StartDate);
            command.Parameters.AddWithValue("@MaleId", (object)log.MaleFishId ?? DBNull.Value);
            command.Parameters.AddWithValue("@FemaleId", (object)log.FemaleFishId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Count", (object)log.OffspringCount ?? DBNull.Value);
            command.Parameters.AddWithValue("@Outcome", (object)log.Outcome ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(token);
        }

        public async Task AddPhotoAsync(int breedingLogId, string imagePath, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "INSERT INTO Images (BreedingLogId, ImagePath) VALUES (@LogId, @Path)";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@LogId", breedingLogId);
            command.Parameters.AddWithValue("@Path", imagePath);
            await command.ExecuteNonQueryAsync(token);
        }

        public async Task<List<string>> GetPhotosAsync(int breedingLogId, CancellationToken token = default)
        {
            var list = new List<string>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "SELECT ImagePath FROM Images WHERE BreedingLogId = @LogId";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@LogId", breedingLogId);

            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                list.Add(reader.GetString(0));
            }
            return list;
        }
        public async Task SettleFryAsync(int logId, int targetAquariumId, int speciesId, int count, string baseName, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string updateLogSql = @"
            UPDATE BreedingLogs 
            SET Outcome = ISNULL(Outcome, '') + N' [Заселено ' + CAST(@Count AS NVARCHAR) + N' шт.]' 
            WHERE BreedingLogId = @LogId";

                await using (var cmd1 = new SqlCommand(updateLogSql, connection, transaction))
                {
                    cmd1.Parameters.AddWithValue("@LogId", logId);
                    cmd1.Parameters.AddWithValue("@Count", count);
                    await cmd1.ExecuteNonQueryAsync(token);
                }
                string insertFishSql = @"
            INSERT INTO Fish (AquariumId, SpeciesId, FishName, Gender, IsAlive)
            VALUES (@AqId, @SpecId, @Name, 'Unknown', 1)";

                for (int i = 1; i <= count; i++)
                {
                    await using var cmdFish = new SqlCommand(insertFishSql, connection, transaction);
                    cmdFish.Parameters.AddWithValue("@AqId", targetAquariumId);
                    cmdFish.Parameters.AddWithValue("@SpecId", speciesId);
                    cmdFish.Parameters.AddWithValue("@Name", $"{baseName} #{i}");
                    await cmdFish.ExecuteNonQueryAsync(token);
                }

                await transaction.CommitAsync(token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async Task<int?> GetFishSpeciesIdAsync(int fishId, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "SELECT SpeciesId FROM Fish WHERE FishId = @Id";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", fishId);

            var result = await command.ExecuteScalarAsync(token);
            return result as int?;
        }
    }
}
