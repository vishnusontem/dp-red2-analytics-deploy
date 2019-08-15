using System;
using System.Collections.Generic;
using System.Text;

namespace RedClientDeploy.Models
{
    public class Response
    {
        public Tenant TenantDetails { get; set; }

        public List<ErrorDetails> ErrorMessages { get; set; }

        public List<ScriptExecution> Tasks { get; set; }

        public Response()
        {
            TenantDetails = new Tenant();

        }

    }


}
