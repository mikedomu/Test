using System.Data;
using Kolos.DTOs;
using Kolos.Exceptions;
using Microsoft.Data.SqlClient;

namespace Kolos.Services;

public interface IDbService
{
    public Task<IEnumerable<PoliticianGetDto>> GetPolitycyDetailsAsync();

    public Task<PoliticianGetDto> CreatePolitykAsync(PoliticianCreateDto politykData);
    public Task<PartiaGetDto> CreatePartiaAsync(PartiaCreateDto partiaData);
}

public class DbService(IConfiguration conf) : IDbService
{
    private async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(conf.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        return connection;
    }

    public async Task<IEnumerable<PoliticianGetDto>> GetPolitycyDetailsAsync()
    {
        var politycyDict = new Dictionary<int, PoliticianGetDto>();
        await using var connection = await GetConnectionAsync();
        var sql = """
                  SELECT p.Id,p.Imie,p.Nazwisko,p.Powiedzenie ,party.nazwa,party.Skrot,party.DataZalozenia,p2.od,p2.do
                  from Polityk p
                  left join Przynaleznosc P2 on p.ID = P2.Polityk_ID
                  left join Partia party on p2.Partia_ID=party.ID
                  """;
        await using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);

            if (!politycyDict.TryGetValue(id, out var dto)) //sprawdzam czy istniej juz polityk o zadanym id
            {
                dto = new PoliticianGetDto
                {
                    Id = id,
                    Imie = reader.GetString(1),
                    Nazwisko = reader.GetString(2),
                    Powiedzonko = reader.GetString(3),
                    Przynaleznosc = new List<PrzynaleznoscGetDto>()
                };

                politycyDict[id] = dto;
            }

