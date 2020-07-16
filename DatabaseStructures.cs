/*  FILE:       DatabaseStructures.cs
 *  PURPOSE:    This file defines the structures that are used to support the storage and creation of the database
 *             schema and SQL scripts. The structures are:
 *               - SchemaColumn. Used to hold column attributes and format output for the SQL script
 *  AUTHOR:     Eddie Middleton
 *  VERSION:    July 2020
 *  USAGE:      Free for use, no warranty or support provided
 */


/// <summary>
/// Namespace declarations
/// </summary>
using System;
using System.Collections.Generic;


/// <summary>
/// Standard namespace declaration
/// </summary>
namespace EMid.Utility.DatabaseMigration
{
    /// <summary>
    /// This class holds the information ro define a data field (column). This is not particularly granular but should serve most needs
    /// </summary>
    internal struct SchemaColumn
    {
        internal string Name;               // The name of the column
        internal string Type;               // The data type - string representation
        internal bool IsNullable;           // Indicates whether the column is nullable
        internal bool IsPrimaryKey;         // Indicator for a primary key field

        /// <summary>
        /// Provides a string representation of a column. Used in writing the SQL script 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string columnString = "\"" + this.Name + "\" " + this.Type;
            if (this.IsPrimaryKey)      { columnString += " PRIMARY KEY"; } 
            else if (this.IsNullable)   { columnString += " NULL"; }
            else                        { columnString += " NOT NULL"; }
            return columnString;
        }
    }
}
