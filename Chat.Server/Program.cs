using Chat.Server.Hubs;
using Chat.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ChatClient", policy =>
    {
        policy
            .WithOrigins("https://localhost:7002")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR();

builder.Services.AddSingleton<ChatUsersService>();
builder.Services.AddSingleton<MessagesService>();

var app = builder.Build();

await app.Services
    .GetRequiredService<MessagesService>()
    .InitializeAsync();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("ChatClient");

app.MapHub<ChatHub>("/chatHub");

app.Run();