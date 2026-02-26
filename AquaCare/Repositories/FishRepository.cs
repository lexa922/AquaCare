using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using AquaCareClasses;
using DbFish = AquaCareClasses.Fish;
using System.Data;

namespace AquaCare.Repositories
{
    public class FishRepository
    {
        private readonly string _connectionString;

        public FishRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<List<DbFish>> GetAllByAquariumIdAsync(int aquariumId, CancellationToken token = default)
        {
            var list = new List<DbFish>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
                SELECT 
                    f.FishId, f.FishName, f.Gender, f.IsAlive, f.AquariumId, f.SpeciesId,
                    s.SpeciesName, s.SpeciesDescription, s.BioLoadValue
                FROM Fish f
                JOIN FishSpecies s ON f.SpeciesId = s.FishSpecieId
                WHERE f.AquariumId = @AquariumId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@AquariumId", aquariumId));

            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                list.Add(MapFish(reader));
            }
            return list;
        }
        public async Task DeleteByIdAsync(int id, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);
            try
            {
 
                string sql = @"
            DELETE FROM Images 
            WHERE DiseaseLogId IN (SELECT DiseaseLogId FROM DiseaseLogs WHERE FishId = @Id);
                
            DELETE FROM DiseaseLogs WHERE FishId=@Id;

            UPDATE BreedingLogs SET MaleFishId = NULL WHERE MaleFishId = @Id;
            UPDATE BreedingLogs SET FemaleFishId = NULL WHERE FemaleFishId = @Id;

            DELETE FROM Fish WHERE FishId = @Id;
            ";
                await using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.Add(new SqlParameter("@Id", id));

                await command.ExecuteNonQueryAsync(token);

                await transaction.CommitAsync(token);
            }
            catch(Exception)
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async Task UpdateFishAsync(DbFish fish, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        UPDATE Fish 
        SET FishName = @Name, 
            Gender = @Gender 
        WHERE FishId = @Id";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", fish.FishId);
            command.Parameters.AddWithValue("@Name", fish.FishName);
            command.Parameters.AddWithValue("@Gender", fish.Gender);

