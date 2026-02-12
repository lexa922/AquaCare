using AquaCare;
using AquaCareClasses;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xceed.Wpf.AvalonDock.Themes;

namespace AquaCareClasses
{
    public class PlantRepository
    {
        private readonly string _connectionString;

        public PlantRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<List<PlantSpecie>> GetCompatiblePlantsAsync(int currentSubstrateTypeId, int currentLightId, CancellationToken token = default)
        {
            var plantsDict = new Dictionary<int, PlantSpecie>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        SELECT 
        p.PlantSpecieId, p.SpeciesName, p.SpeciesDescription, 
        p.LightDurationHrs,
        p.LightIntensityLevelId,
        
        l.Name AS LightName,
        
        i.ImageId, i.ImagePath,
        
        st.TypeName AS SubstrateName,
        st.SubstrateTypeId
        
    FROM PlantSpecies p
    LEFT JOIN LightIntensityLevels l ON p.LightIntensityLevelId = l.LightIntensityLevelId
    LEFT JOIN Images i ON p.PlantSpecieId = i.PlantSpeciesId
    
    LEFT JOIN PlantSpecieSubstrateType pst_data ON p.PlantSpecieId = pst_data.PlantSpeciesPlantSpecieId
    LEFT JOIN SubstrateTypes st ON pst_data.SuitableSubstrateTypesSubstrateTypeId = st.SubstrateTypeId

    WHERE 
    (
        (
            EXISTS (
                SELECT 1 FROM PlantSpecieSubstrateType pst 
                WHERE pst.PlantSpeciesPlantSpecieId = p.PlantSpecieId 
                AND pst.SuitableSubstrateTypesSubstrateTypeId = @SubstrateId
            )
            OR 
            NOT EXISTS (
                SELECT 1 FROM PlantSpecieSubstrateType pst 
                WHERE pst.PlantSpeciesPlantSpecieId = p.PlantSpecieId
            )
            OR
            EXISTS(
                SELECT 1 FROM PlantSpecieSubstrateType pst
                JOIN SubstrateTypes st ON pst.SuitableSubstrateTypesSubstrateTypeId = st.SubstrateTypeId
                WHERE pst.PlantSpeciesPlantSpecieId = p.PlantSpecieId
                AND st.TypeName = N'Не потрібен'
            )
        )
    )
    AND 
    p.LightIntensityLevelId <= @LightId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@SubstrateId", currentSubstrateTypeId));
            command.Parameters.Add(new SqlParameter("@LightId", currentLightId));

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                int plantId = reader.GetInt32(reader.GetOrdinal("PlantSpecieId"));

                if (!plantsDict.TryGetValue(plantId, out var plant))
                {
                    plant = new PlantSpecie
                    {
                        PlantSpecieId = plantId,
                        SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName")),

                        SpeciesDescription = reader.IsDBNull(reader.GetOrdinal("SpeciesDescription")) ? "" : reader.GetString(reader.GetOrdinal("SpeciesDescription")),

                        LightDurationHrs = reader.IsDBNull(reader.GetOrdinal("LightDurationHrs")) ? 0 : reader.GetInt32(reader.GetOrdinal("LightDurationHrs")),

                        LightIntensityLevelId = reader.GetInt32(reader.GetOrdinal("LightIntensityLevelId")),
                        LightIntensityLevel = new LightIntensityLevel
                        {
                            LightIntensityLevelId = reader.GetInt32(reader.GetOrdinal("LightIntensityLevelId")),
                            Name = reader.IsDBNull(reader.GetOrdinal("LightName")) ? "Невідомо" : reader.GetString(reader.GetOrdinal("LightName"))
                        },
                        Images = new List<Image>(),
                        SuitableSubstrateTypes = new List<SubstrateType>()
                    };
                    plantsDict.Add(plantId, plant);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("ImageId")))
                {
                    int imgId = reader.GetInt32(reader.GetOrdinal("ImageId"));
                    if (!plant.Images.Any(x => x.ImageId == imgId))
                    {
                        plant.Images.Add(new Image
                        {
                            ImageId = imgId,
                            ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                            PlantSpeciesId = plantId
                        });
                    }
                }

                if (!reader.IsDBNull(reader.GetOrdinal("SubstrateTypeId")))
                {
                    int subId = reader.GetInt32(reader.GetOrdinal("SubstrateTypeId"));
                    if (!plant.SuitableSubstrateTypes.Any(x => x.SubstrateTypeId == subId))
                    {
                        plant.SuitableSubstrateTypes.Add(new SubstrateType
                        {
                            SubstrateTypeId = subId,
                            TypeName = reader.GetString(reader.GetOrdinal("SubstrateName"))
                        });
                    }
                }
            }

