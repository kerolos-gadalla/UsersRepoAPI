using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UsersRepoAPI;
using System.Runtime.Serialization.Json;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Xml.Linq;
using System.Dynamic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MyDataContext>(opt => opt.UseInMemoryDatabase("User"));
builder.Services.AddSingleton<IHasher, BasicHasher>();
builder.Services.AddSingleton<IJWTService, BasicJWTService>();
builder.Services.AddAuthentication("jwt").AddJwtBearer("jwt", (o) =>
{
    IJWTService jWTService = builder.Services.BuildServiceProvider().GetService<IJWTService>();
    Console.WriteLine("####### I was here");
    Console.WriteLine(o);

    var validationParams = jWTService.GetValidationParameters();
    o.TokenValidationParameters = validationParams;

    o.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents()
    {
        OnMessageReceived = (ctx) =>
        {
            if (ctx.Request.Query.ContainsKey("t"))
            {
                ctx.Token = ctx.Request.Query["t"];
            }

            return Task.CompletedTask;
        }
    };


    //var ms = new MemoryStream();
    //var serial = new DataContractJsonSerializer(typeof(TokenValidationParameters));
    //serial.WriteObject(ms, validationParams);
    //var arr = ms.ToArray();
    //var json = Encoding.UTF8.GetString(arr, 0, arr.Length);
    o.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration()
    {
        SigningKeys = { validationParams.IssuerSigningKey }

    };

    o.MapInboundClaims = false;

});

builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
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

// just knowing what is minimal APIs
//app.MapGet("/hello", () =>
//{
//    return "Hello World";
//});


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

// Some tests I did
//app.MapGet("/validate/{token}", async (string token, MyDataContext db, IHasher hasher, IJWTService jWTService) =>
//{
//    var valid = jWTService.Validate(token);
//    Console.WriteLine(valid);

//    return Results.Ok(valid);
//});

app.MapGet("/users/{id}", async (string id, MyDataContext db, HttpContext ctx, IHasher hasher) =>
{
    var sub = ctx.User.Claims.First(x => hasher.Hash(x.Value).Equals(id));
    if (sub != null)
    {
        return await db.Users.FindAsync(id)
        is User user
            ? Results.Ok(UserMapper.mapUser(user))
            : Results.NotFound();
    }
    return Results.Unauthorized();
}).RequireAuthorization().WithMetadata("Authorization");


//app.MapGet("/users", async (MyDataContext db) =>
//    await db.Users.ToListAsync());

//app.MapGet("/www", async ([FromQuery] string token, HttpContext ctx) =>
//{
//    Console.WriteLine("WWWW");
//    Console.WriteLine(token);

//    return Results.Ok(new { user = ctx.User });
//});


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


class UserMapper
{
    public static dynamic mapUser(User user)
    {
        dynamic dto = new ExpandoObject();

        dto.Id = user.Id;
        dto.FirstName = user.FirstName;
        dto.LastName = user.LastName;
        dto.MarketingConcent = user.MarketingConcent;
        if (user.MarketingConcent)
        {
            dto.Email = user.Email;
        }

        return dto;
    }
}

//class UserHelper
//{
//    ClaimsPrincipal GetFromUser(User user)
//    {

//        var claims = new List<Claim>()
//        {
//            new Claim("email",user.Email)
//        };

//        var identity = new ClaimsIdentity(claims);
//        return new ClaimsPrincipal(identity);
//    }
//}

//class BasicAuthenticationOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
//{
//}


//class CustomAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
//{
//    private IJWTService jwtService;

//    public CustomAuthenticationHandler(
//        IOptionsMonitor<BasicAuthenticationOptions> options,
//        ILoggerFactory logger,
//        UrlEncoder encoder,
//        ISystemClock clock,
//        IJWTService jwtService
//        ) : base(options, logger, encoder, clock)
//    { this.jwtService = jwtService; }

//    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
//    {
//        // Different messages for debugging 
//        if (!Request.Headers.ContainsKey("Authorization"))
//        {
//            return AuthenticateResult.Fail("No Authorization");
//        }

//        string auth = Request.Headers["Authorization"];

//        if (string.IsNullOrEmpty(auth))
//        {
//            return AuthenticateResult.Fail("Empty Authorization");
//        }

//        if (!auth.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
//        {
//            return AuthenticateResult.Fail("Empty Authorization");
//        }

//        var token = auth.Substring("bearer".Length).Trim();


//        if (string.IsNullOrEmpty(token))
//        {
//            return AuthenticateResult.Fail("Empty Authorization");
//        }

//        try
//        {
//            var validated = jwtService.Validate(token);


//            if (!validated.IsValid)
//            {
//                return AuthenticateResult.Fail("Invalid Token");
//            }

//            var email = validated.Claims.email;


//            var claims = new List<Claim>()
//        {
//            new Claim("email",email)
//        };

//            var identity = new ClaimsIdentity(claims);
//            var principle = new ClaimsPrincipal(identity);

//            var ticket = new AuthenticationTicket(principle, Scheme.Name);

//            return AuthenticateResult.Success(ticket);
//        }
//        catch (Exception ex)
//        {
//            return AuthenticateResult.Fail(ex.Message);
//        }
//    }
//}