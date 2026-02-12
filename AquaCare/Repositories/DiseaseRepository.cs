using AquaCareClasses;
using System;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCare.Repositories
{
    public class DiseaseRepository
    {
        private readonly string _connectionString;

        public DiseaseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<List<Disease>> GetAllDiseasesAsync(CancellationToken token = default)
        {
            var list = new List<Disease>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "SELECT DiseaseId, Name, Treatment FROM Diseases ORDER BY Name";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                list.Add(new Disease
                {
                    DiseaseId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Treatment = reader.IsDBNull(2) ? "" : reader.GetString(2)
                });
            }
            return list;
        }
        public async Task AddLogAsync(DiseaseLog log, List<string> imagePaths, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string sqlLog = @"
                    INSERT INTO DiseaseLogs (FishId, PlantId, DiseaseId, TreatmentNotes, StartDate, EndDate)
                    OUTPUT INSERTED.DiseaseLogId
                    VALUES (@FishId, @PlantId, @DiseaseId, @Notes, @Start, @End)";

                int newLogId;

                await using (var command = new SqlCommand(sqlLog, connection, transaction))
                {
                    command.Parameters.AddWithValue("@FishId", (object)log.FishId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PlantId", (object)log.PlantId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DiseaseId", log.DiseaseId);
                    command.Parameters.AddWithValue("@Notes", (object)log.TreatmentNotes ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Start", log.StartDate.ToDateTime(TimeOnly.MinValue));
                    command.Parameters.AddWithValue("@End", log.EndDate.HasValue ? log.EndDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);

                    newLogId = (int)await command.ExecuteScalarAsync(token);
                }

                if (imagePaths != null && imagePaths.Count > 0)
                {
                    string sqlImg = "INSERT INTO Images (DiseaseLogId, ImagePath) VALUES (@LogId, @Path)";

                    foreach (var path in imagePaths)
                    {
                        await using var cmdImg = new SqlCommand(sqlImg, connection, transaction);
                        cmdImg.Parameters.AddWithValue("@LogId", newLogId);
                        cmdImg.Parameters.AddWithValue("@Path", path);
                        await cmdImg.ExecuteNonQueryAsync(token);
                    }
                }
                await transaction.CommitAsync(token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async Task<List<DiseaseLog>> GetActiveLogsByAquariumIdAsync(int aquariumId, CancellationToken token = default)
        {
            var logsDict = new Dictionary<int, DiseaseLog>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
                SELECT 
                    dl.DiseaseLogId, dl.FishId, dl.PlantId, dl.TreatmentNotes, dl.StartDate,
                    d.Name AS DiseaseName,

                    COALESCE(f.FishName, p_species.SpeciesName, 'Невідомий пацієнт') AS PatientName,

                    i.ImageId, i.ImagePath
                FROM DiseaseLogs dl
                JOIN Diseases d ON dl.DiseaseId = d.DiseaseId
                LEFT JOIN Fish f ON dl.FishId = f.FishId
                LEFT JOIN Plants p ON dl.PlantId = p.PlantId
                LEFT JOIN PlantSpecies p_species ON p.SpeciesId = p_species.PlantSpecieId
                LEFT JOIN Images i ON dl.DiseaseLogId = i.DiseaseLogId
                WHERE 
                    (f.AquariumId = @AqId OR p.AquariumId = @AqId)
                    AND dl.EndDate IS NULL
                ORDER BY dl.StartDate DESC";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AqId", aquariumId);
            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                int logId = reader.GetInt32(reader.GetOrdinal("DiseaseLogId"));

                if (!logsDict.TryGetValue(logId, out var log))
                {
                    log = new DiseaseLog
                    {
                        DiseaseLogId = logId,
                        FishId = reader.IsDBNull(reader.GetOrdinal("FishId")) ? null : reader.GetInt32(reader.GetOrdinal("FishId")),
                        PlantId = reader.IsDBNull(reader.GetOrdinal("PlantId")) ? null : reader.GetInt32(reader.GetOrdinal("PlantId")),
                        TreatmentNotes = reader.IsDBNull(reader.GetOrdinal("TreatmentNotes")) ? "" : reader.GetString(reader.GetOrdinal("TreatmentNotes")),
                        StartDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("StartDate"))),
                        DiseaseName = reader.GetString(reader.GetOrdinal("DiseaseName")),
                        PatientName = reader.GetString(reader.GetOrdinal("PatientName"))
                    };
                    logsDict.Add(logId, log);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("ImageId")))
                {
                    log.Images.Add(new Image
                    {
                        ImageId = reader.GetInt32(reader.GetOrdinal("ImageId")),
                        ImagePath = reader.GetString(reader.GetOrdinal("ImagePath"))
                    });
                }
            }
            return new List<DiseaseLog>(logsDict.Values);
        }
        public async Task<List<Symptom>> GetAllSymptomsAsync(CancellationToken token = default)
        {
            var list = new List<Symptom>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "SELECT SymptomId, Name FROM Symptoms ORDER BY Name";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                list.Add(new Symptom
                {
                    SymptomId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            return list;
        }
        public async Task<List<DiagnosisResult>> DiagnoseAsync(List<int> selectedSymptomIds, CancellationToken token = default)
        {
            var results = new List<DiagnosisResult>();
            if (selectedSymptomIds == null || selectedSymptomIds.Count == 0) return results;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string idsString = string.Join(",", selectedSymptomIds);
            string sql = $@"
        SELECT 
            d.DiseaseId, d.Name, d.Treatment,
            COUNT(ds.SymptomsSymptomId) AS Matches,
            (SELECT COUNT(*) FROM DiseaseSymptom WHERE DiseasesDiseaseId = d.DiseaseId) AS TotalSymptoms
        FROM Diseases d
        JOIN DiseaseSymptom ds ON d.DiseaseId = ds.DiseasesDiseaseId
        WHERE ds.SymptomsSymptomId IN ({idsString})
        GROUP BY d.DiseaseId, d.Name, d.Treatment
        ORDER BY Matches DESC, TotalSymptoms ASC";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                results.Add(new DiagnosisResult
                {
                    Disease = new Disease
                    {
                        DiseaseId = reader.GetInt32(reader.GetOrdinal("DiseaseId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Treatment = reader.IsDBNull(reader.GetOrdinal("Treatment")) ? "" : reader.GetString(reader.GetOrdinal("Treatment"))
                    },
                    MatchCount = reader.GetInt32(reader.GetOrdinal("Matches")),
                    TotalSymptoms = reader.GetInt32(reader.GetOrdinal("TotalSymptoms"))
                });
            }
            return results;
        }
        public async Task<List<PatientLookupDto>> GetPotentialPatientsAsync(int aquariumId, bool isFish, CancellationToken token = default)
        {
            var list = new List<PatientLookupDto>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql;

            if (isFish)
            {
                sql = "SELECT FishId, FishName FROM Fish WHERE AquariumId = @AqId AND IsAlive = 1";
            }
            else
            {
                sql = @"
            SELECT MAX(p.PlantId) as PlantId, s.SpeciesName 
            FROM Plants p 
            JOIN PlantSpecies s ON p.SpeciesId = s.PlantSpecieId 
            WHERE p.AquariumId = @AqId
            GROUP BY s.SpeciesName";
            }

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AqId", aquariumId);

            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                list.Add(new PatientLookupDto
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return list;
        }
        public async Task AddImageToLogAsync(int diseaseLogId, string imagePath)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            string sql = @"
        INSERT INTO Images (DiseaseLogId, ImagePath) 
        VALUES (@LogId, @Path)";

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@LogId", diseaseLogId);
            command.Parameters.AddWithValue("@Path", imagePath);

            await command.ExecuteNonQueryAsync();
        }
        public async Task UpdateLogAsync(DiseaseLog log, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        UPDATE DiseaseLogs 
        SET 
            DiseaseId = @DiseaseId,
            TreatmentNotes = @Notes,
            StartDate = @Start
        WHERE DiseaseLogId = @LogId";

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@DiseaseId", log.DiseaseId);
            command.Parameters.AddWithValue("@Notes", (object)log.TreatmentNotes ?? DBNull.Value);
            command.Parameters.AddWithValue("@Start", log.StartDate.ToDateTime(TimeOnly.MinValue));
            command.Parameters.AddWithValue("@LogId", log.DiseaseLogId);

            await command.ExecuteNonQueryAsync(token);
        }
        public async Task<int> AddSymptomAsync(string name, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string checkSql = "SELECT SymptomId FROM Symptoms WHERE Name = @Name";
            await using (var checkCmd = new SqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@Name", name);
                var existing = await checkCmd.ExecuteScalarAsync(token);
                if (existing != null) return (int)existing;
            }

            string sql = "INSERT INTO Symptoms (Name) OUTPUT INSERTED.SymptomId VALUES (@Name)";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            return (int)await command.ExecuteScalarAsync(token);
        }
        public async Task CreateDiseaseWithSymptomsAsync(Disease disease, List<int> symptomIds, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string sqlDisease = @"
            INSERT INTO Diseases (Name, Treatment) 
            OUTPUT INSERTED.DiseaseId 
            VALUES (@Name, @Treatment)";

                int newDiseaseId;
                await using (var cmdDis = new SqlCommand(sqlDisease, connection, transaction))
                {
                    cmdDis.Parameters.AddWithValue("@Name", disease.Name);
                    cmdDis.Parameters.AddWithValue("@Treatment", disease.Treatment);
                    newDiseaseId = (int)await cmdDis.ExecuteScalarAsync(token);
                }
                if (symptomIds != null && symptomIds.Count > 0)
                {
                    string sqlLink = "INSERT INTO DiseaseSymptom (DiseasesDiseaseId, SymptomsSymptomId) VALUES (@DId, @SId)";

                    foreach (var sId in symptomIds)
                    {
                        await using var cmdLink = new SqlCommand(sqlLink, connection, transaction);
                        cmdLink.Parameters.AddWithValue("@DId", newDiseaseId);
                        cmdLink.Parameters.AddWithValue("@SId", sId);
                        await cmdLink.ExecuteNonQueryAsync(token);
                    }
                }

                await transaction.CommitAsync(token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async Task RegisterDeathAsync(int logId, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string closeLogSql = "UPDATE DiseaseLogs SET EndDate = GETDATE() WHERE DiseaseLogId = @LogId";
                await using (var cmd1 = new SqlCommand(closeLogSql, connection, transaction))
                {
                    cmd1.Parameters.AddWithValue("@LogId", logId);
                    await cmd1.ExecuteNonQueryAsync(token);
                }
                string killFishSql = @"
            UPDATE Fish 
            SET IsAlive = 0 
            WHERE FishId = (SELECT FishId FROM DiseaseLogs WHERE DiseaseLogId = @LogId)";

                await using (var cmd2 = new SqlCommand(killFishSql, connection, transaction))
                {
                    cmd2.Parameters.AddWithValue("@LogId", logId);
                    await cmd2.ExecuteNonQueryAsync(token);
                }

                await transaction.CommitAsync(token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async Task CureAsync(int logId, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "UPDATE DiseaseLogs SET EndDate = GETDATE() WHERE DiseaseLogId = @Id";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", logId);
            await command.ExecuteNonQueryAsync(token);
        }
    }
}
