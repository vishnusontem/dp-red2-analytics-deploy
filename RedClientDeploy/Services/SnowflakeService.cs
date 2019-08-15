using Dapper;
using RedClientDeploy.Models;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RedClientDeploy.Services
{
    public class SnowflakeService
    {
        private SnowflakeDbConnection _connection;
        public SnowflakeTenant Tenant { get; private set; }

        public SnowflakeService()
        {
            _connection = new SnowflakeDbConnection
            {
                ConnectionString = @"account=jll;host=jll.east-us-2.azure.snowflakecomputing.com;ROLE=DEVOPS;password=Pipeline1$;user=PIPELINE",
                
            };           

        }
        public async Task<SnowflakeTenant> GetReaderAsync(string clientName)
        {
            SnowflakeTenant t = new SnowflakeTenant();
            //authenticator=https://<your_okta_account_name>.okta.com

            if (_connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
            }
            await _connection.OpenAsync();

            var x = await _connection.QueryAsync<Share>($"show managed accounts like '{clientName}_READER'", null, null, 10, System.Data.CommandType.Text);

            var share = x.Where(i => i.name.StartsWith(clientName)).First();

            t.Locator = share.locator;
            t.OvcId = share.comment.Trim();
            t.Name = clientName;
            _connection.Close();

            Tenant = t;

            return t;

        }

        public async Task<string> DdlAsync(Connection parmList, string commandText, string client)
        {
            var sb = new StringBuilder();

            if (commandText.ToLower() == "show shares")
            {
                var tenant = await GetReaderAsync(client);

                sb.AppendLine(tenant.Locator);
            }
            else
            {

                try
                {

                    _connection.ConnectionString = BuildConnectionString(parmList);

                    using (DbCommand cmd = new SnowflakeDbCommand())
                    {

                        await _connection.OpenAsync();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandTimeout = 0;
                        cmd.CommandText = commandText;
                        cmd.Connection = _connection;

                        using (var res = await cmd.ExecuteReaderAsync())
                        {
                            while (res.Read())
                            {
                                sb.AppendLine(res.GetString(0));
                            }
                        }
                    }

                    _connection.Close();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return sb.ToString();

        }


        public async Task<string> CreateReaderAsync(Connection parmList, string commandText, string clientName)
        {
            SnowflakeTenant tenant = new SnowflakeTenant();

            _connection.ConnectionString = BuildConnectionString(parmList);

            await _connection.OpenAsync();

            using (DbCommand cmd = new SnowflakeDbCommand())
            {
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandTimeout = 0;
                cmd.CommandText = commandText;
                cmd.Connection = _connection;

                try
                {
                    _ = await cmd.ExecuteScalarAsync();
                    System.Threading.Thread.Sleep(2000);

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            _connection.Close();

            try
            {
                var t = await GetReaderAsync(clientName);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            

            return Tenant.Locator;

        }


        private string BuildConnectionString(Connection parmList)
        {

            var csItems = ClassToDictionary(parmList);

            SnowflakeDbConnectionStringBuilder csb = new SnowflakeDbConnectionStringBuilder();

            foreach (KeyValuePair<string, string> item in csItems)
            {
                csb.Add(item.Key, item.Value);
            }

            return csb.ConnectionString;
        }

        public static Dictionary<string, string> ClassToDictionary(object o)
        {
            PropertyInfo[] infos = o.GetType().GetProperties();

            Dictionary<string, string> dix = new Dictionary<string, string>();

            foreach (PropertyInfo info in infos)
            {
                //if (info.Name.ToLower() != "db")
                //{
                dix.Add(info.Name.ToLower(), info.GetValue(o, null).ToString());
                //}

            }

            return dix;
        }
    }

    public partial class Share
    {

        public string kind { get; set; }
        public string name { get; set; }
        public string cloud { get; set; }
        public string region { get; set; }
        public string locator { get; set; }
        public object created_on { get; set; }
        public string url { get; set; }

        public bool is_reader { get; set; }
        //public string database_name { get; set; }
        //public string to { get; set; }
        //public string owner { get; set; }
        public string comment { get; set; }






    }
}
