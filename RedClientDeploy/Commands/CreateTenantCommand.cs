using AzureFromTheTrenches.Commanding.Abstractions;
using Newtonsoft.Json;
using RedClientDeploy.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedClientDeploy.Commands
{
    public class CreateTenantCommand : ICommand<Response>
    {
        [JsonProperty("tenant_ovc_id")]
        public string OvcId { get; set; }
        [JsonProperty("tenant_name")]
        public string Name { get; set; }

        [JsonProperty("tenant_admin_password")]
        public string Password { get; set; }
    }
}
