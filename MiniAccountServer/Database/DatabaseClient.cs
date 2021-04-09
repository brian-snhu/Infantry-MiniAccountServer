using System;
using System.Data.SqlClient;
using System.Xml;
using MiniAccountServer.Models;
using MiniAccountServer.Helpers;
using MiniAccountServer.Helpers.Config;

namespace MiniAccountServer.Database
{
    /// <summary>
    /// Preliminary database client.
    /// </summary>
    public class DatabaseClient
    {
        ConfigSetting _config;
        private SqlConnection _connection;
        private string _connString;

        #region Stored Proc strings

        private String _strLatestIdentity = "SELECT @@IDENTITY";

        private String _strCreateAccount =
            "INSERT INTO account (name, password, ticket, dateCreated, lastAccess, permission, email) VALUES " +
            "(@name, @password, @ticket, @dateCreated, @lastAccess, @permission, @email)";

        private String _strUsernameExists = "SELECT * FROM account WHERE name LIKE @name";

        private String _strAccountValid = "SELECT * FROM account WHERE name LIKE @name AND password LIKE @password";

        private String _strLoginUpdate =
            "UPDATE account SET ticket=@ticket WHERE name LIKE @name;" +
            "UPDATE account SET lastAccess=@time WHERE name LIKE @name;" +
            "UPDATE account SET IPAddress=@ipaddress WHERE name LIKE @name";

        private String _strPasswordUpdate =
            "UPDATE account SET password=@password WHERE name LIKE @name";

        private String _strEmailExists = "SELECT * FROM account WHERE email LIKE @email";

        private String _strCreateToken =
            "INSERT INTO resetToken (account, name, token, expireDate, tokenUsed) VALUES " +
            "(@account, @name, @token, @expireDate, @tokenUsed)";

        private String _strTokenUserExists = "SELECT * FROM resetToken WHERE name LIKE @name";

        private String _strTokenExists = "SELECT * FROM resetToken WHERE token LIKE @token";

        private String _strTokenUpdate = 
            "UPDATE resetToken SET token=@token WHERE name LIKE @name;" +
            "UPDATE resetToken SET expireDate=@expireDate WHERE name LIKE @name;" +
            "UPDATE resetToken SET tokenUsed=@tokenUsed WHERE name LIKE @name";

        private String _strMarkTokenUsed =
            "UPDATE resetToken SET tokenUsed=@tokenUsed WHERE token LIKE @token";

        #endregion

        /// <summary>
        /// Creates our client then opens a connection to our database
        /// </summary>
        public DatabaseClient()
        {
            _connString = "Server=INFANTRY\\SQLEXPRESS;Database=Data;Trusted_Connection=True;";

            _connection = new SqlConnection(_connString);

            _connection.Open();
        }

        /// <summary>
        /// Creates an account in our database and returns the parsed account object
        /// </summary>
        public Account AccountCreate(string username, string password, string ticket, DateTime dateCreated, DateTime lastAccess, int permission, string email)
        {
            if (UsernameExists(username))
            {
                return null;
            }

            var _createAccountCmd = new SqlCommand(_strCreateAccount, _connection);

            _createAccountCmd.Parameters.AddWithValue("@name", username);
            _createAccountCmd.Parameters.AddWithValue("@password", password);
            _createAccountCmd.Parameters.AddWithValue("@ticket", ticket);
            _createAccountCmd.Parameters.AddWithValue("@dateCreated", dateCreated);
            _createAccountCmd.Parameters.AddWithValue("@lastAccess", lastAccess);
            _createAccountCmd.Parameters.AddWithValue("@permission", permission);
            _createAccountCmd.Parameters.AddWithValue("@email", email);

            if(_createAccountCmd.ExecuteNonQuery() != 1)
            {
                return null;
            }

            return new Account
                       {
                           DateCreated = dateCreated,
                           LastAccessed = lastAccess,
                           SessionId = Guid.Parse(ticket),
                           Username = username,
                           Password = password,
                           Permission = permission,
                           Email = email
                       };
        }

