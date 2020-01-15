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
    public class AnimalsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AnimalsController(IConfiguration config)
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
        /// Get all animals
        /// </summary>
        /// <returns> A list of Animals </returns>
        [HttpGet]
        public async Task<IActionResult> GetBySearch()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, PetName, Breed 
                                       FROM Animal";

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    List<Animal> animals = new List<Animal>();

                    while (reader.Read())
                    {
                        Animal animal = new Animal
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            PetName = reader.GetString(reader.GetOrdinal("PetName")),
                            Breed = reader.GetString(reader.GetOrdinal("Breed"))
                        };

                        animals.Add(animal);
                    }
                    reader.Close();

                    return Ok(animals);
                }
            }
        }


        /// <summary>
        /// Get Animal By Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns> A single Animal </returns>
        [HttpGet("{id}", Name = "GetAnimal")]

        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, PetName, Breed
                                        FROM Animal
                                        WHERE Id = @Id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    Animal animal = null;

                    if (reader.Read())
                    {
                        animal = new Animal
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            PetName = reader.GetString(reader.GetOrdinal("PetName")),
                            Breed = reader.GetString(reader.GetOrdinal("Breed")),
                        };
                    }

                    reader.Close();

                    if (animal == null)
                    {
                        return NotFound($"No animal found with the Id of {id}");
                    }

                    return Ok(animal);

                }
            }
        }


        /// <summary>
        /// Post new Animal to database
        /// </summary>
        /// <param name="animal"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Animal animal)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Animal
                                       (PetName, Breed)
                                        OUTPUT INSERTED.Id
                                        VALUES (@PetName, @Breed)";
                    cmd.Parameters.Add(new SqlParameter("@PetName", animal.PetName));
                    cmd.Parameters.Add(new SqlParameter("@Breed", animal.Breed));

                    int newId = (int)await cmd.ExecuteScalarAsync();
                    animal.Id = newId;
                    return CreatedAtRoute("GetAnimal", new { id = newId }, animal);
                }
            }
        }

        /// <summary>
        /// Edit/Update Animal 
        ///</summary>
        /// <param name="id"></param>
        /// <param name="animal"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Animal animal)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Animal
                                            SET PetName = @petName,
                                                Breed = @breed
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));
                        cmd.Parameters.Add(new SqlParameter("@PetName", animal.PetName));
                        cmd.Parameters.Add(new SqlParameter("@Breed", animal.Breed));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        return BadRequest($"No animal with the Id {id}");
                    }
                }
            }
            catch (Exception)
            {
                bool exists = await AnimalExists(id);
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
        /// Delete Animal
        ///</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")] //Code for deleting an animal
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Animal WHERE Id = @id";
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
                if (! await AnimalExists(id))
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
        /// Private method to see if an animal exists
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<bool> AnimalExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, PetName, Breed
                        FROM Animal
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    return reader.Read();
                }
            }
        }
    }
}