            if (!await reader.IsDBNullAsync(4)) // Partia.Nazwa
            {
                dto.Przynaleznosc.Add(new PrzynaleznoscGetDto
                {
                    Nazwa = reader.GetString(4),
                    Skrot = reader.IsDBNull(5) ? null : reader.GetString(5),
                    DataZalozenia = reader.GetDateTime(6),
                    Od = reader.GetDateTime(7),
                    Do = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
                });
            }
        }
        return politycyDict.Values;


    }

    public async Task<PoliticianGetDto> CreatePolitykAsync(PoliticianCreateDto politykData)
    {
        await using var connection = await GetConnectionAsync();
        var parties = new List<PrzynaleznoscGetDto>();
        if (politykData.Przynaleznosc is not null && politykData.Przynaleznosc.Count != 0)
        {
            foreach (var party in politykData.Przynaleznosc)
            {
                var groupCheckSql = """
                                    select Id, Nazwa,Skrot,DataZalozenia
                                    from "Partia" 
                                    where Id = @Id;
                                    """;

                await using var groupCheckCommand = new SqlCommand(groupCheckSql, connection);
                groupCheckCommand.Parameters.AddWithValue("@Id", party);
                await using var groupCheckReader = await groupCheckCommand.ExecuteReaderAsync();

                if (!await groupCheckReader.ReadAsync())
                {
                    throw new NotFoundException($"Group with id {party} does not exist");
                }

                parties.Add(new PrzynaleznoscGetDto
                {
                    Nazwa = groupCheckReader.GetString(1),
                    Skrot = groupCheckReader.IsDBNull(2)? null : groupCheckReader.GetString(2),
                    DataZalozenia = groupCheckReader.GetDateTime(3),
                    Od= DateTime.Now,
                    Do= null,
                        
                    
                });
            }
            
        }
        await using var transaction= await connection.BeginTransactionAsync();
        try
        {
            var insertSql = """
                                INSERT INTO Polityk (Imie, Nazwisko, Powiedzenie)
                                OUTPUT INSERTED.Id
                                VALUES (@Imie, @Nazwisko, @Powiedzenie);
                            """;
            await using var insertCmd = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
            insertCmd.Parameters.AddWithValue("@Imie", politykData.Imie);
            insertCmd.Parameters.AddWithValue("@Nazwisko", politykData.Nazwisko);
            insertCmd.Parameters.AddWithValue("@Powiedzenie", politykData.Powiedzonko);

            var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            foreach (var partia in parties)
            {
                var przynaleznoscSql = """
                                           INSERT INTO Przynaleznosc (Polityk_Id, Partia_Id, Od)
                                           VALUES (@PolitykId, 
                                                   (Select Id from Partia where Nazwa=@Nazwa), 
                                                   @Od);
                                       """;

                await using var przCmd = new SqlCommand(przynaleznoscSql, connection, (SqlTransaction)transaction);
                przCmd.Parameters.AddWithValue("@PolitykId", newId);
                przCmd.Parameters.AddWithValue("@Nazwa", partia.Nazwa);
                przCmd.Parameters.AddWithValue("@Od", partia.Od);
                await przCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return new PoliticianGetDto
            {
                Id = newId,
                Imie = politykData.Imie,
                Nazwisko = politykData.Nazwisko,
                Powiedzonko = politykData.Powiedzonko,
                Przynaleznosc = parties
            };
            
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PartiaGetDto> CreatePartiaAsync(PartiaCreateDto partyData)
    {
        await using var connection = await GetConnectionAsync();
        var politycy= new List<PoliticianSimpleDTO>();
        if (partyData.Czlonkowie is not null && partyData.Czlonkowie.Count != 0)
        {
            foreach (var polityk in partyData.Czlonkowie)
            {
                var sql = """
                          Select Imie,Nazwisko,Powiedzenie
                          from Polityk where Id = @Id;
                          """;
                
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", polityk);
                await using var groupCheckReader = await command.ExecuteReaderAsync();
                if (!await groupCheckReader.ReadAsync())
                {
                    throw new NotFoundException($"Polityk with id {polityk} does not exist");
                }
                politycy.Add(new PoliticianSimpleDTO
                {
                    Imie = groupCheckReader.GetString(0),
                    Nazwisko = groupCheckReader.GetString(1),
                    Powiedzonko = groupCheckReader.IsDBNull(2)? null : groupCheckReader.GetString(2),
                });
                
            }
        }
        await using var transaction  = await connection.BeginTransactionAsync();
        try
        {
            var insertsql = """
                            Insert into Partia(Nazwa,Skrot,DataZalozenia)
                            OUTPUT INSERTED.Id
                            VALUES (@Nazwa, @Skrot, @DataZalozenia)
                            """;
            await using var insertCmd = new SqlCommand(insertsql, connection, (SqlTransaction)transaction);
            insertCmd.Parameters.AddWithValue("@Nazwa", partyData.Nazwa);
            insertCmd.Parameters.AddWithValue("@Skrot", partyData.Skrot);
            insertCmd.Parameters.AddWithValue("@DataZalozenia", partyData.DataZalozenia);
            var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
            foreach (var polityk in politycy)
            {
                var przynaleznoscSql = """
                                       Insert into Przynaleznosc(Partia_Id, Polityk_ID, Od)
                                       values (@PartiaId, @PolitykId, @Od)
                                       """;
                await using var przyznaleznoscCmd =
                    new SqlCommand(przynaleznoscSql, connection, (SqlTransaction)transaction);
                przyznaleznoscCmd.Parameters.AddWithValue("@PartiaId", newId);
                przyznaleznoscCmd.Parameters.AddWithValue("@PolitykId", polityk);
                przyznaleznoscCmd.Parameters.AddWithValue("@Od", DateTime.Now);
            }

            await transaction.CommitAsync();
            return new PartiaGetDto
            {
                Id = newId,
                Nazwa = partyData.Nazwa,
                Skrot = partyData.Skrot,
                DataZalozenia = partyData.DataZalozenia,
                Czlonkowie = politycy

            };

        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
    }
}