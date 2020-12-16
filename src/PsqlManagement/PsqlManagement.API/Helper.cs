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

using PsqlManagement.API.Models;

namespace PsqlManagement.API
{
    /// <summary>
    /// Helper.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Validates database model.
        /// </summary>
        /// <param name="database">The database.</param>
        public static string ValidateDatabaseModel(IDatabase database)
        {
            var response = string.Empty;

            if (string.IsNullOrWhiteSpace(database.Host))
            {
                response += "Host is required. ";
            }

            if (string.IsNullOrWhiteSpace(database.User))
            {
                response += "User is required. ";
            }

            if (string.IsNullOrWhiteSpace(database.Password))
            {
                response += "Password is required. ";
            }

            if (string.IsNullOrWhiteSpace(database.DatabaseName))
            {
                response += "Database is required. ";
            }

            return response.Trim();
        }

        /// <summary>
        /// Builds connection string.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="altDatabase">The alt database.</param>
        /// <param name="altUser">The alt user.</param>
        /// <param name="altPassword">The alt password.</param>
        /// <returns></returns>
        public static string BuildConnectionString(IDatabase database, string altDatabase = null, string altUser = null, string altPassword = null)
        {
            return $"Server={database.Host};Database={altDatabase ?? database.DatabaseName};Port={database.Port};User Id={altUser ?? database.User};Password={altPassword ?? database.Password};Ssl Mode={database.SslMode};Pooling=false;";
        }
    }
}