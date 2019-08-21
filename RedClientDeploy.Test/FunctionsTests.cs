using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace RedClientDeploy.Test
{
    public class FunctionsTests
    {
        private readonly ILogger logger = TestFactory.CreateLogger();

        [Fact]
        public async void Http_trigger_should_return_known_string()
        {
            var request = TestFactory.CreateHttpRequest("name", "Bill");

            RedClientDeploy.Handlers.TenantQueryCommandHandler tq = new RedClientDeploy.Handlers.TenantQueryCommandHandler(null, null, null);

          //  var response = (OkObjectResult)await tq.ExecuteAsync(request, logger);
          //  Assert.Equal("Hello, Bill", response.Value);
        }

        //[Theory]
        //[MemberData(nameof(TestFactory.Data), MemberType = typeof(TestFactory))]
        //public async void Http_trigger_should_return_known_string_from_member_data(string queryStringKey, string queryStringValue)
        //{
        //    var request = TestFactory.CreateHttpRequest(queryStringKey, queryStringValue);
        //    var response = (OkObjectResult)await HttpFunction.Run(request, logger);
        //    Assert.Equal($"Hello, {queryStringValue}", response.Value);
        //}

        //[Fact]
        //public void Timer_should_log_message()
        //{
        //    var logger = (ListLogger)TestFactory.CreateLogger(LoggerTypes.List);
        //    TimerTrigger.Run(null, logger);
        //    var msg = logger.Logs[0];
        //    Assert.Contains("C# Timer trigger function executed at", msg);
        //}
    }
}
