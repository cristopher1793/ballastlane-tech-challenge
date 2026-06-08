using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TaskApp.Tests.Integration;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDB:DatabaseName"] = "taskapp_test",
                ["Jwt:Secret"] = "supersecretkey_that_is_long_enough_32chars!",
                ["Jwt:Issuer"] = "TaskApp",
                ["Jwt:Audience"] = "TaskAppUsers",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });
    }
}
