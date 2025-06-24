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

//public class AppleService : IDynamicWebApi
//{
//    [HttpGet("{id:int}")]
//    public int GetApple(int id)
//    {
//        return id;
//    }
//}

//[DynamicApi]
//[Route("api/ooi")]
public class OrangeService : IDynamicWebApi
{
    //[HttpGet("aa/{id:int}")]
    //[HttpGet("aaa22/{id:int}")]
    //[HttpGet("aaa/{id:int}")]
    //[HttpGet]
    //[HttpGet("{id:int}")]
    //public int GetTestOrange(int id) => id;
    //[ActionName("sss/{id:int}")]

    //[HttpGet("get-orange/hello-world")]
    //[Route("api/[controller]/[action]")]

    public string GetHello()
    {
        return "Hello World";
    }

    public string Post()
    {
        return "Hello World";
    }

    //public string GetHelloWorld(int id)
    //{
    //    return "Hello World";
    //}

    //public string GetHelloWorld2(int id)
    //{
    //    return "Hello World";
    //}

    //[HttpPost("create-student/{student:Student}")]

    //public Student PostStudent(Student student)
    //{
    //    var result = student;
    //    return result;
    //}

    public class Student
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}