/*  FILE:       Program.cs
 *  PURPOSE:    This is the main entry point for the programme. The purpose of the programe is threefold:
 *               - Creates a log file that documents all the objects in an Access database file
 *               - Creates an SQL script that can be used to replicate the core table structure in another engine
 *               - Creates an SQL script that can be used to populate the new structure with the underlying data  
 *  AUTHOR:     Eddie Middleton
 *  VERSION:    July 2020
 *  USAGE:      Free for use, no warranty or support provided
 *  NOTES:      There are a couple of aspects of use that should be considered if implementing this utility.
 *               - It requires the use of the Microsoft Access Database Engine 2010/2016 Redistributable
 *               - It may be that the 32-bit is already installed. The RuntimeIdentifier shoule be set to win10-x86
 *               - A reference to the System.Data.DataSetExtensions is included in the .csproj file
 *              The utility makes use of the GetSchema() method in System.Data. The documentation is very helpful:
 *               - https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection.getschema?view=dotnet-plat-ext-3.1
 */

/// <summary>
/// Namespace declarations
/// </summary>
using System;
using System.IO;
using System.Data;
using System.Collections.Generic;

/// <summary>
/// Standard namespace declaration
/// </summary>
namespace EMid.Utility.DatabaseMigration
{
    /// <summary>
    /// Contains the main program logic - the entry point for the console application
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main program entry point. The database to document is supplied as a command line argument
        /// </summary>
        /// <param name="args">The full path of the database file is provided here</param>
        static void Main(string[] args)
        {
            // Check whether a parameter has been supplied and whether this parameter is a valid file
            const string logFile = "Migration-Log.txt";
            const bool includeData = true;
            string databaseFile = "";
            string databasePath = "";
            
            #region Process the command line arguments and set up logging data
            // Check whether a command line argument has been provided
            if (args.Length == 0)
            {
                // Default applied for testing... uncomment code below for live
                Console.WriteLine("No database path provided as a command line argument. Default being applied... ");
                databaseFile = @"C:\Users\Eddie\Documents\Development\ProjectResources\PersonalFinance\PersonalFinance.accdb";
                databasePath = @"C:\Users\Eddie\Documents\Development\ProjectResources\PersonalFinance";
                
                // Console.WriteLine("No database path provided as a command line argument. Terminating... ");
                // Console.ReadLine();
                // System.Environment.Exit(1);
            } 
            // Check that the specified file exists
            else if (!File.Exists(args[0]))
            {
                Console.WriteLine("The specified file cannot be located. Please chack and re-run. Parameter entered:");
                Console.WriteLine(args[0]);
                Console.ReadLine();
                System.Environment.Exit(1);
            } 
            // Set the database file and path used for the log file 
            else
            {
                databaseFile = args[0];
                databasePath = Path.GetDirectoryName(databaseFile);
            }

            // Set the reference to the log file
            string logFilename = "";
            if (databasePath.Length != 0) { logFilename += databasePath; }
            logFilename += "\\" + logFile;
            if (File.Exists(logFilename)) { File.Delete(logFilename); }
            #endregion

            #region Process the database file and create the schema dictionary
            // Initialise a class to connect to the database and variables to retrieve the schema data
            DatabaseConnector connection = new DatabaseConnector(databaseFile);

            // Process and log the metadata for the Microsoft Access database file
            SchemaDocumentor.GetMetadata(connection, logFilename);
            
            // Log the table data and retrieve a list of tables
            List<string> tableNames = SchemaDocumentor.GetTableNames(connection, logFilename);

            // Log the column data by table and create the schema dictionary
            Dictionary<string, List<SchemaColumn>> schema = SchemaDocumentor.GetColumnNames(connection, tableNames, logFilename);

            // Log the index data by table and update the schema dictionary
            schema = SchemaDocumentor.GetIndexColumns(connection, schema, logFilename);

            // Log the views data - this is not used in the schema creation
            SchemaDocumentor.GetViewData(connection, logFilename);

            #endregion

            #region SQL script creation routine
            // Now run the SQL script creation methods
            string outputFilename = Path.GetFileNameWithoutExtension(databaseFile);
            outputFilename = databasePath + "\\" + outputFilename + ".sql";
            ScriptCreator.CreateSQL(connection, schema, outputFilename, includeData);

            #endregion

            // Message this this is complete and wait for user confirmation
            Console.WriteLine("\nExecution complete. Press enter to exit...");
            Console.ReadLine();
        }
    }
}
