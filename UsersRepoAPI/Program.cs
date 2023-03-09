using Microsoft.EntityFrameworkCore;
using UsersRepoAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MyDataContext>(opt => opt.UseInMemoryDatabase("User"));
builder.Services.AddSingleton<IHasher, BasicHasher>();
builder.Services.AddSingleton<IJWTService, BasicJWTService>();
builder.Services.AddAuthentication("jwt").AddJwtBearer("jwt", o =>
{
    Console.WriteLine("####### I was here");
    Console.WriteLine(o);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();


app.MapGet("/hello", () =>
{
    return "Hello World";
});


app.MapPost("/users", async (User user, MyDataContext db, IHasher hasher, IJWTService jWTService) =>
{
    user.Id = hasher.Hash(user.Email);
    db.Users.Add(user);
    await db.SaveChangesAsync();

    var hash = hasher.Hash("email");

    return Results.Created($"/users/{user.Id}", new
    {
        id = user.Id,
        accessToken = jWTService.Issue(user.Email)
    });
});

app.MapGet("/users/{id}", async (string id, MyDataContext db) =>
    await db.Users.FindAsync(id)
        is User student
            ? Results.Ok(student)
            : Results.NotFound());

app.MapGet("/users", async (MyDataContext db) =>
    await db.Users.ToListAsync());


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


