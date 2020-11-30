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

namespace PsqlManagement.API.Models
{
    /// <summary>
    /// Postgres Database.
    /// </summary>
    public class PostgresDatabase : IDatabase
    {
        /// <summary>
        /// Gets or sets the platform.
        /// </summary>
        /// <value>
        /// The platform.
        /// </value>
        public string Platform { get; set; }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>
        /// The host.
        /// </value>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        public int Port { get; set; } = 5432;

        /// <summary>
        /// Gets or sets the ssl mode.
        /// </summary>
        /// <value>
        /// The ssl mode.
        /// </value>
        public string SslMode { get; set; } = "Prefer";

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the new user password.
        /// </summary>
        /// <value>
        /// The new user password.
        /// </value>
        public string NewUserPassword { get; set; }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        /// <value>
        /// The database name.
        /// </value>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the schemas.
        /// </summary>
        /// <value>
        /// The schemas.
        /// </value>
        public List<string> Schemas { get; set; }

        /// <summary>
        /// Gets or sets the revoke public access.
        /// </summary>
        /// <value>
        /// The revoke public access.
        /// </value>
        public bool RevokePublicAccess { get; set; } = true;

        /// <summary>
        /// Gets or sets the modify existing.
        /// </summary>
        /// <value>
        /// The modify existing.
        /// </value>
        public bool ModifyExisting { get; set; }
    }
}