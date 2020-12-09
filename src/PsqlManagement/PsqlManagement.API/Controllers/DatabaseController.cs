// MIT License
//
// Copyright (c) 2020 RedSail Technologies
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using PsqlManagement.API.Models;

namespace PsqlManagement.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DatabaseController : ControllerBase
    {
        /// <summary>
        /// The _logger.
        /// </summary>
        private readonly ILogger<DatabaseController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public DatabaseController(ILogger<DatabaseController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets database.
        /// </summary>
        /// <param name="postgresDb">The postgres db.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> GetDatabase(PostgresDatabase postgresDb)
        {
            var errorText = Helper.ValidateDatabaseModel(postgresDb);
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                Response.StatusCode = 422;
                return errorText;
            }

            var dbExists = false;

            var npgsqlConnection = new NpgsqlConnection(Helper.BuildConnectionString(postgresDb, altDatabase: "postgres"));
            npgsqlConnection.Open();

            using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_catalog.pg_database WHERE datname='{postgresDb.DatabaseName}'", npgsqlConnection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    dbExists = reader.HasRows;
                }
            }

            npgsqlConnection.Close();

            if (dbExists)
            {
                return dbExists;
            }
            else
            {
                Response.StatusCode = 404;
                return dbExists;
            }
        }


        /// <summary>
        /// Creates database.
        /// </summary>
        /// <param name="postgresDb">The postgres db.</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<object> CreateDatabase(PostgresDatabase postgresDb)
        {
            var errorText = Helper.ValidateDatabaseModel(postgresDb);
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                Response.StatusCode = 422;
                return errorText;
            }

            var npgsqlConnection = new NpgsqlConnection(Helper.BuildConnectionString(postgresDb, altDatabase: "postgres"));
            npgsqlConnection.Open();

            var dbExists = false;
            using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_catalog.pg_database WHERE datname='{postgresDb.DatabaseName}'", npgsqlConnection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    dbExists = reader.HasRows;
                }
            }

            if (!dbExists || (dbExists && postgresDb.ModifyExisting))
            {
                var roleExists = false;
                var userExists = false;
                var role = postgresDb.DatabaseName;
                var user = postgresDb.DatabaseName;

                if (postgresDb.DatabaseName.Any(char.IsUpper))
                {
                    role += "_Role";
                }
                else
                {
                    role += "_role";
                }

                using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_roles WHERE rolname='{role}'", npgsqlConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        roleExists = reader.HasRows;
                    }
                }

                if (!roleExists)
                {
                    if (!string.IsNullOrWhiteSpace(postgresDb.Platform) && postgresDb.Platform.Equals("Azure", StringComparison.OrdinalIgnoreCase))
                    {
                        new NpgsqlCommand($"CREATE ROLE \"{role}\" with NOLOGIN INHERIT CREATEDB CREATEROLE IN ROLE azure_pg_admin;", npgsqlConnection).ExecuteNonQuery();
                    }
                    else
                    {
                        new NpgsqlCommand($"CREATE ROLE \"{role}\" with NOLOGIN INHERIT CREATEDB CREATEROLE SUPERUSER;", npgsqlConnection).ExecuteNonQuery();
                    }
                }

                var pgUser = postgresDb.User.Substring(0, postgresDb.User.IndexOf('@'));
                new NpgsqlCommand($"GRANT \"{role}\" TO \"{pgUser}\";", npgsqlConnection).ExecuteNonQuery();

                using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_roles WHERE rolname='{user}'", npgsqlConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        userExists = reader.HasRows;
                    }
                }

                if (!userExists)
                {
                    new NpgsqlCommand($"CREATE ROLE \"{user}\" with LOGIN NOSUPERUSER CREATEDB CREATEROLE INHERIT NOREPLICATION CONNECTION LIMIT -1 PASSWORD '{postgresDb.NewUserPassword ?? postgresDb.Password}';", npgsqlConnection).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT \"{role}\" TO \"{user}\";", npgsqlConnection).ExecuteNonQuery();
                }
                else
                {
                    new NpgsqlCommand($"ALTER USER \"{user}\" with LOGIN NOSUPERUSER CREATEDB CREATEROLE INHERIT NOREPLICATION CONNECTION LIMIT -1 PASSWORD '{postgresDb.NewUserPassword ?? postgresDb.Password}';", npgsqlConnection).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT \"{role}\" TO \"{user}\";", npgsqlConnection).ExecuteNonQuery();
                }

                if (!string.IsNullOrWhiteSpace(postgresDb.Platform) && postgresDb.Platform.Equals("Azure", StringComparison.OrdinalIgnoreCase))
                {
                    var host = postgresDb.Host.Substring(0, postgresDb.Host.IndexOf(".postgres"));
                    user += $"@{host}";
                }

                npgsqlConnection.Close();

                using (var dbContext = new DbContext(new DbContextOptionsBuilder().UseNpgsql(Helper.BuildConnectionString(postgresDb, altUser: user, altPassword: postgresDb.NewUserPassword ?? postgresDb.Password)).Options))
                {
                    user = user.Substring(0, user.IndexOf('@'));

                    dbContext.Database.EnsureCreated();

                    if (postgresDb.Schemas != null && postgresDb.Schemas.Count > 0)
                    {
                        foreach (var schema in postgresDb.Schemas)
                        {
                            dbContext.Database.ExecuteSqlRaw($"CREATE SCHEMA IF NOT EXISTS \"{schema}\";");
                            dbContext.Database.ExecuteSqlRaw($"GRANT ALL PRIVILEGES ON SCHEMA \"{schema}\" to \"{role}\";");
                        }
                    }

                    dbContext.Database.ExecuteSqlRaw($"ALTER DATABASE \"{postgresDb.DatabaseName}\" OWNER TO \"{role}\";");
                    dbContext.Database.ExecuteSqlRaw($"GRANT ALL PRIVILEGES ON DATABASE \"{postgresDb.DatabaseName}\" TO \"{role}\";");
                    dbContext.Database.ExecuteSqlRaw($"GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO \"{role}\";");
                    dbContext.Database.ExecuteSqlRaw($"GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO \"{role}\";");
                    dbContext.Database.ExecuteSqlRaw($"GRANT ALL PRIVILEGES ON SCHEMA public to \"{role}\";");

                    dbContext.Database.ExecuteSqlRaw($"REASSIGN OWNED BY \"{user}\" TO \"{role}\";");

                    if (postgresDb.RevokePublicAccess)
                    {
                        dbContext.Database.ExecuteSqlRaw($"REVOKE ALL ON DATABASE \"{postgresDb.DatabaseName}\" FROM PUBLIC CASCADE;");
                    }

                    if (postgresDb.AdditionalSqlCommands != null && postgresDb.AdditionalSqlCommands.Count > 0)
                    {
                        foreach (var sql in postgresDb.AdditionalSqlCommands)
                        {
                            dbContext.Database.ExecuteSqlRaw(sql);
                        }
                    }
                }
            }

            return CreatedAtAction(nameof(GetDatabase), new { postgresDb = postgresDb }, postgresDb.DatabaseName);
        }
    }
}
