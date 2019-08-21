using RedClientDeploy.Models;
using Stubble.Core.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RedClientDeploy.Services
{
    public class TemplateService
    {
        public SnowflakeTenant Tenant { get; private set; }
        public string YamlFile { get; private set; }
        public Dictionary<string, object> variables;
        public TemplateService(string yamlFile, string client, string password, string ovcId, string path = "", string locator = "")
        {
            Tenant = new SnowflakeTenant
            {
                Name = client,
                Description = "Corporate Solutions client: " + client,
                OvcId = ovcId,
                Locator = locator.Length > 0 ? locator : "!!locator!!",
                Password = password

            };

            YamlFile = yamlFile;

            variables = new Dictionary<string, object>
            {
                { "client", Tenant.Name },
                { "password", Tenant.Password },
                { "environment", Environment.GetEnvironmentVariable("Environment") },
                { "locator", Tenant.Locator },
                { "ovcid", Tenant.OvcId }
            };
        }
        public YamlJobs PrepareTemplate()
        {
            ProvisioningResult pr = new ProvisioningResult();

            var deserializer = new DeserializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).Build();

            string yaml = ParseTemplate(File.ReadAllText(YamlFile), variables);

            List<ScriptExecution> tasks = new List<ScriptExecution>();

            return deserializer.Deserialize<YamlJobs>(yaml);


        }

        

        private static string ParseTemplate(string template, Dictionary<string, object> variables)
        {
            var stubbleBuilder = new StubbleBuilder()new StubbleBuilder()
  .Configure(settings =>
  {
      settings. .IgnoreCaseOnKeyLookup = true;
      settings.MaxRecursionDepth = 512;
      settings.AddJsonNet(); // Extension method from extension library
  });

            var stubble = stubbleBuilder.Build();

            try
            {
                return stubble.Render(template, variables);
            }
            catch (Exception ex)
            {

                throw ex;
            }



        }

    }
}
