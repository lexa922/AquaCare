using AquaCareClasses;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCare.Repositories
{
    public  class SubstrateRepository
    {
        private readonly string _connectionString;

        public SubstrateRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<SubstrateType>> GetSubstrateTypesAsync(CancellationToken token = default)
        {
            var list = new List<SubstrateType>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = "SELECT SubstrateTypeId, TypeName FROM SubstrateTypes";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                list.Add(new SubstrateType
                {
                    SubstrateTypeId = reader.GetInt32(reader.GetOrdinal("SubstrateTypeId")),
                    TypeName = reader.GetString(reader.GetOrdinal("TypeName"))
                });
            }
            return list;
        }

        public async Task<List<Substrate>> GetSubstratesAsync(CancellationToken token = default)
        {
            var list = new List<Substrate>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);
            string sql = @"
        SELECT 
            s.SubstrateId, 
            s.Name, 
            s.Color, 
            s.Size, 
            s.SubstrateTypeId,
            st.TypeName
        FROM Substrates s
        JOIN SubstrateTypes st ON s.SubstrateTypeId = st.SubstrateTypeId";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                var typeObj = new SubstrateType
                {
                    SubstrateTypeId = reader.GetInt32(reader.GetOrdinal("SubstrateTypeId")),
                    TypeName = reader.GetString(reader.GetOrdinal("TypeName"))
                };

                list.Add(new Substrate
                {
                    SubstrateId = reader.GetInt32(reader.GetOrdinal("SubstrateId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Color = reader.GetString(reader.GetOrdinal("Color")),
                    Size = reader.GetString(reader.GetOrdinal("Size")),

                    SubstrateType = typeObj
                });
            }

            return list;
        }
        public async Task AddSubstrateAsync(Substrate substrate, CancellationToken token = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            string sql = @"
        INSERT INTO Substrates (Name, Color, Size, SubstrateTypeId)
        VALUES (@Name, @Color, @Size, @TypeId)";

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(new SqlParameter("@Name", substrate.Name));
            command.Parameters.Add(new SqlParameter("@Color", substrate.Color ?? (object)DBNull.Value));
            command.Parameters.Add(new SqlParameter("@Size", substrate.Size ?? (object)DBNull.Value));
            command.Parameters.Add(new SqlParameter("@TypeId", substrate.SubstrateTypeId));

            await command.ExecuteNonQueryAsync(token);
        }
    }
}
