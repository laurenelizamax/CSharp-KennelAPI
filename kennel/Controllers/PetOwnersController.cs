using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System;
using kennel.Models;

namespace BangazonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PetOwnersController : ControllerBase
    {
        private readonly IConfiguration _config;

        public PetOwnersController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        /// <summary>
        /// Get all owners
        /// </summary>
        /// <returns> A list of Owners </returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, FirstName, LastName, PetName
                                       FROM PetOwner";

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    List<PetOwner> petOwners = new List<PetOwner>();

                    while (reader.Read())
                    {
                        PetOwner petOwner = new PetOwner
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            PetName = reader.GetString(reader.GetOrdinal("PetName"))
                        };

                        petOwners.Add(petOwner);
                    }
                    reader.Close();

                    return Ok(petOwners);
                }
            }
        }


        /// <summary>
        /// Get Owner By Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns> A single PetOwner </returns>
        [HttpGet("{id}", Name = "GetPetOwner")]

        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, FirstName, LastName, PetName
                                        FROM PetOwner
                                        WHERE Id = @Id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    PetOwner petOwner = null;

                    if (reader.Read())
                    {
                        petOwner = new PetOwner
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            PetName = reader.GetString(reader.GetOrdinal("PetName"))
                        };
                    }

                    reader.Close();

                    if (petOwner == null)
                    {
                        return NotFound($"No Pet Owner found with the Id of {id}");
                    }

                    return Ok(petOwner);

                }
            }
        }


        /// <summary>
        /// Post new Pet Owner to database
        /// </summary>
        /// <param name="petOwner"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PetOwner petOwner)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO PetOwner
                                       (FirstName, LastName, PetName)
                                        OUTPUT INSERTED.Id
                                        VALUES (@FirstName, @LastName, @PetName)";
                    cmd.Parameters.Add(new SqlParameter("@FirstName", petOwner.FirstName));
                    cmd.Parameters.Add(new SqlParameter("@LastName", petOwner.LastName));
                    cmd.Parameters.Add(new SqlParameter("PetName", petOwner.PetName));

                    int newId = (int)await cmd.ExecuteScalarAsync();
                    petOwner.Id = newId;
                    return CreatedAtRoute("GetPetOwner", new { id = newId }, petOwner);
                }
            }
        }

        /// <summary>
        /// Edit/Update Pet Owner 
        ///</summary>
        /// <param name="id"></param>
        /// <param name="petOwner"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] PetOwner petOwner)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE PetOwner
                                            SET FirstName = @firstName,
                                                LastName = @lastName,
                                                PetName = @petName
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));
                        cmd.Parameters.Add(new SqlParameter("@FirstName", petOwner.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@LastName", petOwner.LastName));
                        cmd.Parameters.Add(new SqlParameter("@PetName", petOwner.PetName));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        return BadRequest($"No Pet Owner with the Id {id}");
                    }
                }
            }
            catch (Exception)
            {
                bool exists = await PetOwnerExists(id);
                if (!exists)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Delete Pet Owner
        ///</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")] //Code for deleting an employee
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM PetOwner WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!await PetOwnerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Private method to see if an pet owner exists
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<bool> PetOwnerExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, FirstName, LastName, PetName
                        FROM PetOwner
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    return reader.Read();
                }
            }
        }
    }
}