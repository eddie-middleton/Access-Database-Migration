/*  FILE:       ScriptCreator.cs
 *  PURPOSE:    This static class is used to create SQL scripts that cab help import the contents of a database into a new engine.
 *              There is a single method (with supporting helpers) and an option to either create a bare-bones empty database with
 *              just the database structures or a full replica including data.
 *              This script can overwrite existing data so care, supported by backups, is recommended
 *  AUTHOR:     Eddie Middleton
 *  VERSION:    July 2020
 *  USAGE:      Free for use, no warranty or support provided
 */

/// <summary>
/// Namespace declarations
/// </summary>
using System;
using System.Data;
using System.IO;
using System.Collections.Generic;


//  TODO:   Build a second class, using the LINQ code here -  - to populate the collections

/// <summary>
/// Standard namespace declaration
/// </summary>
namespace EMid.Utility.DatabaseMigration
{
    /// <summary>
    /// This static contains methods to build SQL scripts to allow the recreation of the source database on another platform.
    /// The intended target for this project was SQLite: other engines may need the final output to be tweaked slightly to meet
    /// the requirements. Changes, however, should cosmetic and largely a matter of adjusting formats.
    /// </summary>
    static class ScriptCreator
    {
        /// <summary>
        /// The main method of the class which creates the SQL script for import into another engine. The key steps are as follows:
        ///  - Check whether the tables already exist and remove them if so to avoid conflicts
        ///  - Build the create table definitions
        ///  - Build scripts for each table to load the legacy data into the database
        /// </summary>
        /// <param name="connection">A reference to the database connection</param>
        /// <param name="schema">A dictionary containing all of the scheme information (created by SchemaDocumentor class)</param>
        /// <param name="outputFilename">The fill bame of the output SQL script file</param>
        /// <param name="includeData">A flag to indicate whether data should be included</param>
        internal static void CreateSQL(DatabaseConnector connection, Dictionary<string, List<SchemaColumn>> schema, string outputFilename, bool includeData)
        {
            #region Initial setup and declations
            // Delete previous versions of the output file and open for writing in append mode
            if (File.Exists(outputFilename)) { File.Delete(outputFilename); }
            FileStream file = File.Open(outputFilename, FileMode.Append);
            StreamWriter stream = new StreamWriter(file);
            
            // Write the heading comments and warnings
            stream.WriteLine("-- This file has been created using a database migration utility created by Eddie Middleton");
            stream.WriteLine("-- The utilty and this file are free to use without limitation, but as provided 'as is' with no acceptance of liability");
            stream.WriteLine("-- If used with existing databases this may destroy information. Care, supported by backups, are recommended.");
            stream.WriteLine();
            #endregion

            #region Drop any existing tables
            // Check whether the tables exist in the database and, if so, drop these tables. POTENTIALLY DESTRUCTIVE
            stream.WriteLine("-- Check whether the table names are already present and drop if so.\n");
            foreach (string key in schema.Keys)
            {
                stream.WriteLine($"DROP TABLE IF EXISTS \"{key}\";");
            }
            #endregion
            
            #region  Create the table definitions
            // Now create each of the table definitions using CREATE TABLE
            stream.WriteLine("\n-- Create the table definitions in the database file.\n");
            foreach (KeyValuePair<string, List<SchemaColumn>> table in schema)
            {
                // Initialise the variables used to create the output text
                string tableText = "";
                int columnCount = 0;
                tableText = "CREATE TABLE \"" + table.Key + "\" (\n";
                
                foreach (SchemaColumn col in table.Value)
                {
                    if (columnCount != 0)   { tableText += ", \n"; }
                    columnCount++;
                    tableText += "\t" + col.ToString();
                    if (col.Type == "INTEGER" && col.IsPrimaryKey)  {tableText += " AUTOINCREMENT"; }
                }
                tableText += "\n);";
                stream.WriteLine(tableText);
                stream.WriteLine();
            }
            Console.WriteLine("SQL schema script creation complete...");
            #endregion

            #region Create the scripts to populate the database
            // Now check whether data entries are to be included and process each of the tables if so.
            if (includeData)
            {
                stream.WriteLine("\n-- Writing the data for each table to the script file...");
                int recordCount = 0;
                foreach (KeyValuePair<string, List<SchemaColumn>> table in schema)
                {
                    // Retrieve a dataset from the database to process
                    stream.WriteLine($"\n--Writing records for table: {table.Key}...");
                    string commandText = "SELECT * FROM [" + table.Key + "];";
                    DataSet dataQuery = connection.ExecuteTableQuery(commandText);
                    DataTable tableData = dataQuery.Tables[0];

                    // Now loop through the data set creating an INSERT statement for each row
                    foreach (DataRow row in tableData.AsEnumerable())
                    {
                        // Initialise the variables used to create the output text
                        string dataText = "";
                        int columnCount = 0;
                        dataText = "INSERT INTO \"" + table.Key + "\" (";

                        // Parse the column headers first
                        foreach (SchemaColumn col in table.Value)
                        {
                            if (columnCount != 0)   { dataText += ", "; }
                            columnCount++;
                            dataText += "\"" + col.Name + "\"";
                        }
                        dataText += ") \n\tVALUES (";
                        
                        // Now parse the values data
                        columnCount = 0;
                        foreach (SchemaColumn col in table.Value)
                        {
                            if (columnCount != 0)    { dataText += ", "; }
                            columnCount++;
                            // Format the data based on type
                            switch (col.Type)
                            {
                                case "INTEGER":
                                case "DECIMAL":
                                case "DOUBLE":
                                case "BOOLEAN":
                                    if (row[col.Name] == null)                  { dataText += "NULL"; break; }
                                    if (row[col.Name].ToString().Length == 0)   { dataText += "NULL"; break; }
                                    try     { dataText += row[col.Name].ToString(); }
                                    catch   { dataText += "NULL"; }
                                    break;
                                case "DATETIME":
                                    try
                                    {
                                        DateTime dateValue = (DateTime)row[col.Name];
                                        dataText += "'" + dateValue.ToString("yyyy-MM-dd") +"'";
                                    }
                                    catch   { dataText += "NULL"; }
                                    break;
                                default:
                                    try     { dataText += "\"" + row[col.Name] + "\""; }
                                    catch   { dataText += "NULL"; break; }
                                    break;
                            }
                        }
                        dataText += ");";
                        stream.WriteLine(dataText);
                        recordCount++;
                    }
                }
                // Report results
                Console.WriteLine($"SQL data script creation complete ({recordCount} records complete)...");
                #endregion
            } 
            
            // Complete, report and return the success variable
            stream.Close();
            file.Close();
        }
        
    }
}