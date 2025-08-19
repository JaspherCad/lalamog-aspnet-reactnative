using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using MyApi.Services;
using MyApi.Data;
using MyApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Npgsql;
using MyApi.Interfaces;
using MyApi.Controllers;
using Microsoft.AspNetCore.SignalR;
//PERSONAL NOTE DO NOT DELETE THIS TEXT COMMENT:
// program.cs is just configuration and setup only where we define 
// rules (jwt, addgoogle options etc), 
// settings (external provider client IDs, secret keys)and 
// register services (DI) for the application


//There’s No Business SERVICE Logic in Program.cs
// we're not handling user requests, processing data, or implementing features here.
// we’re just telling ASP.NET Core how to behave:

// Authentication/Authorization configuration

// Service registration for Dependency Injection (DI)

// Middleware setup (what happens per HTTP request, step-by-step)

AppContext.SetSwitch("Npgsql.EnableNetTopologySuite", true);


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy
            .AllowAnyOrigin()      // Allows any origin
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<ApplicationDbContext>(opts =>
{
    var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
    dataSourceBuilder.UseNetTopologySuite(); // Keep this for geographic data.. spatial data

    //dataSourceBuilder.UseSomethingData()
    //dataSourceBuilder.UseMoreConfig()
    //like in java, the CHAIN OF BUILD.


    var dataSource = dataSourceBuilder.Build();
    
    opts.UseNpgsql(dataSource, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory").UseNetTopologySuite());
});


// register Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISwipeService, SwipeService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IFightingScheduleService, FightingScheduleService>();



builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = builder.Configuration.GetValue<long>("SignalR:MaxMessageSize");
});

// Register repositories
builder.Services.AddScoped<MyApi.Repositories.IWeatherRepository, MyApi.Repositories.WeatherRepository>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //for api calls
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

    options.DefaultSignInScheme = "CookiesTest"; //required for google auth BECAUSE OF STATEFUL USE, meaning we need to store the session state aka RETAINS info,... chatgpt stateful again... since multi step ito, this has to be stateful compared to our django STATELESS header lang,
})

//part 2 about cookie below:


.AddCookie("CookiesTest") // cookie here is just a requireaments of ASP NET + Oauth2.. dito iniistore yung STATEFULLNESS ng dalawa.... do i have to worry here? no.
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
    options.SignInScheme = "CookiesTest"; 
});

builder.Services.AddAuthorization();

builder.Services.AddDirectoryBrowser();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


//use CORS policy
app.UseCors("AllowReactApp");


app.MapHub<MessageHub>("/hubs/messagehub");

// Add static files middleware
app.UseDefaultFiles();
app.UseStaticFiles();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
