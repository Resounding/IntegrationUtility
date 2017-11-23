﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataTransfer.AccessDatabase;

namespace LinkGreenODBCUtility
{
    public class Mapping
    {
        public string DsnName;
        public static string TransferDsnName = Settings.DsnName;
        public bool _validFields = true;
        public bool _validPushFields = true;
        public bool _validUpdateFields;

        public Mapping()
        {
            DsnName = TransferDsnName;
        }

        public Mapping(string dsnName)
        {
            if (!string.IsNullOrEmpty(dsnName))
            {
                DsnName = dsnName;
            }
        }

        public List<string> GetTableNames()
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={DsnName}");
            Credentials creds = DsnCreds.GetDsnCreds(DsnName);
            if (creds != null)
            {
                if (!string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
                {
                    _connection.ConnectionString = $"DSN={DsnName};Uid={creds.Username};Pwd={creds.Password}";
                }
            }

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                MessageBox.Show($"Failed to connect to DSN {DsnName}. Are your credentials set?", "Failed to Connect");
                return new List<string>();
            }

            try
            {
                var tables = _connection.GetSchema("Tables");
                List<string> tableNames = new List<string>();

                foreach (DataRow row in tables.Rows)
                {
                    tableNames.Add(row["TABLE_NAME"].ToString());
                }

                var views = _connection.GetSchema("Views");

                foreach (DataRow row in views.Rows) {
                    tableNames.Add(row["TABLE_NAME"].ToString());
                }

                return tableNames;
            }
            finally
            {
                ConnectionInstance.CloseConnection($"DSN={DsnName}");
            }
        }

