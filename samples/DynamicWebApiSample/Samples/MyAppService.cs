using Simple.DynamicWebApi;

namespace WebApplication1;

public class MyAppService : IDynamicWebApi
{
    public Task<string> Get()
    {
        return Task.FromResult("Hello, world!");
    }
}