        /// <summary>
        /// Does the username exist
        /// </summary>
        public bool UsernameExists(string username)
        {
            var cmd = new SqlCommand(_strUsernameExists, _connection);

            cmd.Parameters.AddWithValue("@name", username);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Does the username and password match our records
        /// </summary>
        public bool IsAccountValid(string username, string password)
        {
            var cmd = new SqlCommand(_strAccountValid, _connection);

            cmd.Parameters.AddWithValue("@name", username);
            cmd.Parameters.AddWithValue("@password", password);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Does the email exist
        /// </summary>
        public bool EmailExists(string email)
        {
            var cmd = new SqlCommand(_strEmailExists, _connection);

            cmd.Parameters.AddWithValue("@email", email);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tries logging in using the given username and pass and returns a parsed account object
        /// </summary>
        public Account AccountLogin(string username, string password, string IPAddress)
        {
            //Update some stuff first, mang
            var update = new SqlCommand(_strLoginUpdate, _connection);

            update.Parameters.AddWithValue("@name", username);
            update.Parameters.AddWithValue("@ticket", Guid.NewGuid().ToString());
            update.Parameters.AddWithValue("@time", DateTime.Now);
            update.Parameters.AddWithValue("@ipaddress", IPAddress);

            update.ExecuteNonQuery();
            
            var cmd = new SqlCommand(_strAccountValid, _connection);

            cmd.Parameters.AddWithValue("@name", username);
            cmd.Parameters.AddWithValue("@password", password);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                    return null;

                reader.Read();

                return new Account
                           {
                               Id = reader.GetInt64(0),
                               Username = reader.GetString(1),
                               Password = reader.GetString(2),
                               SessionId = Guid.Parse(reader.GetString(3)),
                               DateCreated = reader.GetDateTime(4),
                               LastAccessed = reader.GetDateTime(5),
                               Permission = reader.GetInt32(6),
                               Email = reader.GetString(7),
                           };
            }
        }

        /// <summary>
        /// Does our token match?
        /// </summary>
        public bool IsTokenValid(string token)
        {
            //TODO: Add validation checks here

            return false;
        }

        /// <summary>
        /// Does the user exist in our reset token records
        /// </summary>
        public bool TokenUsernameExists(string username)
        {
            //TODO: Add checks for username here

            return false;
        }

        /// <summary>
        /// Was this token already used?
        /// </summary>
        public bool TokenUsed(string token)
        {
            bool tokenUsed = false;

            //TODO: Check to see if token has been used

            return tokenUsed;
        }

        /// <summary>
        /// Did the token expire?
        /// </summary>
        public bool TokenExpired(string token)
        {
            DateTime expireDate;

            //TODO: Make checks for date and time here

            return false;
        }

        /// <summary>
        /// Recovers a users account name
        /// </summary>
        public bool AccountRecover(string email, out string username)
        {
            username = string.Empty;

            //TODO: Add the username check here and output it

            return false;
        }

        /// <summary>
        /// Called upon to create a password reset token, outputs email and generated token
        /// </summary>
        public bool AccountReset(string username, out string[] parameters)
        {
            parameters = null;

            //TODO: Add the reset function for the database here

            return false;
        }

        /// <summary>
        /// Updates a users password after being reset
        /// </summary>
        public bool AccountPasswordUpdate(string token, string password)
        {
            //TODO: Add the update method for the database here            

            return false;
        }

        /// <summary>
        /// Sends a recovery email to a specified email
        /// </summary>
        public bool AccountSendMail(string email, string username, string token)
        {
            if (!System.IO.File.Exists("email.xml"))
                return false;

            //TODO: Make a UTF compliant email and have it sent using
            //infantry's google email account
            return false;
        }

        /// <summary>
        /// Parses the email and only shows a select amount of it for the response
        /// </summary>
        public string EncodeEmail(string email)
        {
            string response = string.Empty;

            //TODO: Parse the email and encode it with *

            return response;
        }
    }
}