            return plantsDict.Values.ToList();
        }
        public async Task<List<Plant>> GetAllByAquariumIdAsync(int aquariumId, CancellationToken token = default)
        {
            var list = new List<Plant>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        SELECT 
            p.PlantId, p.Quantity, p.AquariumId, p.SpeciesId,
            
            ps.PlantSpecieId, ps.SpeciesName, ps.LightIntensityLevelId, ps.SpeciesDescription,
            
            l.Name AS LightLevelName
        FROM Plants p
        JOIN PlantSpecies ps ON p.SpeciesId = ps.PlantSpecieId
        LEFT JOIN LightIntensityLevels l ON ps.LightIntensityLevelId = l.LightIntensityLevelId
        WHERE p.AquariumId = @AquariumId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@AquariumId", aquariumId));

            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                Plant plant = MapPlant(reader);
                if (!reader.IsDBNull(reader.GetOrdinal("LightLevelName")))
                {
                    plant.Plants.LightIntensityLevel = new LightIntensityLevel
                    {
                        Name = reader.GetString(reader.GetOrdinal("LightLevelName"))
                    };
                }
                list.Add(plant);
                plant = null;
            }
            return list;
        }
        public async Task AddAsync(Plant plant, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
                INSERT INTO Plants (AquariumId, SpeciesId, Quantity)
                VALUES (@AqId, @SpecId, @Qty)";

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(new SqlParameter("@AqId", plant.AquariumId));
            command.Parameters.Add(new SqlParameter("@SpecId", plant.SpeciesId));
            command.Parameters.Add(new SqlParameter("@Qty", plant.Quantity));

            await command.ExecuteNonQueryAsync(token);
        }
        private Plant MapPlant(SqlDataReader reader)
        {
            var plant = new Plant
            {
                PlantId = reader.GetInt32(reader.GetOrdinal("PlantId")),
                AquariumId = reader.GetInt32(reader.GetOrdinal("AquariumId")),
                SpeciesId = reader.GetInt32(reader.GetOrdinal("SpeciesId")),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),

                Plants = new PlantSpecie()
            };

            plant.Plants.PlantSpecieId = reader.GetInt32(reader.GetOrdinal("PlantSpecieId"));

            if (!reader.IsDBNull(reader.GetOrdinal("SpeciesName")))
            {
                plant.Plants.SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName"));
            }

            if (!reader.IsDBNull(reader.GetOrdinal("SpeciesDescription")))
            {
                plant.Plants.SpeciesDescription = reader.GetString(reader.GetOrdinal("SpeciesDescription"));
            }

            return plant;
        }
        public async Task<List<PlantSpecie>> GetAllSpeciesAsync(CancellationToken token = default)
        {
            var list = new List<PlantSpecie>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        SELECT 
            ps.PlantSpecieId, 
            ps.SpeciesName, 
            ps.SpeciesDescription,
            ps.LightDurationHrs,
            lil.Name AS LightName,

            (SELECT TOP 1 i.ImagePath 
             FROM Images i 
             WHERE i.PlantSpeciesId = ps.PlantSpecieId) AS ImagePath,

            (SELECT STRING_AGG(st.TypeName, ', ') 
             FROM SubstrateTypes st
             JOIN PlantSpecieSubstrateType link ON st.SubstrateTypeId = link.SuitableSubstrateTypesSubstrateTypeId
             WHERE link.PlantSpeciesPlantSpecieId = ps.PlantSpecieId) AS SubstrateNames

        FROM PlantSpecies ps
        LEFT JOIN LightIntensityLevels lil ON ps.LightIntensityLevelId = lil.LightIntensityLevelId";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                list.Add(MapSpecie(reader));
            }
            return list;
        }

        public async Task<PlantSpecie?> GetSpeciesByIdAsync(int id, CancellationToken token = default)
        {
            PlantSpecie? specie = null;
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        SELECT 
            ps.PlantSpecieId, 
            ps.SpeciesName, 
            ps.SpeciesDescription,
            ps.LightDurationHrs,
            lil.Name AS LightName,
            
            (SELECT TOP 1 i.ImagePath 
             FROM Images i 
             WHERE i.PlantSpeciesId = ps.PlantSpecieId) AS ImagePath,

            (SELECT STRING_AGG(st.TypeName, ', ') 
             FROM SubstrateTypes st
             JOIN PlantSpecieSubstrateType link ON st.SubstrateTypeId = link.SuitableSubstrateTypesSubstrateTypeId
             WHERE link.PlantSpeciesPlantSpecieId = ps.PlantSpecieId) AS SubstrateNames

        FROM PlantSpecies ps
        LEFT JOIN LightIntensityLevels lil ON ps.LightIntensityLevelId = lil.LightIntensityLevelId
        WHERE ps.PlantSpecieId = @Id;

        SELECT ImagePath 
        FROM Images 
        WHERE PlantSpeciesId = @Id;";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            await using var reader = await command.ExecuteReaderAsync(token);

            if (await reader.ReadAsync(token))
            {
                specie = MapSpecie(reader);

                specie.Images = new List<Image>();
            }

            if (specie == null) return null;

            if (await reader.NextResultAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("ImagePath")))
                    {
                        string path = reader.GetString(reader.GetOrdinal("ImagePath"));
                        specie.Images.Add(new Image { ImagePath = path });
                    }
                }
            }

            return specie;
        }

        public async Task AddSpeciesAsync(PlantSpecie plant, List<int> substrateIds, List<string> imagePaths, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string sqlPlant = @"
            INSERT INTO PlantSpecies 
            (SpeciesName, SpeciesDescription, LightDurationHrs, LightIntensityLevelId) 
            VALUES 
            (@Name, @Desc, @Dur, @LightId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newPlantId;

                await using (var cmdPlant = new SqlCommand(sqlPlant, connection, transaction))
                {
                    cmdPlant.Parameters.Add(new SqlParameter("@Name", plant.SpeciesName));
                    cmdPlant.Parameters.Add(new SqlParameter("@Desc", (object)plant.SpeciesDescription ?? DBNull.Value));
                    cmdPlant.Parameters.Add(new SqlParameter("@Dur", plant.LightDurationHrs));
                    cmdPlant.Parameters.Add(new SqlParameter("@LightId", (object)plant.LightIntensityLevelId ?? DBNull.Value));

                    newPlantId = (int)await cmdPlant.ExecuteScalarAsync(token);
                }
                string sqlLinks = @"
            INSERT INTO PlantSpecieSubstrateType 
            (PlantSpeciesPlantSpecieId, SuitableSubstrateTypesSubstrateTypeId) 
            VALUES (@PId, @SId)";

                foreach (int subId in substrateIds)
                {
                    await using var cmdLink = new SqlCommand(sqlLinks, connection, transaction);
                    cmdLink.Parameters.Add(new SqlParameter("@PId", newPlantId));
                    cmdLink.Parameters.Add(new SqlParameter("@SId", subId));
                    await cmdLink.ExecuteNonQueryAsync(token);
                }

                if (imagePaths != null && imagePaths.Any())
                {
                    string sqlImg = "INSERT INTO Images (PlantSpeciesId, ImagePath) VALUES (@PId, @Path)";

                    foreach (var path in imagePaths)
                    {
                        await using var cmdImg = new SqlCommand(sqlImg, connection, transaction);
                        cmdImg.Parameters.Add(new SqlParameter("@PId", newPlantId));
                        cmdImg.Parameters.Add(new SqlParameter("@Path", path));
                        await cmdImg.ExecuteNonQueryAsync(token);
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

        private PlantSpecie MapSpecie(SqlDataReader reader)
        {
            var specie = new PlantSpecie
            {
                PlantSpecieId = reader.GetInt32(reader.GetOrdinal("PlantSpecieId")),
                SpeciesName = reader.GetString(reader.GetOrdinal("SpeciesName")),
                LightDurationHrs = reader.GetInt32(reader.GetOrdinal("LightDurationHrs"))
            };

            if (!reader.IsDBNull(reader.GetOrdinal("SpeciesDescription")))
                specie.SpeciesDescription = reader.GetString(reader.GetOrdinal("SpeciesDescription"));

            if (!reader.IsDBNull(reader.GetOrdinal("LightName")))
                specie.LightLevelName = reader.GetString(reader.GetOrdinal("LightName"));

            if (!reader.IsDBNull(reader.GetOrdinal("SubstrateNames")))
            {
                specie.CompatibleSubstrates = reader.GetString(reader.GetOrdinal("SubstrateNames"));
            }

            if (!reader.IsDBNull(reader.GetOrdinal("ImagePath")))
                specie.ImagePath = reader.GetString(reader.GetOrdinal("ImagePath"));
            
            return specie;
        }
    }
}
