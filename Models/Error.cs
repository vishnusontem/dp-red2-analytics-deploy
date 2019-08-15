using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
namespace RedClientDeploy.Models
{
    [DataContract]
    [JsonObject("error")]
    public class ErrorDetails
    {
        [DataMember]
        public string Summary { get; set; }
        [DataMember]
        public string Detail { get; set; }
        [DataMember]
        public string Code { get; set; }
        
        public string Message { get; set; }
        
        public HttpStatusCode StatusCode { get; set; }
        
        public string ReasonPhrase { get; set; }
    }

    public class ErrorManager
    {
        
        public ErrorDetails Error { get; set; }
        [JsonIgnore]
        public bool HasError { get; set; }

        public async System.Threading.Tasks.Task ManageHttpErrorAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
          //  Error = await response.Content.ReadAsAsync<ErrorDetails>();

            var e = (ErrorManager)JsonConvert.DeserializeObject(content, typeof(ErrorManager));

            Error = e.Error;

            if (string.IsNullOrEmpty(Error.Detail))
            {
                Error.Detail = content;
            }

            Error.Message = content;

            //  ErrorMessage error = JsonConvert.DeserializeObject<ErrorMessage>(message);
            // error.Message = message;

            Error.StatusCode = response.StatusCode;
            Error.ReasonPhrase = response.ReasonPhrase;

            HasError = true;
        }

        public void Clear()
        {
            Error = new ErrorDetails();
            HasError = false;
        }
    }


}
