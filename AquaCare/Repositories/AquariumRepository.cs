using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class AquariumRepository
    {
        private readonly string _connectionString;

        public AquariumRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<int> CreateAsync(Aquarium aquarium, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
            INSERT INTO Aquariums 
            (UserId, Name, Volume, SubstrateId, LightIntensityLevelId, LightOnTime, LightOffTime) 
            VALUES 
            (@UserId, @Name, @Volume, @SubstrateId, @LightIntensityLevelId, @LightOnTime, @LightOffTime);
            SELECT CAST(SCOPE_IDENTITY() as int);";

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(new SqlParameter("@UserId", aquarium.UserId));
            command.Parameters.Add(new SqlParameter("@Name", aquarium.Name));
            command.Parameters.Add(new SqlParameter("@Volume", aquarium.Volume));
            command.Parameters.Add(new SqlParameter("@SubstrateId", aquarium.SubstrateId));
            command.Parameters.Add(new SqlParameter("@LightIntensityLevelId", aquarium.LightIntensityLevelId));

            command.Parameters.Add(new SqlParameter("@LightOnTime", aquarium.LightOnTime.ToTimeSpan()));
            command.Parameters.Add(new SqlParameter("@LightOffTime", aquarium.LightOffTime.ToTimeSpan()));

            var result = await command.ExecuteScalarAsync(token);
            return (int)result;
        }
        public async Task<Aquarium?> GetByIdWithFishAsync(int id, CancellationToken token = default)
        {
            Aquarium aquarium=null;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
            SELECT 
            a.AquariumId, a.UserId, a.Name, a.Volume, a.LightOnTime, a.LightOffTime,
            
            a.SubstrateId, 
            a.LightIntensityLevelId,
            
            s.Name AS SubstrateName,
            s.Color,
            s.Size,
            s.SubstrateTypeId,

            l.Name AS LightLevelName

        FROM Aquariums a
        LEFT JOIN Substrates s ON a.SubstrateId = s.SubstrateId
        LEFT JOIN LightIntensityLevels l ON a.LightIntensityLevelId = l.LightIntensityLevelId
        WHERE a.AquariumId = @Id;

        SELECT 
            f.FishId, f.FishName, f.Gender, f.IsAlive, f.AquariumId, f.SpeciesId,
            fs.SpeciesName
        FROM Fish f
        JOIN FishSpecies fs ON f.SpeciesId = fs.FishSpecieId
        WHERE f.AquariumId = @Id";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            await using var reader = await command.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
            {
                aquarium = MapAquarium(reader);
            }
            if (await reader.NextResultAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    var fish = new Fish
                    {
                        FishId = reader.GetInt32(reader.GetOrdinal("FishId")),
                        FishName = reader.GetString(reader.GetOrdinal("FishName")),
                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                        IsAlive = reader.GetBoolean(reader.GetOrdinal("IsAlive")),
                        AquariumId = id,
                        SpeciesId = reader.GetInt32(reader.GetOrdinal("SpeciesId")),

                        Species = new FishSpecie
                        {
                            FishSpecieId = reader.GetInt32(reader.GetOrdinal("SpeciesId")),
                            SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName"))
                        }
                    };

                    aquarium.Fishes.Add(fish);
                }
            }

            return aquarium;
        }
        public async Task<Aquarium?> GetByIdAsync(int id, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"SELECT 
            a.AquariumId, a.UserId, a.Name, a.Volume, a.LightOnTime, a.LightOffTime,
            
            a.SubstrateId, 
            a.LightIntensityLevelId,
            
            s.SubstrateTypeId,
            s.Name AS SubstrateName,
            s.Color,
            s.Size,

            l.Name AS LightLevelName

        FROM Aquariums a
        LEFT JOIN Substrates s ON a.SubstrateId = s.SubstrateId
        LEFT JOIN LightIntensityLevels l ON a.LightIntensityLevelId = l.LightIntensityLevelId
        WHERE a.AquariumId = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            await using var reader = await command.ExecuteReaderAsync(token);

            if (await reader.ReadAsync(token))
            {
                return MapAquarium(reader);
            }
            return null;
        }
        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string sql = @"
           DELETE FROM Images
                WHERE WaterChangeId IN (SELECT WaterChangeId FROM WaterChanges WHERE AquariumId = @Id);

            DELETE FROM Images 
                WHERE DiseaseLogId IN (
                    SELECT DiseaseLogId FROM DiseaseLogs 
                    WHERE FishId IN (SELECT FishId FROM Fish WHERE AquariumId = @Id)
                        OR PlantId IN (SELECT PlantId FROM Plants WHERE AquariumId = @Id)
                        );

            DELETE FROM Images
                WHERE BreedingLogId IN (SELECT BreedingLogId FROM BreedingLogs WHERE AquariumId = @Id);

            DELETE FROM DiseaseLogs 
                WHERE FishId IN (SELECT FishId FROM Fish WHERE AquariumId = @Id)
                OR PlantId IN (SELECT PlantId FROM Plants WHERE AquariumId = @Id);

            DELETE FROM WaterChanges WHERE AquariumId = @Id;

            DELETE FROM BreedingLogs WHERE AquariumId = @Id;

            UPDATE BreedingLogs 
                SET MaleFishId = NULL 
                WHERE MaleFishId IN (SELECT FishId FROM Fish WHERE AquariumId = @Id);

            UPDATE BreedingLogs 
                SET FemaleFishId = NULL 
                WHERE FemaleFishId IN (SELECT FishId FROM Fish WHERE AquariumId = @Id);


            DELETE FROM Fish WHERE AquariumId = @Id;

            DELETE FROM Plants WHERE AquariumId = @Id;

            DELETE FROM Aquariums WHERE AquariumId = @Id;";

                await using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.Add(new SqlParameter("@Id", id));

                await command.ExecuteNonQueryAsync(token);

                await transaction.CommitAsync(token);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        private Aquarium MapAquarium(SqlDataReader reader)
        {
            var aqua = new Aquarium
            {
                AquariumId = reader.GetInt32(reader.GetOrdinal("AquariumId")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Volume = reader.GetInt32(reader.GetOrdinal("Volume")),

                SubstrateId = reader.IsDBNull(reader.GetOrdinal("SubstrateId")) ? 0 : reader.GetInt32(reader.GetOrdinal("SubstrateId")),
                LightIntensityLevelId = reader.IsDBNull(reader.GetOrdinal("LightIntensityLevelId")) ? 0 : reader.GetInt32(reader.GetOrdinal("LightIntensityLevelId")),

                Fishes = new List<Fish>(),
                Plants = new List<Plant>(),
                WaterChanges = new List<WaterChange>()
            };

            if (!reader.IsDBNull(reader.GetOrdinal("LightOnTime")))
                aqua.LightOnTime = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("LightOnTime")));

            if (!reader.IsDBNull(reader.GetOrdinal("LightOffTime")))
                aqua.LightOffTime = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("LightOffTime")));

            if (!reader.IsDBNull(reader.GetOrdinal("SubstrateTypeId")))
            {
                aqua.Substrate = new Substrate
                {
                    SubstrateId = aqua.SubstrateId,

                    SubstrateTypeId = reader.GetInt32(reader.GetOrdinal("SubstrateTypeId")),
                    Name = reader.GetString(reader.GetOrdinal("SubstrateName")),
                    Color = reader.IsDBNull(reader.GetOrdinal("Color")) ? null : reader.GetString(reader.GetOrdinal("Color")),
                    Size = reader.IsDBNull(reader.GetOrdinal("Size")) ? null : reader.GetString(reader.GetOrdinal("Size"))
                };
            }

            if (!reader.IsDBNull(reader.GetOrdinal("LightLevelName")))
            {
                aqua.LightIntensityLevel = new LightIntensityLevel
                {
                    LightIntensityLevelId = aqua.LightIntensityLevelId,
                    Name = reader.GetString(reader.GetOrdinal("LightLevelName"))
                };
            }

            return aqua;
        }

        public async Task<List<Aquarium>> GetAquariumsByUserIdAsync(int userId, CancellationToken token = default)
        {
            var list = new List<Aquarium>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
            SELECT 
            a.AquariumId, a.UserId, a.Name, a.Volume, a.LightOnTime, a.LightOffTime,
            
            a.SubstrateId, 
            a.LightIntensityLevelId,
            
            s.SubstrateTypeId,
            s.Name AS SubstrateName,
            s.Color,
            s.Size,

            l.Name AS LightLevelName

        FROM Aquariums a
        LEFT JOIN Substrates s ON a.SubstrateId = s.SubstrateId
        LEFT JOIN LightIntensityLevels l ON a.LightIntensityLevelId = l.LightIntensityLevelId
            WHERE a.UserId = @UserId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@UserId", userId));

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                var aquarium = MapAquarium(reader);
                var onTimeSpan = reader.GetTimeSpan(reader.GetOrdinal("LightOnTime"));
                aquarium.LightOnTime = TimeOnly.FromTimeSpan(onTimeSpan);

                var offTimeSpan = reader.GetTimeSpan(reader.GetOrdinal("LightOffTime"));
                aquarium.LightOffTime = TimeOnly.FromTimeSpan(offTimeSpan);
                if (!reader.IsDBNull(reader.GetOrdinal("SubstrateName")))
                {
                    aquarium.Substrate = new Substrate
                    {
                        SubstrateId = aquarium.SubstrateId,
                        Name = reader.GetString(reader.GetOrdinal("SubstrateName"))
                    };
                }
                if (!reader.IsDBNull(reader.GetOrdinal("LightLevelName")))
                {
                    aquarium.LightIntensityLevel = new LightIntensityLevel
                    {
                        LightIntensityLevelId = aquarium.LightIntensityLevelId,
                        Name = reader.GetString(reader.GetOrdinal("LightLevelName"))
                    };
                }

                list.Add(aquarium);
            }
            return list;
        }
        public async Task<List<Aquarium>> GetAquariumsWithDetailsAsync(int userId, CancellationToken token = default)
        {
            var aquariumDict = new Dictionary<int, Aquarium>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        SELECT 
            a.AquariumId, a.UserId, a.Name, a.Volume, a.LightOnTime, a.LightOffTime,
            a.SubstrateId, s.Name AS SubstrateName,
            a.LightIntensityLevelId, l.Name AS LightLevelName
        FROM Aquariums a
        LEFT JOIN Substrates s ON a.SubstrateId = s.SubstrateId
        LEFT JOIN LightIntensityLevels l ON a.LightIntensityLevelId = l.LightIntensityLevelId
        WHERE a.UserId = @UserId;

        SELECT 
            f.FishId, f.FishName, f.Gender, f.IsAlive, f.AquariumId, f.SpeciesId,
            fs.SpeciesName, fs.BioLoadValue
        FROM Fish f
        JOIN Aquariums a ON f.AquariumId = a.AquariumId
        JOIN FishSpecies fs ON f.SpeciesId = fs.FishSpecieId
        WHERE a.UserId = @UserId;

        SELECT 
            p.PlantId, p.Quantity, p.AquariumId, p.SpeciesId,
            ps.SpeciesName
        FROM Plants p
        JOIN Aquariums a ON p.AquariumId = a.AquariumId
        JOIN PlantSpecies ps ON p.SpeciesId = ps.PlantSpecieId
        WHERE a.UserId = @UserId;

        SELECT wc.WaterChangeId, wc.Volume, wc.ChangeDate, wc.AquariumId
        FROM WaterChanges wc
        JOIN Aquariums a ON wc.AquariumId = a.AquariumId
        WHERE a.UserId = @UserId;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@UserId", userId));

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                var aq = new Aquarium
                {
                    AquariumId = reader.GetInt32(reader.GetOrdinal("AquariumId")),
                    UserId = userId,
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Volume = reader.GetInt32(reader.GetOrdinal("Volume")),
                    SubstrateId = reader.IsDBNull(reader.GetOrdinal("SubstrateId")) ? 0 : reader.GetInt32(reader.GetOrdinal("SubstrateId")),
                    LightIntensityLevelId = reader.IsDBNull(reader.GetOrdinal("LightIntensityLevelId")) ? 0 : reader.GetInt32(reader.GetOrdinal("LightIntensityLevelId")),

                    Fishes = new List<Fish>(),
                    Plants = new List<Plant>(),
                    WaterChanges = new List<WaterChange>()
                };

                if (!reader.IsDBNull(reader.GetOrdinal("LightOnTime")))
                    aq.LightOnTime = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("LightOnTime")));
                if (!reader.IsDBNull(reader.GetOrdinal("LightOffTime")))
                    aq.LightOffTime = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("LightOffTime")));

                if (!reader.IsDBNull(reader.GetOrdinal("SubstrateName")))
                {
                    aq.Substrate = new Substrate
                    {
                        SubstrateId = aq.SubstrateId,
                        Name = reader.GetString(reader.GetOrdinal("SubstrateName"))
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("LightLevelName")))
                {
                    aq.LightIntensityLevel = new LightIntensityLevel
                    {
                        LightIntensityLevelId = aq.LightIntensityLevelId,
                        Name = reader.GetString(reader.GetOrdinal("LightLevelName"))
                    };
                }

                aquariumDict.Add(aq.AquariumId, aq);
            }

            if (await reader.NextResultAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    int aqId = reader.GetInt32(reader.GetOrdinal("AquariumId"));

                    if (aquariumDict.TryGetValue(aqId, out var parentAquarium))
                    {
                        var fish = new Fish
                        {
                            FishId = reader.GetInt32(reader.GetOrdinal("FishId")),
                            FishName = reader.GetString(reader.GetOrdinal("FishName")),
                            Gender = reader.GetString(reader.GetOrdinal("Gender")),
                            IsAlive = reader.GetBoolean(reader.GetOrdinal("IsAlive")),
                            AquariumId = aqId,
                            SpeciesId = reader.GetInt32(reader.GetOrdinal("SpeciesId")),

                            Species = new FishSpecie
                            {
                                FishSpecieId = reader.GetInt32(reader.GetOrdinal("SpeciesId")),
                                SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName")),
                                BioLoadValue=reader.GetDouble(reader.GetOrdinal("BioLoadValue"))
                            }
                        };
                        parentAquarium.Fishes.Add(fish);
                    }
                }
            }

            if (await reader.NextResultAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    int aqId = reader.GetInt32(reader.GetOrdinal("AquariumId"));
                    if (aquariumDict.TryGetValue(aqId, out var parentAquarium))
                    {
                        var plant = new Plant
                        {
                            PlantId = reader.GetInt32(reader.GetOrdinal("PlantId")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                            AquariumId = aqId,
                            SpeciesId = reader.GetInt32(reader.GetOrdinal("SpeciesId")),

                            Plants = new PlantSpecie
                            {
                                PlantSpecieId = reader.GetInt32(reader.GetOrdinal("SpeciesId")),
                                SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName"))
                            }
                        };
                        parentAquarium.Plants.Add(plant);
                    }
                }
            }

            if (await reader.NextResultAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    int aqId = reader.GetInt32(reader.GetOrdinal("AquariumId"));
                    if (aquariumDict.TryGetValue(aqId, out var parentAquarium))
                    {
                        var wc = new WaterChange
                        {
                            WaterChangeId = reader.GetInt32(reader.GetOrdinal("WaterChangeId")),
                            Volume = reader.GetInt32(reader.GetOrdinal("Volume")),
                            ChangeDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("ChangeDate"))),
                            AquariumId = aqId
                        };
                        parentAquarium.WaterChanges.Add(wc);
                    }
                }
            }

            return aquariumDict.Values.ToList();
        }
    }
}
