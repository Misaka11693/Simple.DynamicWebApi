//using Microsoft.AspNetCore.Mvc;
//using Simple.DynamicApiController.Dependencies;

//namespace DynamicWebApiExample
//{
//    //[Route("api/[controller]")]
//    //[NonController]
//    public class ExampleAppService : IDynamicApiController
//    {
//        //[NonAction]标记不是一个Action方法，不会被注册为路由
//        //[HttpGet("GetName")]
//        public string GetHelloWorldAsync()
//        {
//            return "Hello World!";
//        }

//        //[HttpGet("SayHelloWorld")]
//        public string SayHelloWorld()
//        {
//            return "Hello World!";
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using Simple.DynamicWebApi;

public class AppleService : IDynamicWebApi
{
    [HttpGet("{id:int}")]
    public int GetApple(int id)
    {
        return id;
    }
}

[DynamicApi]
public class OrangeService
{
    [HttpGet("{id:int}")]
    public int GetOrange(int id) => id;
}