            await command.ExecuteNonQueryAsync(token);
        }
        public async Task AddAsync(DbFish fish, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
                INSERT INTO Fish (FishName, Gender, IsAlive, SpeciesId, AquariumId)
                VALUES (@Name, @Gender, @IsAlive, @SpecId, @AqId)";

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(new SqlParameter("@Name", fish.FishName));
            command.Parameters.Add(new SqlParameter("@Gender", fish.Gender));
            command.Parameters.Add(new SqlParameter("@IsAlive", fish.IsAlive));
            command.Parameters.Add(new SqlParameter("@SpecId", fish.SpeciesId));
            command.Parameters.Add(new SqlParameter("@AqId", fish.AquariumId));

            await command.ExecuteNonQueryAsync(token);
        }
        public async Task<List<CompatibilityRule>> CheckCompatibilityAsync(int newFishId, List<int> existingFishIds)
        {
            var rules = new List<CompatibilityRule>();
            if (existingFishIds == null || existingFishIds.Count == 0) return rules;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string idsString = string.Join(",", existingFishIds);

            string sql = $@"
        SELECT CompatibilityRuleId, Species1Id, Species2Id, CompatabilityLevel, Notes 
        FROM CompatabilityRules 
        WHERE 
            (Species1Id = @NewId AND Species2Id IN ({idsString}))
            OR 
            (Species2Id = @NewId AND Species1Id IN ({idsString}))";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@NewId", newFishId));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rules.Add(new CompatibilityRule
                {
                    CompatibilityRuleId = reader.GetInt32(reader.GetOrdinal("CompatibilityRuleId")),
                    Species1Id = reader.GetInt32(reader.GetOrdinal("Species1Id")),
                    Species2Id = reader.GetInt32(reader.GetOrdinal("Species2Id")),
                    CompatabilityLevel = reader.GetInt32(reader.GetOrdinal("CompatabilityLevel")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString(reader.GetOrdinal("Notes"))
                });
            }
            return rules;
        }

        private DbFish MapFish(SqlDataReader reader)
        {
            var fish = new DbFish
            {
                FishId = reader.GetInt32(reader.GetOrdinal("FishId")),
                FishName = reader.GetString(reader.GetOrdinal("FishName")),
                Gender = reader.GetString(reader.GetOrdinal("Gender")),
                IsAlive = reader.GetBoolean(reader.GetOrdinal("IsAlive")),
                AquariumId = reader.GetInt32(reader.GetOrdinal("AquariumId")),
                SpeciesId = reader.GetInt32(reader.GetOrdinal("SpeciesId")),

                Species = new FishSpecie()
            };
            if (!reader.IsDBNull(reader.GetOrdinal("SpeciesName")))
            {
                fish.Species.SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName"));
            }
            if (!reader.IsDBNull(reader.GetOrdinal("SpeciesDescription")))
            {
                fish.Species.SpeciesDescription = reader.GetString(reader.GetOrdinal("SpeciesDescription"));
            }
            if (!reader.IsDBNull(reader.GetOrdinal("BioLoadValue")))
            {
                fish.Species.BioLoadValue = reader.GetDouble(reader.GetOrdinal("BioLoadValue"));
            }

            return fish;
        }
        
        public async Task<FishSpecie?> GetSpeciesByIdAsync(int id, CancellationToken token = default)
        {
            FishSpecie? specie = null;

            await using var connection = new SqlConnection(UserSession.CurrentConnectionString);
            await connection.OpenAsync(token);
            string sql = @"
        SELECT 
            FishSpecieId, 
            SpeciesName, 
            SpeciesDescription, 
            MinTankSize, 
            BioLoadValue, 
            MinGroupSize 
        FROM FishSpecies 
        WHERE FishSpecieId = @Id;

        SELECT ImagePath 
        FROM Images 
        WHERE FishSpeciesId = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            await using var reader = await command.ExecuteReaderAsync(token);

            if (await reader.ReadAsync(token))
            {
                specie = new FishSpecie
                {
                    FishSpecieId = reader.GetInt32(reader.GetOrdinal("FishSpecieId")),
                    SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName")),
                    MinTankSize = reader.GetInt32(reader.GetOrdinal("MinTankSize")),
                    BioLoadValue = reader.GetDouble(reader.GetOrdinal("BioLoadValue")),
                    MinGroupSize = reader.GetInt32(reader.GetOrdinal("MinGroupSize"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("SpeciesDescription")))
                    specie.SpeciesDescription = reader.GetString(reader.GetOrdinal("SpeciesDescription"));

                specie.Images = new List<Image>();
            }

            if (specie == null) return null;

            if (await reader.NextResultAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    string dbPath = reader.GetString(reader.GetOrdinal("ImagePath"));
                    specie.Images.Add(new Image
                    {
                        ImagePath = dbPath
                    });
                }
            }
            return specie;
        }

        public async Task AddSpeciesAsync(FishSpecie species, List<string> imagePaths, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(UserSession.CurrentConnectionString);
            await connection.OpenAsync(token);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string sqlFish = @"
            INSERT INTO FishSpecies 
            (SpeciesName, SpeciesDescription, MinTankSize, BioLoadValue, MinGroupSize) 
            VALUES 
            (@Name, @Desc, @Tank, @Load, @GrSize);
            
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newFishId;

                await using (var command = new SqlCommand(sqlFish, connection, transaction))
                {
                    command.Parameters.Add(new SqlParameter("@Name", species.SpeciesName));
                    command.Parameters.Add(new SqlParameter("@Desc", (object)species.SpeciesDescription ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Tank", species.MinTankSize));
                    command.Parameters.Add(new SqlParameter("@Load", species.BioLoadValue));
                    command.Parameters.Add(new SqlParameter("@GrSize", species.MinGroupSize));
                    var result = await command.ExecuteScalarAsync(token);
                    newFishId = (int)result;
                }
                if (imagePaths != null && imagePaths.Count > 0)
                {
                    string sqlImage = "INSERT INTO Images (FishSpeciesId, ImagePath) VALUES (@FId, @Path)";

                    foreach (var path in imagePaths)
                    {
                        await using var imgCommand = new SqlCommand(sqlImage, connection, transaction);
                        imgCommand.Parameters.Add(new SqlParameter("@FId", newFishId));
                        imgCommand.Parameters.Add(new SqlParameter("@Path", path));

                        await imgCommand.ExecuteNonQueryAsync(token);
                    }
                }
                await transaction.CommitAsync(token);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async Task<List<FishSpecie>> GetAllSpeciesAsync(CancellationToken token = default)
        {
            var list = new List<FishSpecie>();
            await using var connection = new SqlConnection(UserSession.CurrentConnectionString);
            await connection.OpenAsync(token);

            string sql = @"
        SELECT 
            s.FishSpecieId, 
            s.SpeciesName, 
            s.SpeciesDescription, 
            s.MinTankSize,
            s.BioLoadValue,
            s.MinGroupSize,
            (SELECT TOP 1 i.ImagePath 
             FROM Images i 
             WHERE i.FishSpeciesId = s.FishSpecieId) AS ImagePath
        FROM FishSpecies s";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                var specie = new FishSpecie
                {
                    FishSpecieId = reader.GetInt32(reader.GetOrdinal("FishSpecieId")),
                    SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName")),

                    MinTankSize = reader.GetInt32(reader.GetOrdinal("MinTankSize")),
                    BioLoadValue = reader.GetDouble(reader.GetOrdinal("BioLoadValue")),
                    MinGroupSize = reader.GetInt32(reader.GetOrdinal("MinGroupSize"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("SpeciesDescription")))
                {
                    specie.SpeciesDescription = reader.GetString(reader.GetOrdinal("SpeciesDescription"));
                }

                if (!reader.IsDBNull(reader.GetOrdinal("ImagePath")))
                {
                    specie.ImagePath = reader.GetString(reader.GetOrdinal("ImagePath"));
                }

                list.Add(specie);
            }
            return list;
        }
    }
}