        public string GetDsnName(string tableName)
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            var command = new OdbcCommand($"SELECT `DsnName` FROM `TableMappings` WHERE `TableName` = '{tableName}'", _connection);

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return null;
            }

            OdbcDataReader reader = command.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    return reader[0].ToString();
                }

                return null;
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }
        }

        public string GetTableMapping(string tableName)
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            var command = new OdbcCommand($"SELECT `MappingName` FROM `TableMappings` WHERE `TableName` = '{tableName}'", _connection);

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return null;
            }

            OdbcDataReader reader = command.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    return reader[0].ToString();
                }

                return null;
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }
        }

        public string GetFieldMapping(string tableName, string fieldName)
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            var command = new OdbcCommand($"SELECT `MappingName` FROM `FieldMappings` WHERE `TableName` = '{tableName}' AND `FieldName` = '{fieldName}'", _connection);

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return null;
            }

            OdbcDataReader reader = command.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    return reader[0].ToString();
                }

                return null;
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }
        }

        public string GetMappingField(string tableName, string mappingName)
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            var command = new OdbcCommand($"SELECT `FieldName` FROM `FieldMappings` WHERE `TableName` = '{tableName}' AND `MappingName` = '{mappingName}'", _connection);

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return null;
            }

            OdbcDataReader reader = command.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    return reader[0].ToString();
                }

                return null;
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }
        }

        public string GetFieldProperty(string tableName, string fieldName, string property, OdbcConnection _connection = null)
        {
            if (_connection == null)
            {
                _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            }
            var command = new OdbcCommand($"SELECT `{property}` FROM `FieldMappings` WHERE `TableName` = '{tableName}' AND (`FieldName` = '{fieldName}' OR `MappingName` = '{fieldName}')", _connection);

            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }

            OdbcDataReader reader = command.ExecuteReader();

            try
            {
                while (reader.Read())
                {
                    return reader[0].ToString();
                }

                return null;
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }
        }

        public List<MappingField> GetTableFields(string tableName)
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            var command = new OdbcCommand($"SELECT `TableName`, `FieldName`, `MappingName`, `DisplayName`, `Description`, `DataType`, `Required` FROM `FieldMappings` WHERE `TableName` = '{tableName}'", _connection);

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return new List<MappingField>();
            }

            OdbcDataReader reader = command.ExecuteReader();
            Dictionary<string, int> columnIndexes = new Dictionary<string, int>();

            for (int x = 0; x < reader.FieldCount; x++)
            {
                if (!string.IsNullOrEmpty(reader.GetName(x)))
                {
                    columnIndexes.Add(reader.GetName(x), x);
                }
            }

            if (columnIndexes.Count > 0)
            {
                Logger.Instance.Debug($"Column indexes created for retrieving required fields from table: {TransferDsnName}.{tableName}.");
            }
            else
            {
                Logger.Instance.Warning($"No column indexes created for retrieving required fields from table: {TransferDsnName}.{tableName}.");
            }

            List<MappingField> rows = new List<MappingField>();
            try
            {
                int count = 0;
                while (reader.Read())
                {
                    var row = new MappingField();
                    row.TableName = reader[columnIndexes["TableName"]].ToString();
                    row.FieldName = reader[columnIndexes["FieldName"]].ToString();
                    row.MappingName = reader[columnIndexes["MappingName"]].ToString();
                    row.DisplayName = reader[columnIndexes["DisplayName"]].ToString();
                    row.Description = reader[columnIndexes["Description"]].ToString();
                    row.DataType = reader[columnIndexes["DataType"]].ToString();
                    row.Required = (bool) reader[columnIndexes["Required"]];
                    count++;
                    rows.Add(row);
                }

                if (rows.Count > 0)
                {
                    Logger.Instance.Debug($"{rows.Count} required fields were returned for table: {TransferDsnName}.{tableName}");
                }
                else
                {
                    Logger.Instance.Warning($"No required fields were found for table: {TransferDsnName}.{tableName}");
                }

                return rows;
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }
        }

        public List<MappingField> GetUnmappedFields(string tableName)
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            var command = new OdbcCommand($"SELECT `TableName`, `FieldName`, `MappingName`, `DisplayName`, `Description`, `DataType`, `Required` " +
                                          $"FROM `FieldMappings` " +
                                          $"WHERE `TableName` = '{tableName}' " +
                                            $"AND (`MappingName` = '' OR `MappingName` IS NULL)", _connection);
            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return new List<MappingField>();
            }

            OdbcDataReader reader = command.ExecuteReader();
            Dictionary<string, int> columnIndexes = new Dictionary<string, int>();

            for (int x = 0; x < reader.FieldCount; x++)
            {
                if (!string.IsNullOrEmpty(reader.GetName(x)))
                {
                    columnIndexes.Add(reader.GetName(x), x);
                }
            }

            if (columnIndexes.Count > 0)
            {
                Logger.Instance.Debug($"Column indexes created for retrieving unmapped fields from table: {TransferDsnName}.{tableName}.");
            }
            else
            {
                Logger.Instance.Warning($"No column indexes created for retrieving unmapped fields from table: {TransferDsnName}.{tableName}.");
            }

            List<MappingField> rows = new List<MappingField>();
            try
            {
                while (reader.Read())
                {
                    var row = new MappingField();
                    row.TableName = reader[columnIndexes["TableName"]].ToString();
                    row.FieldName = reader[columnIndexes["FieldName"]].ToString();
                    row.MappingName = reader[columnIndexes["MappingName"]].ToString();
                    row.DisplayName = reader[columnIndexes["DisplayName"]].ToString();
                    row.Description = reader[columnIndexes["Description"]].ToString();
                    row.DataType = reader[columnIndexes["DataType"]].ToString();
                    row.Required = (bool)reader[columnIndexes["Required"]];
                    rows.Add(row);
                }

                Logger.Instance.Debug($"{rows.Count} unmapped fields were returned for table: {TransferDsnName}.{tableName}");

                return rows;
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }
        }

        public List<MappingField> GetMappedFields(string tableName)
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            var command = new OdbcCommand($"SELECT `TableName`, `FieldName`, `MappingName`, `DisplayName`, `Description`, `DataType`, `Required`, `Updatable` " +
                                          $"FROM `FieldMappings` " +
                                          $"WHERE `TableName` = '{tableName}' " +
                                          $"AND `MappingName` IS NOT NULL", _connection);

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return new List<MappingField>();
            }

            OdbcDataReader reader = command.ExecuteReader();
            Dictionary<string, int> columnIndexes = new Dictionary<string, int>();

            for (int x = 0; x < reader.FieldCount; x++)
            {
                if (!string.IsNullOrEmpty(reader.GetName(x)))
                {
                    columnIndexes.Add(reader.GetName(x), x);
                }
            }

            if (columnIndexes.Count > 0)
            {
                Logger.Instance.Debug($"Column indexes created for retrieving unmapped fields from table: {TransferDsnName}.{tableName}.");
            }
            else
            {
                Logger.Instance.Warning($"No column indexes created for retrieving unmapped fields from table: {TransferDsnName}.{tableName}.");
            }

            List<MappingField> rows = new List<MappingField>();
            try
            {
                while (reader.Read())
                {
                    var row = new MappingField();
                    row.TableName = reader[columnIndexes["TableName"]].ToString();
                    row.FieldName = reader[columnIndexes["FieldName"]].ToString();
                    row.MappingName = reader[columnIndexes["MappingName"]].ToString();
                    row.DisplayName = reader[columnIndexes["DisplayName"]].ToString();
                    row.Description = reader[columnIndexes["Description"]].ToString();
                    row.DataType = reader[columnIndexes["DataType"]].ToString();
                    row.Required = (bool)reader[columnIndexes["Required"]];
                    row.Updatable = (bool)reader[columnIndexes["Updatable"]];
                    rows.Add(row);
                }

                Logger.Instance.Debug($"{rows.Count} unmapped fields were returned for table: {TransferDsnName}.{tableName}");

                return rows;
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }
        }

        public List<string> GetColumns(string tableName, string dsnName = "")
        {
            if (string.IsNullOrEmpty(dsnName))
            {
                dsnName = Settings.DsnName;
            }
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={dsnName}");
            Credentials creds = DsnCreds.GetDsnCreds(dsnName);
            if (creds != null)
            {
                if (!string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
                {
                    _connection.ConnectionString = $"DSN={dsnName};Uid={creds.Username};Pwd={creds.Password}";
                }
            }

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return new List<string>();
            }

            try
            {
                var columns = _connection.GetSchema("Columns");
                List<string> columnNames = new List<string>();

                foreach (DataRow row in columns.Rows)
                {
                    if (row["TABLE_NAME"].ToString().Equals(tableName))
                    {
                        if (!row["COLUMN_NAME"].ToString().ToUpper().Equals("ID"))
                        {
                            columnNames.Add(row["COLUMN_NAME"].ToString());
                        }
                    }
                }

                if (!columnNames.Any())
                {
                    Logger.Instance.Warning($"No column names found for {dsnName}.{tableName}.");
                }

                return columnNames;
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"Unable to retrieve column names from {dsnName}.{tableName}: {e.Message}");
            }
            finally
            {
                ConnectionInstance.CloseConnection($"DSN={dsnName}");
            }

            return new List<string>();
        }

        public bool MigrateData(string tableName, bool nuke = true)
        {
            if (ValidateRequiredFields(tableName))
            {
                string tableMappingName = GetTableMapping(tableName);

                List<MappingField> fromColumns = GetMappedFields(tableName);
                List<string> toColumnNames = new List<string>();
                List<string> fromColumnNames = new List<string>();

                foreach (MappingField fromColumn in fromColumns)
                {
                    toColumnNames.Add(fromColumn.FieldName);
                    fromColumnNames.Add(fromColumn.MappingName);
                }

                string chainedToColumnNames = string.Join(",", toColumnNames);

                string chainedFromColumnNames = string.Join(",", fromColumnNames);

                var _connection = ConnectionInstance.Instance.GetConnection($"DSN={DsnName}");
                Credentials creds = DsnCreds.GetDsnCreds(DsnName);
                if (creds != null)
                {
                    if (!string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
                    {
                        _connection.ConnectionString = $"DSN={DsnName};Uid={creds.Username};Pwd={creds.Password}";
                    }
                }

                string sql = $"SELECT {chainedFromColumnNames} FROM {tableMappingName}";
                var command = new OdbcCommand(sql)
                {
                    Connection = _connection
                };

                try
                {
                    _connection.Open();

                    OdbcDataReader reader = command.ExecuteReader();
                    var columnIndexes = new List<KeyValuePair<string, int>>();
                    try
                    {
                        for (int x = 0; x < reader.FieldCount; x++)
                        {
                            string fieldName = GetMappingField(tableName, reader.GetName(x));
                            if (!string.IsNullOrEmpty(fieldName))
                            {
                                var columnIndex = new KeyValuePair<string, int>(fieldName, x);
                                columnIndexes.Add(columnIndex);
                            }
                        }

                        if (columnIndexes.Count > 0)
                        {
                            Logger.Instance.Debug($"Column indexes created for migrating data to {Settings.DsnName}.{tableName}.");
                        }
                        else
                        {
                            Logger.Instance.Warning($"No column indexes were created for migrating data to {Settings.DsnName}.{tableName}.");
                        }

                        var _conn = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
                        var nukeCommand = new OdbcCommand($"DELETE * FROM {tableName}")
                        {
                            Connection = _conn
                        };

                        try
                        {
                            if (nuke)
                            {
                                _conn.Open();
                                nukeCommand.ExecuteNonQuery();
                                Logger.Instance.Debug($"{Settings.DsnName}.{tableName} nuked.");
                            }
                        }
                        catch (OdbcException e)
                        {
                            Logger.Instance.Error($"Failed to nuke {Settings.DsnName}{tableName}: {e.Message}");
                        }

                        var rowCount = 0;
                        while (reader.Read())
                        {
                            if (columnIndexes.Count == toColumnNames.Count)
                            {
                                List<string> readerColumns = new List<string>();

                                foreach (KeyValuePair<string, int> colIndex in columnIndexes)
                                {
                                    string text = reader[colIndex.Value].ToString();
                                    text = text.Replace("'", "''").Replace("\"", "\\\"");
                                    string original = text;
                                    text = SanitizeField(tableName, colIndex.Key, text, _conn);
                                    if (!string.IsNullOrEmpty(text) && Settings.GetSanitizeLog() && original != text)
                                    {
                                        File.AppendAllText(@"log-sanitized.txt", $"{DateTime.Now} {tableName}:{colIndex} [{original} -> {text}] {Environment.NewLine}");
                                    }
                                    if (string.IsNullOrEmpty(text))
                                    {
                                        text = "null";
                                        readerColumns.Add(text);
                                    }
                                    else
                                    {
                                        readerColumns.Add("'" + text + "'");
                                    }
                                }

                                string readerColumnValues = string.Join(",", readerColumns);
                                string stmt = $"INSERT INTO {tableName} ({chainedToColumnNames}) VALUES ({readerColumnValues})";

                                var comm = new OdbcCommand(stmt)
                                {
                                    Connection = _conn
                                };

                                try
                                {
                                    comm.ExecuteNonQuery();
                                    rowCount++;
                                }
                                catch (OdbcException e)
                                {
                                    Logger.Instance.Error($"Failed to insert record into {Settings.DsnName}.{tableName}: {e.Message} \n\nCommand: {comm.CommandText}");
                                }

                            }
                        }

                        if (rowCount > 0)
                        {
                            Logger.Instance.Debug($"{rowCount} records inserted into {Settings.DsnName}.{tableName}.");
                        }
                        else
                        {
                            Logger.Instance.Warning($"No records inserted into {Settings.DsnName}.{tableName}.");
                        }

                        return true;
                    }
                    finally
                    {
                        reader.Close();
                        ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
                    }
                }
                catch (OdbcException e)
                {
                    Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                    return false;
                }
                finally
                {
                    ConnectionInstance.CloseConnection($"DSN={DsnName}");
                    ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
                }
            }

            _validFields = false;
            return false;
        }

        /// <summary>
        /// Pushes data from the Access database into 
        /// </summary>
        /// <param name="tableName">Name of the Access table to push</param>
        /// <param name="tableKey">Name of the Field Name that maps to the primary key of the mapped table</param>
        /// <param name="clearProduction">Whether to clear out the production table before populating it.</param>
        /// <returns>True if successful</returns>
        public bool PushData(string tableName, string tableKey, bool clearProduction = false)
        {
            if (ValidateRequiredFields(tableName)) {
                string tableMappingName = GetTableMapping(tableName);

                List<MappingField> fromColumns = GetMappedFields(tableName);
                List<MappingField> toColumns = new List<MappingField>();
                List<string> fromColumnNames = new List<string>();

                foreach (MappingField fromColumn in fromColumns) {
                    toColumns.Add(fromColumn);
                    fromColumnNames.Add(fromColumn.FieldName);
                }

                string chainedToColumnNames = string.Join(",", toColumns.Select(c => c.MappingName));

                string chainedFromColumnNames = string.Join(",", fromColumnNames);

                if (clearProduction) {
                    var _conn = ConnectionInstance.Instance.GetConnection($"DSN={DsnName}");
                    Credentials creds = DsnCreds.GetDsnCreds(DsnName);
                    if (creds != null)
                    {
                        if (!string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
                        {
                            _conn.ConnectionString = $"DSN={DsnName};Uid={creds.Username};Pwd={creds.Password}";
                        }
                    }
                    var clearCommand = new OdbcCommand($"DELETE FROM {tableMappingName}") {
                        Connection = _conn
                    };

                    try {
                        _conn.Open();
                        clearCommand.ExecuteNonQuery();
                        Logger.Instance.Debug($"{DsnName}.{tableMappingName} cleared.");
                    } catch (OdbcException e) {
                        Logger.Instance.Error($"Failed to clear {DsnName}.{tableMappingName}: {e.Message}");
                    } finally {
                        ConnectionInstance.CloseConnection($"DSN={DsnName}");
                    }
                }

                var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");

                string sql = $"SELECT {chainedFromColumnNames} FROM {tableName}";
                var command = new OdbcCommand(sql) {
                    Connection = _connection
                };
                _connection.Open();
                OdbcDataReader reader = command.ExecuteReader();
                Dictionary<string, int> columnIndexes = new Dictionary<string, int>();
                try {
                    for (int x = 0; x < reader.FieldCount; x++) {
                        string fieldName = GetFieldMapping(tableName, reader.GetName(x));
                        if (!string.IsNullOrEmpty(fieldName)) {
                            columnIndexes.Add(fieldName, x);
                        }
                    }

                    if (columnIndexes.Count > 0) {
                        Logger.Instance.Debug($"Column indexes created for migrating data to {DsnName}.{tableMappingName}.");
                    } else {
                        Logger.Instance.Warning($"No column indexes were created for migrating data to {DsnName}.{tableMappingName}.");
                    }

                    var _conn = ConnectionInstance.Instance.GetConnection($"DSN={DsnName}");
                    Credentials creds = DsnCreds.GetDsnCreds(DsnName);
                    if (creds != null)
                    {
                        if (!string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
                        {
                            _conn.ConnectionString = $"DSN={DsnName};Uid={creds.Username};Pwd={creds.Password}";
                        }
                    }

                    // we need to ensure that this record doesn't exist in the production db already
                    var mappedKey = GetFieldMapping(tableName, tableKey);
                    string existsSql = null;

                    var rowCount = 0;
                    while (reader.Read()) {
                        if (columnIndexes.Count == toColumns.Count) {                            
                            var readerColumns = new List<string>();
                            foreach (var col in toColumns) {                                
                                var text = ValueOrNull(reader[columnIndexes[col.MappingName]].ToString(), col.DataType);
                                readerColumns.Add(text);
                                if (col.MappingName == mappedKey) {
                                    existsSql = $"SELECT * FROM {tableMappingName} WHERE {mappedKey} = {text}";
                                }
                            }
                            string readerColumnValues = string.Join(",", readerColumns);
                            string stmt = $"INSERT INTO {tableMappingName} ({chainedToColumnNames}) VALUES ({readerColumnValues})";

                            var comm = new OdbcCommand(stmt) {
                                Connection = _conn
                            };

                            if (!string.IsNullOrEmpty(existsSql)) {
                                var existsCommand = new OdbcCommand(existsSql, _conn);
                                var existsReader = existsCommand.ExecuteReader();
                                if (existsReader.Read()) {
                                    // there's already a record with this key. move along...
                                    continue;
                                }
                            }
                            try {
                                _conn.Open();
                                comm.ExecuteNonQuery();
                                rowCount++;
                            } catch (OdbcException e) {
                                Logger.Instance.Error($"Failed to insert record into {Settings.DsnName}.{tableName}: {e.Message} \n\nCommand: {comm.CommandText}");
                            } finally {
                                ConnectionInstance.CloseConnection($"DSN={DsnName}");
                            }

                        }
                    }

                    if (rowCount > 0) {
                        Logger.Instance.Debug($"{rowCount} records inserted into {DsnName}.{tableMappingName}.");
                    } else {
                        Logger.Instance.Warning($"No records inserted into {DsnName}.{tableMappingName}.");
                    }

                    return true;
                } finally {
                    reader.Close();
                    ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
                }
            }

            _validPushFields = false;
            return false;
        }

        /// <summary>
        /// Updates updatable fields in the Access database from the production data
        /// </summary>
        /// <param name="tableName">Name of the Access table to push</param>
        /// <param name="tableKey">Name of the Field Name that maps to the primary key of the mapped table</param>
        /// <returns>True if successful</returns>
        public bool UpdateData(string tableName, string tableKey)
        {
            if (ValidateRequiredFields(tableName)) {
                var tableMappingName = GetTableMapping(tableName);
                var keyMappingName = GetFieldMapping(tableName, tableKey);

                var updatableColumns = GetMappedFields(tableName).Where(c => c.Updatable).ToList();
                var fromColumnNames = new List<string>();

                foreach (var fromColumn in updatableColumns) {
                    fromColumnNames.Add(fromColumn.MappingName);
                }

                string chainedFromColumnNames = string.Join(",", fromColumnNames.Select(c => $"{c}"));

                var _connection = ConnectionInstance.Instance.GetConnection($"DSN={DsnName}");
                Credentials creds = DsnCreds.GetDsnCreds(DsnName);
                if (creds != null)
                {
                    if (!string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
                    {
                        _connection.ConnectionString = $"DSN={DsnName};Uid={creds.Username};Pwd={creds.Password}";
                    }
                }

                string sql = $"SELECT {keyMappingName}, {chainedFromColumnNames} FROM {tableMappingName}";
                var command = new OdbcCommand(sql) {
                    Connection = _connection
                };

                try
                {
                    _connection.Open();
                }
                catch (OdbcException e)
                {
                    Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                    return false;
                }

                OdbcDataReader reader = command.ExecuteReader();
                Dictionary<string, int> columnIndexes = new Dictionary<string, int>();
                try {
                    for (int x = 0; x < reader.FieldCount; x++) {
                        string fieldName = GetMappingField(tableName, reader.GetName(x));
                        if (!string.IsNullOrEmpty(fieldName)) {
                            columnIndexes.Add(fieldName, x);
                        }
                    }

                    if (columnIndexes.Count > 0) {
                        Logger.Instance.Debug($"Column indexes created for migrating data to {TransferDsnName}.{tableName}.");
                    } else {
                        Logger.Instance.Warning($"No column indexes were created for migrating data to {TransferDsnName}.{tableName}.");
                    }

                    var _conn = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");

                    var rowCount = 0;
                    while (reader.Read()) {
                        if (columnIndexes.Count == updatableColumns.Count + 1) {
                            List<string> readerColumns = new List<string>();
                            foreach (var col in updatableColumns) {
                                var colName = col.FieldName;
                                string value = ValueOrNull(reader[columnIndexes[colName]].ToString(), col.DataType);
                                if (value != "null") {
                                    readerColumns.Add($"{colName} = {value}");
                                }
                            }
                            if (!readerColumns.Any()) {
                                continue;
                            }

                            var keyValue = ValueOrNull(reader[columnIndexes[tableKey]].ToString(), "Number");
                            string readerColumnValues = string.Join(",", readerColumns);
                            string stmt = $"UPDATE {tableName} SET {readerColumnValues} WHERE {tableKey} = {keyValue}";

                            var comm = new OdbcCommand(stmt) {
                                Connection = _conn
                            };

                            try
                            {
                                _conn.Open();
                            }
                            catch (OdbcException e)
                            {
                                Logger.Instance.Error($"Failed to connect using connection string {_conn.ConnectionString}.");
                                return false;
                            }

                            try {
                                comm.ExecuteNonQuery();
                                rowCount++;
                            } catch (OdbcException e) {
                                Logger.Instance.Error($"Failed to update record in {DsnName}.{tableMappingName}: {e.Message} \n\nCommand: {comm.CommandText}");
                            } finally {
                                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
                            }

                        }
                    }

                    if (rowCount > 0) {
                        Logger.Instance.Debug($"{rowCount} records updated in {TransferDsnName}.{tableName}.");
                    } else {
                        Logger.Instance.Warning($"No records updated in {TransferDsnName}.{tableName}.");
                    }

                    return true;
                } finally {
                    reader.Close();
                    ConnectionInstance.CloseConnection($"DSN={DsnName}");
                }
            }

            _validUpdateFields = false;
            return false;
        }

        private static string ValueOrNull(string value, string fieldType = "Short Text")
        {
            var delimiter = (fieldType == "Number" || fieldType == "Decimal") ? "" : "'";
            return string.IsNullOrEmpty(value) ? "null" : $"{delimiter}{value.Replace("'", "''").Replace("\"", "\\\"")}{delimiter}";
        }

        private string SanitizeField(string tableName, string field, string text, OdbcConnection connection)
        {
            string sanitizeNumbersOnly = GetFieldProperty(tableName, field, "SanitizeNumbersOnly", connection);
            string sanitizeEmail = GetFieldProperty(tableName, field, "SanitizeEmail", connection);
            string sanitizePrice = GetFieldProperty(tableName, field, "SanitizePrice", connection);
            string sanitizeAlphanumeric = GetFieldProperty(tableName, field, "SanitizeAlphaNumeric", connection);
            string sanitizeUniqueId = GetFieldProperty(tableName, field, "SanitizeUniqueId", connection);

            if (!string.IsNullOrEmpty(text))
            {
                if (Convert.ToBoolean(sanitizeNumbersOnly))
                {
                    text = Tools.CleanStringOfNonDigits(text);
                }
                
                if (Convert.ToBoolean(sanitizeEmail))
                {
                    text = Tools.CleanEmail(text);
                }
                
                if (Convert.ToBoolean(sanitizePrice))
                {
                    text = Tools.FormatDecimal(text);
                }
                
                if (Convert.ToBoolean(sanitizeAlphanumeric))
                {
                    text = Tools.CleanAlphanumeric(text);
                }
                
                if (Convert.ToBoolean(sanitizeUniqueId))
                {
                    text = Tools.CleanUniqueId(text);
                }

                return !string.IsNullOrEmpty(text) ? Tools.CleanStringForSql(text) : "";
            }

            return text;
        }

        private bool ValidateRequiredFields(string tableName)
        {
            var _connection = ConnectionInstance.Instance.GetConnection($"DSN={TransferDsnName}");
            var command = new OdbcCommand($"SELECT * FROM `FieldMappings` " +
                                          $"WHERE `TableName` = '{tableName}' " +
                                          $"AND (`MappingName` = '' OR `MappingName` IS NULL)" +
                                          $"AND `Required` = TRUE", _connection);

            try
            {
                _connection.Open();
            }
            catch (OdbcException e)
            {
                Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                return false;
            }

            OdbcDataReader reader = command.ExecuteReader();
            try
            {
                if (reader.HasRows)
                {
                    return false;
                }
            }
            finally
            {
                reader.Close();
                ConnectionInstance.CloseConnection($"DSN={TransferDsnName}");
            }

            return true;
        }

        public DataTable PreviewMapping(string tableName)
        {
            string tableMappingName = GetTableMapping(tableName);
            List<string> columns = GetColumns(tableMappingName, DsnName);
            List<string> mappingColumns = new List<string>();

            foreach (string column in columns)
            {
                string field = GetMappingField(tableName, column);
                string fieldDisplayName = GetFieldProperty(tableName, field, "DisplayName");
                string combinedColumnName = "`" + column + "`" + " AS \"" + fieldDisplayName + " : " + column + "\""; 
                if (!string.IsNullOrEmpty(field))
                {
                    mappingColumns.Add(combinedColumnName);
                }
            }

            string columnNames = string.Join(",", mappingColumns);

            if (mappingColumns.Count > 0)
            {
                var _connection = ConnectionInstance.Instance.GetConnection($"DSN={DsnName}");
                Credentials creds = DsnCreds.GetDsnCreds(DsnName);
                if (creds != null)
                {
                    if (!string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
                    {
                        _connection.ConnectionString = $"DSN={DsnName};Uid={creds.Username};Pwd={creds.Password}";
                    }
                }

                string query = $"SELECT {columnNames} FROM `{tableMappingName}`";
                var command = new OdbcCommand(query)
                {
                    Connection = _connection
                };

                try
                {
                    _connection.Open();
                }
                catch (OdbcException e)
                {
                    Logger.Instance.Error($"Failed to connect using connection string {_connection.ConnectionString}.");
                    return new DataTable();
                }

                OdbcDataAdapter adapter = new OdbcDataAdapter(query, _connection);
                DataTable table = new DataTable();

                var items = GetUnmappedFields(tableName);
                foreach (MappingField item in items)
                {
                    table.Columns.Add(item.DisplayName);
                }

                try
                {
                    adapter.Fill(table);
                    int rowCount = table.Rows.Count;
                    for (int i = 20; i < rowCount; i++)
                    {
                        table.Rows.RemoveAt(i);
                    }

                    table.DefaultView.AllowDelete = false;
                    table.DefaultView.AllowEdit = false;
                    table.DefaultView.AllowNew = false;

                    return table;
                }
                catch (Exception e)
                {
                    Logger.Instance.Error($"An error occured while populating the preview table: {e.Message}");
                }
                finally
                {
                    ConnectionInstance.CloseConnection($"DSN={DsnName}");
                }
            }

            DataTable previewTable = new DataTable();

            var previewItems = GetUnmappedFields(tableName);
            foreach (MappingField item in previewItems)
            {
                previewTable.Columns.Add(item.DisplayName);
            }

            return previewTable;
        }
    }
}
