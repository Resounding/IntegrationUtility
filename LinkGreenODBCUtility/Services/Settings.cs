﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataTransfer.AccessDatabase;
using LinkGreen.Applications.Common;
using LinkGreen.Applications.Common.Model;

namespace LinkGreenODBCUtility
{
    public static class Settings
    {
        public static string ConnectViaDsnName = "LinkGreenDataTransfer";
        public static bool DebugMode = false;
        public static readonly string ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=LinkGreenDataTransfer.mdb;Persist Security Info=True";


        public static void Init()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                string encryptionKey = config.AppSettings.Settings["EncryptionKey"].Value;
                if (string.IsNullOrEmpty(encryptionKey))
                {
                    Guid g = Guid.NewGuid();
                    string GuidString = Convert.ToBase64String(g.ToByteArray());
                    GuidString = GuidString.Replace("=", "");
                    GuidString = GuidString.Replace("+", "");

                    config.AppSettings.Settings["EncryptionKey"].Value = GuidString;
                    config.Save(ConfigurationSaveMode.Modified);
                }

                if (GetSandboxMode())
                {
                    config.AppSettings.Settings["BaseUrl"].Value = "http://dev.linkgreen.ca/";
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"An error occured while initializing the app config: {e.Message}");
            }
        }

        public static bool TryConnect()
        {
            //var _connection = ConnectionInstance.Instance.GetConnection($"DSN={DsnName}");
            using (var cInstance = new OleDbConnectionInstance(ConnectionString))
            {
                var connection = cInstance.GetConnection();
                try
                {
                    connection.Open();
                }
                catch (OleDbException e)
                {
                    Logger.Instance.Error($"Could not connect to the DSN {ConnectionString}: {e.Message}");
                    return false;
                }
                finally
                {
                    cInstance.CloseConnection();
                }
            }

            return true;
        }

        public static string GetApiKey()
        {
            using (var cInstance = new OleDbConnectionInstance(ConnectionString))
            {
                var _connection = cInstance.GetConnection();
                var command = new OleDbCommand($"SELECT `ApiKey` FROM `Settings` WHERE `Id` = 1", _connection);
                _connection.Open();
                try
                {
                    var key = command.ExecuteScalar();
                    return key?.ToString() ?? "";
                }
                catch (Exception e)
                {
                    Logger.Instance.Error($"An error occurred while retrieving the ApiKey: {e.Message}");
                }
                finally
                {
                    cInstance.CloseConnection();
                }
            }

            return null;
        }

        public static void SaveApiKey(string apiKey)
        {
            using (var cInstance = new OleDbConnectionInstance(ConnectionString))
            {
                var _connection = cInstance.GetConnection();
                var command = new OleDbCommand($"UPDATE `Settings` SET `ApiKey` = '{apiKey}' WHERE `ID` = 1",
                    _connection);

                _connection.Open();
                try
                {
                    command.ExecuteNonQuery();

                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["ApiKey"].Value = apiKey;
                    config.Save(ConfigurationSaveMode.Modified);

                    Logger.Instance.Debug($"ApiKey saved: '{apiKey}'");
                }
                catch (Exception e)
                {
                    Logger.Instance.Error($"An error occured while updating the ApiKey.");
                }
                finally
                {
                    cInstance.CloseConnection();
                }
            }
        }

        public static string GetInstallationId()
        {
            using (var cInstance = new OleDbConnectionInstance(ConnectionString))
            {
                var _connection = cInstance.GetConnection();
                var command = new OleDbCommand($"SELECT `InstallationId` FROM `Settings` WHERE `Id` = 1", _connection);
                _connection.Open();
                OleDbDataReader reader = command.ExecuteReader();
                try
                {
                    string installationId = null;
                    while (reader.Read())
                    {
                        installationId = reader[0].ToString();
                    }

                    if (string.IsNullOrEmpty(installationId))
                    {
                        var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        installationId = !string.IsNullOrEmpty(config.AppSettings.Settings["InstallationId"].Value)
                            ? config.AppSettings.Settings["InstallationId"].Value
                            : null;
                    }

                    return installationId;
                }
                catch (Exception e)
                {
                    Logger.Instance.Error($"An error occured while retrieving the InstallationId: {e.Message}");
                }
                finally
                {
                    reader.Close();
                    cInstance.CloseConnection();
                }
            }

            return null;
        }

        public static void SaveInstallationId()
        {
            var guid = Guid.NewGuid();
            using (var cInstance = new OleDbConnectionInstance(ConnectionString))
            {
                var _connection = cInstance.GetConnection();
                var command = new OleDbCommand($"UPDATE `Settings` SET `InstallationId` = '{guid}' WHERE `ID` = 1", _connection);

                _connection.Open();
                try
                {
                    command.ExecuteNonQuery();

                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["InstallationId"].Value = guid.ToString();
                    config.Save(ConfigurationSaveMode.Modified);

                    Logger.Instance.Debug(
                        $"InstallationId set to: {config.AppSettings.Settings["InstallationId"].Value}");
                }
                catch (Exception e)
                {
                    Logger.Instance.Error($"An error occured while creating the InstallationId: {e.Message}");
                }
                finally
                {
                    cInstance.CloseConnection();
                }
            }
        }

        public static bool GetUpdateCategories()
        {
            using (var _connection = new OleDbConnectionInstance(ConnectionString).GetConnection())
            {
                var command = new OleDbCommand($"SELECT `UpdateCategories` FROM `Settings` WHERE `Id` = 1", _connection);
                _connection.Open();
                OleDbDataReader reader = command.ExecuteReader();
                try
                {
                    bool? updateCategories = null;
                    while (reader.Read())
                    {
                        updateCategories = Convert.ToInt32(reader[0].ToString()) == 1;
                    }

                    if (updateCategories == null)
                    {
                        var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        updateCategories = Convert.ToInt32(config.AppSettings.Settings["UpdateCategories"].Value) == 1;
                    }

                    return updateCategories ?? true;
                }
                catch (Exception e)
                {
                    Logger.Instance.Error(
                        $"An error occurred while retrieving the setting UpdateCategories: {e.Message}");
                }
                finally
                {
                    reader.Close();
                    _connection.Close();
                }
            }

            return true;
        }

        public static void SaveUpdateCategories(string updateCategories)
        {

            var _connection = new OleDbConnectionInstance(ConnectionString).GetConnection();
            var command = new OleDbCommand($"UPDATE `Settings` SET `UpdateCategories` = '{updateCategories}' WHERE `ID` = 1")
            {
                Connection = _connection
            };

            _connection.Open();
            try
            {
                command.ExecuteNonQuery();

                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["UpdateCategories"].Value = updateCategories;
                config.Save(ConfigurationSaveMode.Modified);

                Logger.Instance.Debug($"Setting UpdateCategories saved: '{updateCategories}'");
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"An error occured while updating the setting UpdateCategories.");
            }
            finally
            {
                _connection.Close();
            }
        }

        public static bool GetSanitizeLog()
        {
            var _connection = new OleDbConnectionInstance(ConnectionString).GetConnection();
            var command = new OleDbCommand($"SELECT `SanitizeLog` FROM `Settings` WHERE `Id` = 1", _connection);
            _connection.Open();
            OleDbDataReader reader = command.ExecuteReader();
            try
            {
                bool? sanitizeLog = null;
                while (reader.Read())
                {
                    sanitizeLog = Convert.ToInt32(reader[0].ToString()) == 1;
                }

                if (sanitizeLog == null)
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    sanitizeLog = Convert.ToInt32(config.AppSettings.Settings["SanitizeLog"].Value) == 1;
                }

                return sanitizeLog ?? false;
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"An error occurred while retrieving the setting SanitizeLog: {e.Message}");
            }
            finally
            {
                reader.Close();
                _connection.Close();
            }

            return false;
        }

        public static bool GetSandboxMode()
        {
            var _connection = new OleDbConnectionInstance(ConnectionString).GetConnection();
            var command = new OleDbCommand($"SELECT `SandboxMode` FROM `Settings` WHERE `Id` = 1", _connection);
            _connection.Open();
            OleDbDataReader reader = command.ExecuteReader();
            try
            {
                bool? sandboxMode = null;
                while (reader.Read())
                {
                    sandboxMode = Convert.ToInt32(reader[0].ToString()) == 1;
                }

                if (sandboxMode == null)
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    sandboxMode = Convert.ToInt32(config.AppSettings.Settings["SandboxMode"].Value) == 1;
                }

                return sandboxMode ?? true;
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"An error occurred while retrieving the setting SandboxMode: {e.Message}");
            }
            finally
            {
                reader.Close();
                _connection.Close();
            }

            return true;
        }

        public static void SaveSandboxMode(string sandboxMode)
        {
            var _connection = new OleDbConnectionInstance(ConnectionString).GetConnection();
            var command = new OleDbCommand($"UPDATE `Settings` SET `SandboxMode` = '{sandboxMode}' WHERE `ID` = 1")
            {
                Connection = _connection
            };

            _connection.Open();
            try
            {
                command.ExecuteNonQuery();

                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["SandboxMode"].Value = sandboxMode;
                config.Save(ConfigurationSaveMode.Modified);

                Logger.Instance.Debug($"Setting SandboxMode saved: '{sandboxMode}'");
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"An error occured while updating the setting SandboxMode.");
            }
            finally
            {
                _connection.Close();
            }
        }

        public static void SetupUserConfig(string apiKey)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                UserAndCompany user = WebServiceHelper.GetUserInfoByApiKey(apiKey);

                if (user != null)
                {
                    config.AppSettings.Settings["ClientId"].Value = user.CompanyId.ToString();
                    //                    config.AppSettings.Settings["ClientName"].Value = user.CompanyName;
                    config.AppSettings.Settings["UserName"].Value = user.FullName;
                    config.AppSettings.Settings["EmailAddress"].Value = user.EmailAddress;
                    config.AppSettings.Settings["PhoneNumber"].Value = user.FormattedPhone;
                    config.Save(ConfigurationSaveMode.Modified);

                    if (GetInstallationId() == null)
                    {
                        SaveInstallationId();
                    }

                    Logger.Instance.Debug($"Application setup completed using: (CompanyId: {user.CompanyId}, FullName: {user.FullName}, EmailAddress: {user.EmailAddress}, FormattedPhone: {user.FormattedPhone}");
                }
                else
                {
                    Logger.Instance.Error($"Failed to retrieve the user information for apiKey: {apiKey}");
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"An error occured while setting up the user config: {e.Message}");
            }
        }
    }
}
