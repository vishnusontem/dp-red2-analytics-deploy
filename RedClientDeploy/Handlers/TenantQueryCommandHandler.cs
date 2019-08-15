using AzureFromTheTrenches.Commanding.Abstractions;
using Newtonsoft.Json;
using RedClientDeploy.Commands;
using RedClientDeploy.Models;
using RedClientDeploy.Services;
using RedClientDeploy.Services2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RedClientDeploy.Handlers
{
    public class TenantQueryCommandHandler : ICommandHandler<TenantQueryCommand, Response>
    {
        private readonly RedClientDeploy.Services2.TableauService _tableauService;
        private readonly SnowflakeService _snowflakeService;

        public TenantQueryCommandHandler(RedClientDeploy.Services2.TableauService tableauService, SnowflakeService snowflakeService)
        {
            _tableauService = tableauService;
            _snowflakeService = snowflakeService;
        }

        public async Task<Response> ExecuteAsync(TenantQueryCommand command, Response previousResult)
        {

            Response tenant = new Response();

            try
            {
                var tb = new TableauTenant();
                var sf = new SnowflakeTenant();

                var tasks = new Task[] {
                    Task.Run(async () => tb = await _tableauService.GetTableauSiteAsync(command.Name)),
                    Task.Run(async () => sf = await _snowflakeService.GetReaderAsync(command.Name))
                };

                await Task.WhenAll(tasks);

                tenant.TenantDetails.Tableau = tb;
                tenant.TenantDetails.Snowflake = sf;

                //tenant.Tableau = await _tableauService.GetTableauSiteAsync(command.Name);
                //tenant.Snowflake = await _snowflakeService.GetReaderAsync(command.Name);
            }
            catch (Exception ex)
            {
                ErrorManager em = new ErrorManager();

                em = _tableauService.Error;

                tenant.ErrorMessages = new List<ErrorDetails>
                {
                    em.Error
                };
            }

            return tenant;

        }

    }
}
