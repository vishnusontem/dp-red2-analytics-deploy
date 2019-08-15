using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedClientDeploy.Models
{
    public class ProvisioningResult
    {
        [JsonProperty("tenant")]
        public Tenant Client { get; set; }
        [JsonIgnore]
        public string YamlFile { get; set; }
        public List<ErrorDetails> ErrorMessages { get; set; }
        [JsonProperty("steps")]
        public List<ScriptExecution> Steps { get; set; }

    }

    public class ScriptExecution
    {
        public string Command { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }

    }

    public class Step
    {
        public string Ddl { get; set; }
        public string File { get; set; }
        public string Share { get; set; }
        public string User { get; set; }
        public string Message { get; set; }
        public bool Succeeded { get; set; }
    }

    public class YamlJob
    {
        public string Job { get; set; }
        public Connection Connection { get; set; }
        public List<Step> Steps { get; set; }

    }

    public class YamlJobs
    {
        public List<YamlJob> Jobs { get; set; }
    }

    public class Connection
    {
        public string Dbms { get; set; }
        public string Account { get; set; }
        public string User { get; set; }
        public string Host { get; set; }
        public string Role { get; set; }
        public string Password { get; set; }
        public string Db { get; set; } = "SNOWFLAKE";
    }

    public enum ScriptStatus
    {
        Success,
        Failure
    }

    public enum CommandType
    {
        DdlStatement,
        Share,
        File,
        Text,
        Unknown
    }
}
