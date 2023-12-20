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

using System.Collections.Generic;
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
    public class QueryController : ControllerBase
    {
        /// <summary>
        /// The _logger.
        /// </summary>
        private readonly ILogger<QueryController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public QueryController(ILogger<QueryController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets data using ExecuteReader.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> GetData(Query query)
        {
            var errorText = Helper.ValidateDatabaseModel(query);
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                Response.StatusCode = 422;
                return errorText;
            }

            var dbExists = new Common().dbExists(query);

            if (dbExists)
            {
                var npgsqlConnection = new NpgsqlConnection(Helper.BuildConnectionString(query));
                npgsqlConnection.Open();

                var results = new List<Dictionary<string, string>>();

                try
                {
                    var queryString = query.UrlDecodeQueryString ? WebUtility.UrlDecode(query.QueryString) : query.QueryString;
                    var q = new NpgsqlCommand(queryString, npgsqlConnection);
                    var dataReader = q.ExecuteReader();
                    while (dataReader.Read())
                    {
                        var row = new Dictionary<string, string>();

                        if (dataReader.FieldCount > 0)
                        {
                            int i = 0;
                            while (i < dataReader.FieldCount)
                            {
                                row.Add(dataReader.GetName(i), dataReader.GetValue(i).ToString());
                                i++;
                            }
                        }

                        results.Add(row);
                    }
                }
                catch
                {
                    return StatusCodes.Status400BadRequest;
                }
                finally
                {
                    npgsqlConnection.Close();
                }

                Response.StatusCode = 200;
                return results;
            }
            else
            {
                return StatusCodes.Status404NotFound;
            }
        }


        /// <summary>
        /// Runs query using ExecuteNonQuery.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<object> RunQuery(Query query)
        {
            var errorText = Helper.ValidateDatabaseModel(query);
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                Response.StatusCode = 422;
                return errorText;
            }

            var dbExists = new Common().dbExists(query);

            if (dbExists)
            {
                var npgsqlConnection = new NpgsqlConnection(Helper.BuildConnectionString(query));
                npgsqlConnection.Open();

                try
                {
                    var queryString = query.UrlDecodeQueryString ? WebUtility.UrlDecode(query.QueryString) : query.QueryString;
                    new NpgsqlCommand(queryString, npgsqlConnection).ExecuteNonQuery();
                    return StatusCodes.Status202Accepted;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    npgsqlConnection.Close();
                }
            }
            else
            {
                return StatusCodes.Status404NotFound;
            }
        }
    }
}
