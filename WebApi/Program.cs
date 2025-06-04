
using Simple.DynamicWebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

//���Swagger����
builder.Services.AddSwaggerGen(o =>
{
    //����Ҫ����ӷ����ĵ�(������ӷ��飬�� ��̬WebApi �ӿڲ�����ʾ��Swagger�ĵ���)
    o.DocInclusionPredicate((docName, description) => true);

    //���XMLע���ĵ�
    var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
    foreach (var xmlFile in Directory.GetFiles(basePath, "*.xml"))
    {
        o.IncludeXmlComments(xmlFile, true);
    }
});

//��Ӷ�̬WebApi������
builder.Services.AddDynamicApiController(o =>
{
    //�Ƿ񽫸�·����ӵ�·��(Ĭ�ϲ����) api/app/..
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
