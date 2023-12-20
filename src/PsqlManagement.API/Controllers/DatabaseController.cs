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
using System.Net;
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
        /// <param name="database">The postgres db.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> GetDatabase(Database database)
        {
            var errorText = Helper.ValidateDatabaseModel(database);
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                Response.StatusCode = 422;
                return errorText;
            }

            var dbExists = new Common().dbExists(database);

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
        /// <param name="database">The postgres db.</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<object> CreateDatabase(CreateDatabase database)
        {
            var errorText = Helper.ValidateDatabaseModel(database);
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                Response.StatusCode = 422;
                return errorText;
            }

            var dbExists = new Common().dbExists(database);

            if (!dbExists || (dbExists && database.ModifyExisting))
            {
                var role = database.DatabaseName;
                var user = database.DatabaseName;
                var npgsqlConnection = new NpgsqlConnection(Helper.BuildConnectionString(database, altDatabase: "postgres"));
                npgsqlConnection.Open();
                try
                {
                    var roleExists = false;
                    var userExists = false;

                    if (database.DatabaseName.Any(char.IsUpper))
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
                        if (!string.IsNullOrWhiteSpace(database.Platform) && database.Platform.StartsWith("Azure", StringComparison.OrdinalIgnoreCase))
                        {
                            new NpgsqlCommand($"CREATE ROLE \"{role}\" with NOLOGIN INHERIT CREATEDB CREATEROLE IN ROLE azure_pg_admin;", npgsqlConnection).ExecuteNonQuery();
                        }
                        else
                        {
                            new NpgsqlCommand($"CREATE ROLE \"{role}\" with NOLOGIN INHERIT CREATEDB CREATEROLE SUPERUSER;", npgsqlConnection).ExecuteNonQuery();
                        }
                    }

                    var pgUser = database.User;
                    if (pgUser.Contains("@"))
                    {
                        pgUser = pgUser.Substring(0, pgUser.IndexOf('@'));
                    }

                    new NpgsqlCommand($"GRANT \"{role}\" TO \"{pgUser}\";", npgsqlConnection).ExecuteNonQuery();

                    using (var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_roles WHERE rolname='{user}'", npgsqlConnection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            userExists = reader.HasRows;
                        }
                    }

                    var commandType = userExists ? "ALTER" : "CREATE";
                    var privileges = $"LOGIN INHERIT CREATEDB CREATEROLE SUPERUSER NOREPLICATION CONNECTION LIMIT -1 PASSWORD '{database.NewUserPassword ?? database.Password}'";

                    // if azure, remove SUPERUSER
                    if (!string.IsNullOrWhiteSpace(database.Platform) && database.Platform.StartsWith("Azure", StringComparison.OrdinalIgnoreCase))
                    {
                        privileges = privileges.Replace("SUPERUSER ", "");
                    }

                    // create or update user
                    new NpgsqlCommand($"{commandType} USER \"{user}\" with {privileges};", npgsqlConnection).ExecuteNonQuery();

                    // add user to role
                    new NpgsqlCommand($"GRANT \"{role}\" TO \"{user}\";", npgsqlConnection).ExecuteNonQuery();

                    // if azure, add user to azure_pg_admin role
                    if (!string.IsNullOrWhiteSpace(database.Platform) && database.Platform.StartsWith("Azure", StringComparison.OrdinalIgnoreCase))
                    {
                        new NpgsqlCommand($"GRANT azure_pg_admin TO \"{user}\";", npgsqlConnection).ExecuteNonQuery();
                    }

                    if (!dbExists)
                    {
                        new NpgsqlCommand($"CREATE DATABASE \"{database.DatabaseName}\" TEMPLATE template0 OWNER \"{role}\";", npgsqlConnection).ExecuteNonQuery();
                    }

                    if (!string.IsNullOrWhiteSpace(database.Platform) && database.Platform.Equals("Azure", StringComparison.OrdinalIgnoreCase))
                    {
                        var host = database.Host.Substring(0, database.Host.IndexOf(".postgres"));
                        user += $"@{host}";
                    }

                    npgsqlConnection.Close();
                }
                catch (Exception)
                {
                    npgsqlConnection.Close();
                    throw;
                }


                var npgsqlConnection2 = new NpgsqlConnection(Helper.BuildConnectionString(database, altUser: user, altPassword: database.NewUserPassword ?? database.Password));
                npgsqlConnection2.Open();
                try
                {
                    if (user.Contains("@"))
                    {
                        user = user.Substring(0, user.IndexOf('@'));
                    }

                    if (database.Schemas != null && database.Schemas.Count > 0)
                    {
                        foreach (var schema in database.Schemas)
                        {
                            new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS \"{schema}\";", npgsqlConnection2).ExecuteNonQuery();
                            new NpgsqlCommand($"GRANT ALL PRIVILEGES ON SCHEMA \"{schema}\" to \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                        }
                    }

                    new NpgsqlCommand($"ALTER DATABASE \"{database.DatabaseName}\" OWNER TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT ALL PRIVILEGES ON DATABASE \"{database.DatabaseName}\" TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();
                    new NpgsqlCommand($"GRANT ALL PRIVILEGES ON SCHEMA public to \"{role}\";", npgsqlConnection2).ExecuteNonQuery();

                    new NpgsqlCommand($"REASSIGN OWNED BY \"{user}\" TO \"{role}\";", npgsqlConnection2).ExecuteNonQuery();

                    if (database.RevokePublicAccess)
                    {
                        new NpgsqlCommand($"REVOKE ALL ON DATABASE \"{database.DatabaseName}\" FROM PUBLIC CASCADE;", npgsqlConnection2).ExecuteNonQuery();
                    }

                    if (database.AdditionalSqlCommands != null && database.AdditionalSqlCommands.Count > 0)
                    {
                        foreach (var sql in database.AdditionalSqlCommands)
                        {
                            var queryString = database.UrlDecodeAdditionalSqlCommands ? WebUtility.UrlDecode(sql) : sql;
                            new NpgsqlCommand(queryString, npgsqlConnection2).ExecuteNonQuery();
                        }
                    }

                    npgsqlConnection2.Close();
                }
                catch (Exception)
                {
                    npgsqlConnection2.Close();
                    throw;
                }
            }

            return CreatedAtAction(nameof(GetDatabase), new { database = database }, database.DatabaseName);
        }
    }
}
