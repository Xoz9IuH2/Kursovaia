using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using web_api.Data;
using web_api.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры
builder.Services.AddControllers();

// Добавляем Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Подключаем PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<VoenkomDbContext>(options =>
    options.UseNpgsql(connectionString));

// Настраиваем JWT аутентификацию
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

// Добавляем CORS для React фронтенда
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Регистрируем сервисы
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PersonalFileService>();
builder.Services.AddScoped<SummonService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<EvaderService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<GeoLocationService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddHostedService<ArchiveCleanupService>();
builder.Services.AddHostedService<SummonAutoService>();

var app = builder.Build();

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VoenkomDbContext>();
    await DbInitializer.InitializeAsync(context);
    
    // Добавляем поле ArchivedAt если его нет
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""PersonalFiles"" ADD COLUMN IF NOT EXISTS ""ArchivedAt"" timestamp with time zone;
        ");
    }
    catch { }

    // Добавляем новые колонки в Users
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""AccountStatus"" varchar(100);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""DateOfBirth"" timestamp with time zone;
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""RegistrationAddress"" varchar(500);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""ResidenceAddress"" varchar(500);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Passport_series"" varchar(10);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Passport_number"" varchar(20);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Passport_issued"" varchar(500);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Passport_date"" timestamp with time zone;
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""MilitaryTicketNumber"" varchar(50);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""FitnessCategory"" varchar(10);
        ");
    }
    catch { }

    // Добавляем колонку Action в Notifications
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Notifications"" ADD COLUMN IF NOT EXISTS ""Action"" varchar(100);
        ");
    }
    catch { }

    // Добавляем колонки в PersonalFiles
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""PersonalFiles"" ADD COLUMN IF NOT EXISTS ""MilitaryTicketSeries"" varchar(10);
            ALTER TABLE ""PersonalFiles"" ADD COLUMN IF NOT EXISTS ""MilitaryTicketNumber"" varchar(20);
            ALTER TABLE ""PersonalFiles"" ADD COLUMN IF NOT EXISTS ""MilitaryRank"" varchar(50);
            ALTER TABLE ""Summons"" ADD COLUMN IF NOT EXISTS ""DeliveredAt"" timestamp with time zone;
        ");
    }
    catch { }

    // Добавляем колонки в Documents
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""Status"" varchar(50) DEFAULT 'pending';
            ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""RejectionReason"" text;
            ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""DocumentType"" varchar(100);
            ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""UploadedById"" integer;
            ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""VerifiedAt"" timestamp with time zone;
        ");
    }
    catch { }

    // Добавляем колонки в CalendarEvents
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""CalendarEvents"" ADD COLUMN IF NOT EXISTS ""BookedSlots"" integer DEFAULT 0;
            ALTER TABLE ""CalendarEvents"" ADD COLUMN IF NOT EXISTS ""MaxSlots"" integer DEFAULT 30;
            ALTER TABLE ""CalendarEvents"" ADD COLUMN IF NOT EXISTS ""IsAvailable"" boolean DEFAULT true;
            ALTER TABLE ""CalendarEvents"" ADD COLUMN IF NOT EXISTS ""CreatedById"" integer DEFAULT 0;
            ALTER TABLE ""CalendarEvents"" ADD COLUMN IF NOT EXISTS ""UpdatedAt"" timestamp with time zone;
        ");
    }
    catch { }

    // Добавляем колонки в AuditLogs
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""AuditLogs"" ADD COLUMN IF NOT EXISTS ""TableName"" varchar(100);
            ALTER TABLE ""AuditLogs"" ADD COLUMN IF NOT EXISTS ""RecordId"" integer;
        ");
    }
    catch { }

    // Добавляем колонки Users для фото документов
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PassportPhotoPath"" varchar(500);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PassportPhotoStatus"" varchar(50) DEFAULT 'none';
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""MilitaryPhotoPath"" varchar(500);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""MilitaryPhotoStatus"" varchar(50) DEFAULT 'none';
        ");
    }
    catch { }

    // Удаляем старые архивы
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""PersonalFiles"" WHERE status = 'archived';");
    }
    catch { }
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseStaticFiles();

var distPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "dist");
if (Directory.Exists(distPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(distPath),
        RequestPath = ""
    });
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(distPath)
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
