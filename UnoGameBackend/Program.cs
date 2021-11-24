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
        // 支持多个域名端口，注意端口号后不要带/斜杆：比如localhost:8000/，是错的
        // http://127.0.0.1:1818 和 http://localhost:1818 是不一样的，尽量写两个
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
            .AllowAnyHeader() //允许任意头
            .AllowAnyMethod(); //允许任意方法
        // .WithExposedHeaders("act"); //允许自定义的act头信息
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