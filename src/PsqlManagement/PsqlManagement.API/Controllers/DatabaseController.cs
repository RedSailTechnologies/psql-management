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

            var dbExists = false;

            var npgsqlConnection = new NpgsqlConnection(Helper.BuildConnectionString(postgresDb, altDatabase: "postgres"));
            npgsqlConnection.Open();
            try
            {

                using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_catalog.pg_database WHERE datname='{postgresDb.DatabaseName}'", npgsqlConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        dbExists = reader.HasRows;
                    }
                }
            }
            catch (Exception e)
            {
                npgsqlConnection.Close();
                throw e;
            }

            if (!dbExists || (dbExists && postgresDb.ModifyExisting))
            {
                var role = postgresDb.DatabaseName;
                var user = postgresDb.DatabaseName;
                try
                {
                    var roleExists = false;
                    var userExists = false;

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

                    var pgUser = postgresDb.User;
                    if (pgUser.Contains("@"))
                    {
                        pgUser = pgUser.Substring(0, postgresDb.User.IndexOf('@'));
                    }

                    new NpgsqlCommand($"GRANT \"{role}\" TO \"{pgUser}\";", npgsqlConnection).ExecuteNonQuery();

                    using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_roles WHERE rolname='{user}'", npgsqlConnection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            userExists = reader.HasRows;
                        }
                    }

                    var commandType = "CREATE";
                    if (userExists)
                    {
                        commandType = "UPDATE";
                    }

                    var privileges = $"LOGIN INHERIT CREATEDB CREATEROLE SUPERUSER NOREPLICATION CONNECTION LIMIT -1 PASSWORD '{postgresDb.NewUserPassword ?? postgresDb.Password}'";
                    if (!string.IsNullOrWhiteSpace(postgresDb.Platform) && postgresDb.Platform.Equals("Azure", StringComparison.OrdinalIgnoreCase))
                    {
                        privileges = $"LOGIN INHERIT CREATEDB CREATEROLE IN ROLE azure_pg_admin NOREPLICATION CONNECTION LIMIT -1 PASSWORD '{postgresDb.NewUserPassword ?? postgresDb.Password}'";
                    }

                    new NpgsqlCommand($"{commandType} USER \"{user}\" with {privileges};", npgsqlConnection).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT \"{role}\" TO \"{user}\";", npgsqlConnection).ExecuteNonQuery();

                    if (!dbExists)
                    {
                        new NpgsqlCommand($"CREATE DATABASE \"{postgresDb.DatabaseName}\" TEMPLATE template0 OWNER \"{role}\";", npgsqlConnection).ExecuteNonQuery();
                    }

                    if (!string.IsNullOrWhiteSpace(postgresDb.Platform) && postgresDb.Platform.Equals("Azure", StringComparison.OrdinalIgnoreCase))
                    {
                        var host = postgresDb.Host.Substring(0, postgresDb.Host.IndexOf(".postgres"));
                        user += $"@{host}";
                    }

                    npgsqlConnection.Close();
                }
                catch (Exception e)
                {
                    npgsqlConnection.Close();
                    throw e;
                }


                var npgsqlConnection2 = new NpgsqlConnection(Helper.BuildConnectionString(postgresDb, altUser: user, altPassword: postgresDb.NewUserPassword ?? postgresDb.Password));
                npgsqlConnection2.Open();
                try
                {
                    if (user.Contains("@"))
                    {
                        user = user.Substring(0, postgresDb.User.IndexOf('@'));
                    }

                    if (postgresDb.Schemas != null && postgresDb.Schemas.Count > 0)
                    {
                        foreach (var schema in postgresDb.Schemas)
                        {
                            new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS \"{schema}\";", npgsqlConnection2).ExecuteNonQuery();
                            new NpgsqlCommand($"GRANT ALL PRIVILEGES ON SCHEMA \"{schema}\" to \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                        }
                    }

                    new NpgsqlCommand($"ALTER DATABASE \"{postgresDb.DatabaseName}\" OWNER TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT ALL PRIVILEGES ON DATABASE \"{postgresDb.DatabaseName}\" TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT ALL PRIVILEGES ON SCHEMA public to \"{role}\";", npgsqlConnection2).ExecuteNonQuery();

                    new NpgsqlCommand($"REASSIGN OWNED BY \"{user}\" TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();

                    if (postgresDb.RevokePublicAccess)
                    {
                        new NpgsqlCommand($"REVOKE ALL ON DATABASE \"{postgresDb.DatabaseName}\" FROM PUBLIC CASCADE;", npgsqlConnection2).ExecuteNonQuery();
                    }

                    if (postgresDb.AdditionalSqlCommands != null && postgresDb.AdditionalSqlCommands.Count > 0)
                    {
                        foreach (var sql in postgresDb.AdditionalSqlCommands)
                        {
                            new NpgsqlCommand(sql, npgsqlConnection2).ExecuteNonQuery();
                        }
                    }

                    npgsqlConnection2.Close();
                }
                catch (Exception e)
                {
                    npgsqlConnection2.Close();
                    throw e;
                }
            }

            return CreatedAtAction(nameof(GetDatabase), new { postgresDb = postgresDb }, postgresDb.DatabaseName);
        }
    }
}
