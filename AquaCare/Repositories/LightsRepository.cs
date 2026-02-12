using AquaCareClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace AquaCare.Repositories
{
    public class LightsRepository
    {
        private string _connectionString;
        public LightsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<LightIntensityLevel>> GetLightIntensityLevelsAsync(CancellationToken token = default)
        {
            var list = new List<LightIntensityLevel>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "SELECT LightIntensityLevelId, Name FROM LightIntensityLevels";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                list.Add(new LightIntensityLevel
                {
                    LightIntensityLevelId = reader.GetInt32(reader.GetOrdinal("LightIntensityLevelId")),
                    Name = reader.GetString(reader.GetOrdinal("Name"))
                });
            }
            return list;
        }
    }
}
