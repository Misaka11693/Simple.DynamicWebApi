
using Simple.DynamicWebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

//添加Swagger服务
builder.Services.AddSwaggerGen(o =>
{
    //※重要：添加分组文档(如果不加分组，则 动态WebApi 接口不会显示在Swagger文档中)
    o.DocInclusionPredicate((docName, description) => true);

    //添加XML注释文档
    var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
    foreach (var xmlFile in Directory.GetFiles(basePath, "*.xml"))
    {
        o.IncludeXmlComments(xmlFile, true);
    }
});

//添加动态WebApi控制器
builder.Services.AddDynamicApiController(o =>
{
    //是否将根路径添加到路由(默认不添加) api/app/..
    o.AddRootPathToRoute = true;
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
