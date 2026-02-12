using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using AquaCareClasses;
using Microsoft.IdentityModel.Tokens;

namespace AquaCare.Repositories
{
    public class WaterChangeRepository
    {
        private readonly string _connectionString;
        public WaterChangeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<WaterChange>> GetAllByAquariumIdAsync(int id, CancellationToken token = default)
        {
            var logsDict = new Dictionary<int, WaterChange>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        SELECT wc.WaterChangeId, wc.AquariumId, wc.ChangeDate, wc.Volume,
               i.ImageId, i.ImagePath
        FROM WaterChanges wc
        LEFT JOIN Images i ON wc.WaterChangeId = i.WaterChangeId
        WHERE wc.AquariumId = @AqId
        ORDER BY wc.ChangeDate DESC";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@AqId", id));

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                int wcId = reader.GetInt32(reader.GetOrdinal("WaterChangeId"));

                if (!logsDict.TryGetValue(wcId, out var waterChange))
                {
                    waterChange = MapWaterChange(reader);
                    logsDict.Add(wcId, waterChange);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("ImageId")))
                {
                    var img = new Image
                    {
                        ImageId = reader.GetInt32(reader.GetOrdinal("ImageId")),
                        ImagePath = reader.GetString(reader.GetOrdinal("ImagePath")),
                    };
                    waterChange.Images.Add(img);
                }
            }

            return logsDict.Values.ToList();
        }

        public async Task AddAsync(WaterChange waterChange, List<string>imagePaths, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);

            try
            {
                string sqlLog = @"
            INSERT INTO WaterChanges (AquariumId, ChangeDate, Volume) 
            OUTPUT INSERTED.WaterChangeId -- Одразу отримуємо ID
            VALUES (@AqId, @Date, @Vol);";

                int newId;

                await using (var cmd = new SqlCommand(sqlLog, connection, transaction))
                {
                    cmd.Parameters.Add(new SqlParameter("@AqId", waterChange.AquariumId));
                    cmd.Parameters.Add(new SqlParameter("@Date", waterChange.ChangeDate.ToDateTime(TimeOnly.MinValue)));
                    cmd.Parameters.Add(new SqlParameter("@Vol", waterChange.Volume));

                    var result = await cmd.ExecuteScalarAsync(token);
                    newId = Convert.ToInt32(result);
                }

                if (imagePaths != null && imagePaths.Count > 0)
                {
                    string sqlImg = "INSERT INTO Images (WaterChangeId, ImagePath) VALUES (@WcId, @Path)";

                    foreach (var path in imagePaths)
                    {
                        await using var cmdImg = new SqlCommand(sqlImg, connection, transaction);
                        cmdImg.Parameters.Add(new SqlParameter("@WcId", newId));
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

        private WaterChange MapWaterChange(SqlDataReader reader) 
        {
            return new WaterChange
            {
                WaterChangeId = reader.GetInt32(reader.GetOrdinal("WaterChangeId")),
                AquariumId = reader.GetInt32(reader.GetOrdinal("AquariumId")),
                ChangeDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("ChangeDate"))),
                Volume = reader.GetInt32(reader.GetOrdinal("Volume")),

                Images = new List<Image>()
            };
        }
    }
}
