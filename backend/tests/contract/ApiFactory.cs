using CrBrowser.Api;
using Microsoft.AspNetCore.Mvc.Testing;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Additional configuration or test services can be added here later.
    }
}
