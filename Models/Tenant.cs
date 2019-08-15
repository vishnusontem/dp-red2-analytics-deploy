using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedClientDeploy.Models
{
    public class Tenant
    {
        public SnowflakeTenant Snowflake { get; set; }
        public TableauTenant Tableau { get; set; }
        public string Message { get; set; }

    }

     public class SnowflakeTenant
    {
        [JsonProperty("tenant_name")]
        public string Name { get; set; }
        [JsonProperty("tenant_description")]
        public string Description { get; set; }
        [JsonProperty("tenant_ovc_id")]
        public string OvcId { get; set; }
        [JsonProperty("tenant_snowflake_locator")]
        public string Locator { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
    }

    public class TableauTenant
    {
        [JsonProperty("tenant_tableau_site_id")]
        public string SiteId { get; set; }
        [JsonProperty("tenant_tableau_project_id")]
        public string ProjectId { get; set; }
    }
}
