/*  FILE:       SchemaDocumentor.cs
 *  PURPOSE:    This class is used for two distict purposes:
 *               - Create a text log file documenting the structure of the Microsoft Access table. This supports analysis
 *               - Create an internal object representing the schema that is used later to create a SQL script
 *              There is a set of methods (Get...) to process various objects within the scheme (Tables, Columns, Indexes etc) 
 *              supported by a helper method (LogSchemaData) that writes the text log.
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
using System.Linq;


/// <summary>
/// Standard namespace declaration
/// </summary>
namespace EMid.Utility.DatabaseMigration
{
    /// <summary>
    /// This class processes the database file, writes a log and builds the schema object.
    /// Note this is a static class - the methods are used to create part of the program output
    /// </summary>
    static class SchemaDocumentor
    {
        #region Log the database metadata
        // Read the database metadata and write this to the log. This is not used in the creation of the schema object
        /// <summary>
        /// Read the database metadata and write this to the log. This is not used in the creation of the schema object
        /// </summary>
        /// <param name="connection">A reference to the database connection</param>
        /// <param name="logFilename">The full file path of the log file. Created next to the source database</param>
        internal static void GetMetadata(DatabaseConnector connection, string logFilename)
        {
            // Get the database metadata and add this to the log file. 
            // This is provided for inforrmation only. This is not used in the creation of the schema
            string[] restrictionValues = new string[4];
            DataTable schemaData = connection.ExecuteGetSchema("", restrictionValues);
            if (schemaData != null) { SchemaDocumentor.LogSchemaData(schemaData, logFilename, "Metadata", 25); }
            Console.WriteLine("Processed database metadata...");
        }
        #endregion
        
        #region Log and retrieve the table names
        /// <summary>
        /// Retrieve the list of table objects and log these. The names of the required tables are returned in a list 
        /// </summary>
        /// <param name="connection">A reference to the database connection</param>
        /// <param name="logFilename">The full file path of the log file.</param>
        /// <returns>A list of database table names - user tables, not system</returns>
        internal static List<string> GetTableNames(DatabaseConnector connection, string logFilename)
        {
            // Create the return variable to hold a list of tables
            List<string> tableNames = new List<string>();

            // Get the table information. This is stored as a list that is used in the construction of the 
            // column data and other schema objects. The tables collection allows for four restrictions;
            // catalog, cchema, table Name and table type. We're not interested in the system tables, so set
            // the final of these restrictions to the 'Tables' type.
            string[] restrictionValues = new string[4];
            restrictionValues[3] = "Table";
            DataTable schemaData = connection.ExecuteGetSchema("Tables", restrictionValues);
            if (schemaData != null) 
            { 
                // Update the log file
                SchemaDocumentor.LogSchemaData(schemaData, logFilename, "Tables", 25);        // Log file documentation
                
                // Now extract the table names and populate the list
                var selectedRows = from content in schemaData.AsEnumerable()
                                    select new { TableName = content["table_name"] };
            
                foreach (var row in selectedRows)   { tableNames.Add(row.TableName.ToString()); }
            }
            Console.WriteLine("Processed database table data...");
            return tableNames;
        }
        #endregion

        #region Log and retrive the column names
        /// <summary>
        /// Retrieve the list of column objects by table and log these. For each table a list of columns is created and then
        /// added to the database schema object which is returned
        /// </summary>
        /// <param name="connection">A reference to the database connection</param>
        /// <param name="tableNames">The list of table names to process</param>
        /// <param name="logFilename">The full file path of the log file</param>
        /// <returns>A database schema object containing the table and column information for the Access database</returns>
        internal static Dictionary<string, List<SchemaColumn>> GetColumnNames(DatabaseConnector connection, List<string> tableNames, string logFilename)
        {
            // Create the return variable to hold the schema
            Dictionary<string, List<SchemaColumn>> schema = new Dictionary<string, List<SchemaColumn>>();

            // Variables to hold the data table containing the column information
            DataTable schemaData;
            List<SchemaColumn> columnList;
            string[] restrictionValues = new string[4];

            // For each of the tables in the database get the column schema data relating to that table and process
            foreach (string tableName in tableNames)
            {
                // Set the restriction (filter) to be the table name and retrieve the column data
                restrictionValues[2] = tableName;
                schemaData = connection.ExecuteGetSchema("Columns", restrictionValues);
                if (schemaData != null)
                {
                    // Update the log file
                    SchemaDocumentor.LogSchemaData(schemaData, logFilename, "Columns for Table: " + tableName, 25);
                    
                    // Create an empty list to hold the column data 
                    columnList = new List<SchemaColumn>();
                    
                    // Extract the column data and populate the list
                    var selectedRows = from content in schemaData.AsEnumerable()
                                        select new
                                        {
                                            ColumnName = content["COLUMN_NAME"],
                                            ColumnDataType = content["DATA_TYPE"],
                                            ColumnLength = content["CHARACTER_MAXIMUM_LENGTH"],
                                            ColumnNullable = content["IS_NULLABLE"],
                                            ColumnOrder = content["ORDINAL_POSITION"]
                                        };
                    
                    // Now loop through this row collection and populate a new schema column
                    SchemaColumn currentColumn;
                    foreach (var row in selectedRows.OrderBy(row => row.ColumnOrder))
                    {
                        currentColumn.Name = row.ColumnName.ToString();
                        currentColumn.Type = MapDataType((int) row.ColumnDataType);
                        currentColumn.IsNullable = (bool) row.ColumnNullable;
                        currentColumn.IsPrimaryKey = false;
                        columnList.Add(currentColumn);
                    }
                    schema.Add(tableName, columnList);
                }
            }
            Console.WriteLine("Processed database column data...");
            return schema;
        }
        #endregion

        #region Log and retrive the primary key column names

        /// <summary>
        /// Retrieve the list of primary keys by table and log these. For each table a duplicate the column ist and update values
        /// if a primary key before adding  to the database schema dictionary which is returned
        /// </summary>
        /// <param name="connection">A reference to the database connection</param>
        /// <param name="schema">A reference to the schema that is to be duolicated and updated</param>
        /// <param name="logFilename">The full file path of the log file</param>
        /// <returns></returns>
        internal static Dictionary<string, List<SchemaColumn>> GetIndexColumns(DatabaseConnector connection, Dictionary<string, List<SchemaColumn>> schema, string logFilename)
        {
            // Create an empty dictionary to use as a return value
            Dictionary<string, List<SchemaColumn>> schemaReturn = new Dictionary<string, List<SchemaColumn>>();

            // There are five restrictions possible; Catalog, Schema, Table Name, Constraint Name and Column Name.
            // We use two: table name and the constriant name PrimaryKey
            string[] restrictionValues = new string[5];
            restrictionValues[2] = "PrimaryKey";

            // Loops through each table, documenting the primary key data and extracting the primary key columns
            foreach (KeyValuePair<string, List<SchemaColumn>> table in schema)
            {
                // Get the table name from the dictionary entry
                string tableName = table.Key;
                
                // Add the table name as a second restriction, set up varaibles for the primary key list and duplicate schema.
                List<SchemaColumn> updatedColumns = new List<SchemaColumn>();
                List<string> keyList = new List<string>();
                restrictionValues[4] = tableName;
                DataTable schemaData = connection.ExecuteGetSchema("Indexes", restrictionValues);
                
                // Get the index information and doument this in the log file
                if (schemaData != null) 
                { 
                    // Update the logfile
                    SchemaDocumentor.LogSchemaData(schemaData, logFilename, "Index for Table:" + tableName, 25);

                    // Extract the list of primary key columns
                    var selectedRows = from content in schemaData.AsEnumerable()
                                        select new
                                        {
                                            columnName = content["COLUMN_NAME"],
                                            ColumnOrder = content["ORDINAL_POSITION"]
                                        };
                    
                    // Now loop through this row collection and populate a list of key fields
                    foreach (var row in selectedRows.OrderBy(row => row.ColumnOrder)) { keyList.Add(row.columnName.ToString()); }
                }

                // For each column in the schema, check against the key list. Write the original and updated column data to the new list
                foreach (SchemaColumn col in table.Value)
                {
                    SchemaColumn updatedColumn;
                    updatedColumn.Name = col.Name;
                    updatedColumn.Type = col.Type;
                    updatedColumn.IsNullable = col.IsNullable;
                    updatedColumn.IsPrimaryKey = false;
                    
                    // Check whether this column is a primary key
                    if (keyList.Contains(col.Name))     
                    {
                        updatedColumn.IsPrimaryKey = true;
                    }
                    updatedColumns.Add(updatedColumn);
                }
                
                // Update the duplicate schema with the updated table definition
                schemaReturn.Add(tableName, updatedColumns);
            }

            // Confirm completion and return the updated schema
            Console.WriteLine("Processed database index data...");
            return schemaReturn;
        }
        #endregion

        #region Log the views data...
        /// <summary>
        /// Read the views definitions from the database file and write this to the log.true This is not used in the schema but is helpful
        /// </summary>
        /// <param name="connection">A reference to the database connection</param>
        /// <param name="logFilename">The full file path of the log file</param>
        internal static void GetViewData(DatabaseConnector connection, string logFilename)
        {
            // Get the database views and add this to the log file. Three restrictions are required - none are used. 
            // This is provided for inforrmation only. This is not used in the creation of the schema
            string[] restrictionValues = new string[3];
            DataTable schemaData = connection.ExecuteGetSchema("Views", restrictionValues);
            if (schemaData != null) { SchemaDocumentor.LogSchemaData(schemaData, logFilename, "Views", 32); }
            Console.WriteLine("Processed database views data...");
        }
        #endregion

        #region Helper methods to write to the log file and map data types 
        /// <summary>
        /// Helper function that writes to the logfile. The material that is written to the log is defined by the table 
        /// </summary>
        /// <param name="table">The table of database objects to log</param>
        /// <param name="logFilename">The full path name of the log file</param>
        /// <param name="title">The title of this section in the log file</param>
        /// <param name="length">The padded length for each of the columns</param>
        private static void LogSchemaData(DataTable table, string logFilename, string title, int length)
        {
            // Open the log file for writing in append mode
            FileStream file = File.Open(logFilename, FileMode.Append);
            StreamWriter stream = new StreamWriter(file);
            
            // Write the section title
            stream.WriteLine("\n" + title);
            stream.WriteLine(string.Concat(Enumerable.Repeat("=", 80)));

            // Process the column headers
            foreach (DataColumn col in table.Columns)
            {
                stream.Write("{0,-" + length + "}", col.ColumnName);
            }
            stream.WriteLine();

            // Now process the individual rows in the data table structure 
            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    if (col.DataType.Equals(typeof(DateTime)))
                    {
                        stream.Write("{0,-" + length + ":d}", row[col]);
                    }
                    else if (col.DataType.Equals(typeof(Decimal)))
                    {
                        stream.Write("{0,-" + length + ":C}", row[col]);
                    }
                    else
                    { 
                        stream.Write("{0,-" + length + "}", row[col]);
                    }
                }
                stream.WriteLine();
            }
            // Close the file
            stream.WriteLine();
            stream.Close();
            file.Close();
        }

        /// <summary>
        /// This method is a kludge.null It takes the integer representation of the datatype and returns a string that can be used
        /// in the creation of the SQL scripts. The values themselves are pretty messy, but are seemingly documented here:
        /// https://docs.microsoft.com/en-us/dotnet/api/system.data.oledb.oledbtype?view=dotnet-plat-ext-3.1
        /// The translation could be made more granular depending on target platform. This should be good for SQLite
        /// </summary>
        /// <param name="typeID">The integer representation of type</param>
        /// <returns>A string representation of type</returns>
        private static string MapDataType(int typeID)
        {
            // Map the different types to a series of arrays
            int[] booleanType = {11};                                   // BOOLEAN
            int[] datetimeType = {7, 64,133,134,135};                   // DATETIME
            int[] decimalType = {6, 14,131,139};                        // DECIMAL
            int[] doubleType = {4, 5};                                  // DOUBLE
            int[] intType = {2, 3,16,17,18,19,20,21,128,204,205};       // INT
            int[] nullType = {0};                                       // NULL
            int[] stringType = {8, 72,129,130,200,201,202,203};         // STRING
            int[] blobType = {9, 10,12,13,138};                         // BLOB
            
            // Now just check against each of these arrays
            string mappedDataType = "BLOB";
            if (booleanType.Contains(typeID))       { mappedDataType = "BOOLEAN"; }
            if (datetimeType.Contains(typeID))      { mappedDataType = "DATETIME"; }
            if (decimalType.Contains(typeID))       { mappedDataType = "DECIMAL"; }
            if (doubleType.Contains(typeID))        { mappedDataType = "DOUBLE"; }
            if (intType.Contains(typeID))           { mappedDataType = "INTEGER"; }
            if (stringType.Contains(typeID))        { mappedDataType = "STRING"; }
            
            // Now return the data type
            return mappedDataType;
        }
        #endregion
    }
}
