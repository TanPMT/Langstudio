using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using backend.Data;
using backend.Models;
using backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true; // Ensure email is unique
    options.SignIn.RequireConfirmedEmail = true; // Require email confirmation
    //options.User.AllowedUserNameValidator = new CustomUserNameValidator(); // Allow non-unique FullName
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    // Thêm đoạn này để lấy token từ cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddScoped<MongoDbContext>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    return new MongoDbContext(configuration);
});

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWritingService, WritingService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// cros
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://tanpmt.github.io") // đổi theo domain FE nếu cần
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // nếu dùng cookie hoặc auth header
    });
});


var app = builder.Build();
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 404 &&
        !context.Response.HasStarted &&
        context.Request.Path.StartsWithSegments(""))
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("""
                                          {
                                              "status": 404,
                                              "message": "Not Found"
                                          }
                                          """);
    }
});

app.UseCors("AllowVueApp");


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Custom validator to bypass unique UserName requirement
public class CustomUserNameValidator : IUserValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        // Allow non-unique UserName (FullName), as Email is the unique identifier
        return Task.FromResult(IdentityResult.Success);
    }
}
