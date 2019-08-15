using AzureFromTheTrenches.Commanding.Abstractions;
using RedClientDeploy.Models;
using RedClientDeploy.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedClientDeploy.Commands
{
    public class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, Response>
    {
        private readonly TableauService _tableauService;
        private readonly SnowflakeService _snowflakeService;
        private readonly ArtifactService _artifactService;
        private TemplateService _templateService;


        public CreateTenantCommandHandler(TableauService tableauService, SnowflakeService snowflakeService, ArtifactService artifactService)
        {
            _tableauService = tableauService;
            _snowflakeService = snowflakeService;
            _artifactService = artifactService;
        }
        public async Task<Response> ExecuteAsync(CreateTenantCommand command, Response previousResult)
        {
            var execution = new Response();

           string path = await _artifactService.GetScriptsAsync();
            _templateService = new TemplateService(Path.Combine(path, Environment.GetEnvironmentVariable("InternalClientDeployFile")), command.Name, command.Password, command.OvcId);

            if (path.Length > 0)
            {
                try
                {


                    string client = command.Name;

                    string siteName = $"{client} RED 2.0 Site";

                    Tenant tenant = new Tenant();

                    tenant = await _tableauService.NewTableauSiteAsync(command.Name, siteName);

                    if (!string.IsNullOrEmpty(tenant.Message))
                    {
                        execution.TenantDetails = tenant;
                    }
                    else
                    {
                        execution.TenantDetails = tenant;

                        var yamlObject = _templateService.PrepareTemplate();

                        foreach (YamlJob job in from job in yamlObject.Jobs
                                                select job)
                        {
                            var sfTenant = await ProcessInternalAsync(job, client);

                           // scriptExecutions.Add(se);
                        }

                       // pr.Steps = scriptExecutions;
                    }
                    

                   // provisioningResults.Add(pr);

                    /*
                    var r = await t.CreateSiteAsync(logIn, siteName, command.Name);

                    var project = await t.CreateProjectAsync(r);

                    string password = command.Password;//Helper.PasswordStore.GeneratePassword(16, 5);
                    string rootFolder = Path.Combine(path, Environment.GetEnvironmentVariable("WorkingDirectory"));
                    string yamlFile = Path.Combine(path, Environment.GetEnvironmentVariable("InternalClientDeployFile"));

                    var result = await ProcessYaml.ProcessInternalAsync(yamlFile, command.Name, password, command.OvcId);

                    yamlFile = Path.Combine(path, Environment.GetEnvironmentVariable("ReaderClientDeployFile"));

                    var clientMsgs = new List<ScriptExecution>();

                    System.Threading.Thread.Sleep(5000);

                    clientMsgs = await ProcessYaml.ProcessShareAsync(yamlFile, command.Name, password, result.Client.Locator);

                    foreach (var item in clientMsgs)
                    {
                        result.Steps.Add(item);
                    }

                    r1.Add(result);

                    result.Client.SiteId = r.Site.Id;
                    result.Client.ProjectId = project.Id;

    */

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                   // throw ex;
                }

            }

            return execution;
        }

        private async System.Threading.Tasks.Task<SnowflakeTenant> ProcessInternalAsync(YamlJob job, string client)
        {

            ScriptExecution se = new ScriptExecution();

            foreach (Step step in job.Steps)
            {
                job.Connection.User = Environment.GetEnvironmentVariable("SnowflakeUser");
                job.Connection.Password = Environment.GetEnvironmentVariable("SnowflakePassword");

                if (step.Ddl != null)
                {

                    if (step.Ddl.Contains("!!locator!!"))
                    {
                        step.Ddl = step.Ddl.Replace("!!locator!!", _templateService.Tenant.Locator);
                    }

                    Console.WriteLine(step.Ddl);

                    se.Command = step.Ddl;
                    se.Message = await _snowflakeService.DdlAsync(job.Connection, step.Ddl, client);
                    se.Status = ScriptStatus.Success.ToString();

                }
                else if (step.Share != null)
                {
                    Console.WriteLine(step.Share);
                    se.Command = step.Share;
                    se.Type = CommandType.Share.ToString();
                    se.Message = await _snowflakeService.CreateReaderAsync(job.Connection, step.Share, client);
                    se.Status = ScriptStatus.Success.ToString();
                    _templateService.Tenant.Locator = se.Message;
                }
            }

            return null;


                //try
                //{


                //    if (step.Ddl != null)
                //    {

                //        if (step.Ddl.Contains("!!locator!!"))
                //        {
                //            step.Ddl = step.Ddl.Replace("!!locator!!", c.Locator);
                //        }

                //        Console.WriteLine(step.Ddl);

                //        se.Command = step.Ddl;

                //        se.Type = CommandType.DdlStatement.ToString();
                //        se.Message = await sf.DdlAsync(job.Connection, step.Ddl);
                //        se.Status = ScriptStatus.Success.ToString();
                //    }
                //    else if (step.Share != null)
                //    {
                //        Console.WriteLine(step.Share);
                //        se.Command = step.Share;
                //        se.Type = CommandType.Share.ToString();
                //        se.Message = await sf.CreateReaderAsync(job.Connection, step.Share, client);
                //        se.Status = ScriptStatus.Success.ToString();
                //        c.Locator = se.Message;
                //    }
                //    else if (step.File != null)
                //    {
                //        Console.WriteLine(step.File);
                //        se.Command = step.File;
                //        se.Type = CommandType.File.ToString();
                //        string scriptFile = System.IO.Path.Combine(path, step.File);
                //        se.Message = await sf.FileAsync(job.Connection, scriptFile, ovcId, variables);
                //        se.Status = ScriptStatus.Success.ToString();
                //    }
                //    else
                //    {
                //        // ct = CommandType.Unknown;
                //    }
                //}


                //catch (Exception ex)
                //{
                //    se.Message = ex.Message;
                //    se.Status = ScriptStatus.Failure.ToString();
                //    tasks.Add(se);
                //    break;
                //}

            //    tasks.Add(se);
            //}

            //pr.Client = c;
            //pr.Steps = tasks;
            //return pr;
        }
    }
}
