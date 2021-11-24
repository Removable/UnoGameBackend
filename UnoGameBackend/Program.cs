using UnoGameBackend.Hubs;

var origins = new[] { "https://uno.geekgz.cn", "http://localhost:3000", "https://localhost:3000" };
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddCors(c =>
{
    c.AddDefaultPolicy(policy =>
    {
        // ֧�ֶ�������˿ڣ�ע��˿ںź�Ҫ��/б�ˣ�����localhost:8000/���Ǵ��
        // http://127.0.0.1:1818 �� http://localhost:1818 �ǲ�һ���ģ�����д����
        foreach (var origin in origins)
        {
            policy.WithOrigins(origin);
        }

        policy
            // .WithOrigins("http://192.168.1.40:3000")
            // .WithOrigins("http://localhost:3000")
            // .WithOrigins("https://localhost:3000")
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials()
            .AllowAnyHeader() //��������ͷ
            .AllowAnyMethod(); //�������ⷽ��
        // .WithExposedHeaders("act"); //�����Զ����actͷ��Ϣ
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120),
};
// webSocketOptions.AllowedOrigins.Add("http://192.168.1.40:3000");
// webSocketOptions.AllowedOrigins.Add("http://localhost:3000");
// webSocketOptions.AllowedOrigins.Add("https://localhost:3000");
foreach (var origin in origins)
{
    webSocketOptions.AllowedOrigins.Add(origin);
}

app.UseWebSockets(webSocketOptions);

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseDefaultFiles();

app.MapControllers();

app.UseRouting();

app.UseCors(b =>
{
    b.AllowAnyHeader()
        .AllowAnyMethod()
        // .WithOrigins("http://192.168.1.40:3000")
        // .WithOrigins("http://localhost:3000")
        // .WithOrigins("https://localhost:3000")
        .AllowCredentials();
    foreach (var origin in origins)
    {
        b.WithOrigins(origin);
    }
});

app.UseAuthorization();

app.UseEndpoints(endpoints => endpoints.MapHub<UnoHub>("/uno-hub"));

app.Run("http://*:25501");