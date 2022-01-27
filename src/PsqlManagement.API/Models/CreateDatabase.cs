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
    /// Create Database.
    /// </summary>
    public class CreateDatabase : Database
    {

        /// <summary>
        /// Gets or sets the new user password.
        /// </summary>
        /// <value>
        /// The new user password.
        /// </value>
        public string NewUserPassword { get; set; }

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

        /// <summary>
        /// Gets or sets the additional sql commands.
        /// </summary>
        /// <value>
        /// The additional sql commands.
        /// </value>
        public List<string> AdditionalSqlCommands { get; set; }
    }
}