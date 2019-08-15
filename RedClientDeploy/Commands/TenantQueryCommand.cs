
using AzureFromTheTrenches.Commanding.Abstractions;
using RedClientDeploy.Models;

namespace RedClientDeploy.Commands
{
    public class TenantQueryCommand : ICommand<Response>
    {
        public string Name { get; set; }
    }
}
