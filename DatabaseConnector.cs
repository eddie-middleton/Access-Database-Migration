/*  FILE:       DatabaseCommector.cs
 *  PURPOSE:    This class is used to create a connection to the Access database. The key methods are as follows:
 *               - Connect: creates the connection object based on the  connection string
 *               - ExecuteGetSchema: returns a data table containing schemea information. The parameters determine the return information
 *              The assumption is made in this implementation that the database file is not password protected. Should the utility
 *              need to support passwords then the connection string will need to be changed to support its inclusion programmatically.  
 *  AUTHOR:     Eddie Middleton
 *  VERSION:    July 2020
 *  USAGE:      Free for use, no warranty or support provided
 */

/// <summary>
/// Namespace declarations
/// </summary>
using System;
using System.Data;
using System.Data.OleDb;
using System.Collections.Generic;

/// <summary>
/// Standard namespace declaration
/// </summary>
namespace EMid.Utility.DatabaseMigration
{
    /// <summary>
    /// Creates a connection to the Access database and executes commands to retrieve information
    /// </summary>
    internal class DatabaseConnector
    {
        // Declare the private variables that store the connection string, database name and connection object
        private const string defaultConnection = "Provider=Microsoft.ACE.OleDb.12.0;Data Source=";
        private string _connectionString;
        private string _databaseFile;
        private OleDbConnection _connection;

        /// <summary>
        /// Default constructor for the class. Takes the database filename as an input. The connection string is default
        /// </summary>
        /// <param name="databaseFile">The name of the database file to process</param>
        internal DatabaseConnector(string databaseFile)
        {
            this.ConnectionString = defaultConnection;
            this.DatabaseFile = databaseFile;
        }

        /// <summary>
        /// Creates a connection object and stores that in a private variable
        /// </summary>
        internal void Connect()
        {
            _connection = new OleDbConnection(this.ConnectionString + this.DatabaseFile);
        }

        /// <summary>
        /// Method that returns a data table containing the schema information. Seet the documentation linked in the main
        /// program file for further information on the method. 
        /// </summary>
        /// <param name="collectionName">The collection name to process (Tables, Indexes, Views etc.)</param>
        /// <param name="restrictionValues">An array of restrictions. These are more fully explained in the calling method</param>
        /// <returns></returns>
        internal DataTable ExecuteGetSchema(string collectionName, string[] restrictionValues)
        {
            DataTable result;           // The return value
            
            // Try to open the connection to the database and execute the GetSchema method. If successful set the return value
            try
            {
                Connect();
                _connection.Open();
                if (collectionName.Length == 0)
                {
                    result = _connection.GetSchema();
                }
                else
                {
                    result = _connection.GetSchema(collectionName, restrictionValues);
                }
            }
            // In case of error set the return type and report the error to the console
            catch (Exception e)
            {
                result = null;
                Console.WriteLine("Application has failed in connecting to the database. Error message:");
                Console.WriteLine($"\t {e.Message}");
            }
            // In all instances close the connection
            finally
            {
                _connection.Close();
            }
            return result;
        }

        // TODO: Method required to return a recordset. this will be used to create the SQL script populating the database
        internal DataSet ExecuteTableQuery(string commandText)
        {
            DataSet result;             // The return value
            
            // Try to open the connection to the database and execute a query command
            try
            {
                OleDbCommand command = new OleDbCommand(commandText);
                Connect();
                _connection.Open();
                command.Connection = _connection;
                IDataAdapter adapter = new OleDbDataAdapter(command);
                result = new DataSet();
                adapter.Fill(result);                
            }
            // In case of error set the return value to null and report trhe error to the console
            catch (Exception e)
            {
                result = null;
                Console.WriteLine("Application has failed in connecting to the database. Error message:");
                Console.WriteLine($"\t {e.Message}");
            }
            // In all instances close the connection
            finally
            {
                _connection.Close();
            }
            return result;
        }

        /// <summary>
        /// Accessor method for the connection string. A default is provided in the constructor, but this can be overridden.
        /// </summary>
        /// <value>Connection string</value>
        public string ConnectionString { get => _connectionString; set => _connectionString = value; }
        
        /// <summary>
        /// Accessor methods for the database file name.
        /// </summary>
        /// <value>Full path to the Access database file</value>
        public string DatabaseFile { get => _databaseFile; set => _databaseFile = value; }
    }
}
