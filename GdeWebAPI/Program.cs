
using GdeWebAPI.Middleware;
using GdeWebAPI.Services;
using GdeWebDB;
using GdeWebDB.Interfaces;
using GdeWebDB.Services;
using LangChain.Providers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;


namespace GdeWebAPI
{
    /// <summary>
    /// Alkalmazás belépési pontja és host konfiguráció.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Az alkalmazás belépési pontja. Beállítja a hostot, a szolgáltatásokat és elindítja a webalkalmazást.
        /// </summary>
        /// <param name="args">Parancssori argumentumok.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Cors policy
            var myAllowSpecificOrigins = "_myPolicy";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: myAllowSpecificOrigins,
                                  policy =>
                                  {
                                      policy.WithOrigins("http://localhost",
                                                          "https://eduai.omegacode.cloud")
                                      .WithMethods("POST", "PUT", "DELETE", "GET")
                                      .SetIsOriginAllowedToAllowWildcardSubdomains()
                                      .AllowAnyOrigin()
                                      .AllowAnyHeader()
                                      .AllowAnyMethod();
                                  });
            });

            // Message Rate Limiter
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("MessagePolicy", opt =>
                {
                    opt.PermitLimit = 1;              // 1 kérés
                    opt.Window = TimeSpan.FromMinutes(1); // percenként
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;               // ne soroljunk
                });

                // 💬 Egyedi hibaüzenet, ha a limit túllépésre kerül
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests; // vagy 503, ha úgy tetszik
                    context.HttpContext.Response.ContentType = "application/json; charset=utf-8";

                    var message = new
                    {
                        success = false,
                        error = "Az ön hitelesítő tokenje korlátozva van: percenként legfeljebb 1 kérés engedélyezett az ön szerveréről."
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(message);
                    await context.HttpContext.Response.WriteAsync(json, token);
                };
            });

            // SQLite file az app mappájában
            var cs = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=./gde.db";

            builder.Services.AddDbContext<GdeDbContext>(opt => opt.UseSqlite(cs));

            // AI SERVICE
            builder.Services.AddHttpClient("openai", c =>
            {
                c.Timeout = TimeSpan.FromMinutes(10); // vagy Timeout.InfiniteTimeSpan
                //c.Timeout = Timeout.InfiniteTimeSpan;
                // Accept header-t a kérésnél is állítjuk majd a stream-re, de itt is maradhat általános
            });
            builder.Services.AddSingleton<AiService>();   // saját, pici szolgáltatás

            builder.Services.AddScoped<IAuthService, AuthService>(); // az új EF-es AuthService
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ITrainingService, TrainingService>();
            builder.Services.AddScoped<INoteService, NoteService>();

            builder.Services.AddScoped<SpeechToTextService>();
            builder.Services.AddScoped<TextCleaningService>();

            // Ha a MailService / LogService is DbContextet használ, azokat is Scoped-ra.
            // Ha nem használ DbContextet és stateless, maradhat Singleton.
            builder.Services.AddSingleton<IMailService, MailService>();
            builder.Services.AddSingleton<ILogService, LogService>();
            

            // Hosted Service - Background Service regisztráció
            builder.Services.AddHostedService<HostedService>();

            // Add services to the container.

            builder.Services.AddControllersWithViews();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            
            // Middleware szolgáltatások regisztrációja
            builder.Services.AddScoped<Middleware.AccessTokenFilter>();
            //builder.Services.AddControllers(o => o.Filters.Add<AccessTokenFilter>());

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Gde.API",
                    Version = "v1",
                    Description = "Gde1.API Swagger Documentation",
                    Contact = new OpenApiContact
                    {
                        Name = "Gde Developer",
                        Email = "teszt@gde.hu",
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // Set the comments path for the Swagger JSON and UI.**
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            var app = builder.Build();

            // Apply pending migrations automatically
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<GdeDbContext>();
                    
                    // First try to apply migrations
                    try
                    {
                        db.Database.Migrate();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Database migrations applied successfully.");
                    }
                    catch
                    {
                        // If migration fails, create tables manually as fallback
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Migration failed, attempting to create tables manually...");
                        
                        try
                        {
                            // Create A_NOTE table if it doesn't exist
                            db.Database.ExecuteSqlRaw(@"
                                CREATE TABLE IF NOT EXISTS ""A_NOTE"" (
                                    ""NOTEID"" INTEGER NOT NULL CONSTRAINT ""PK_A_NOTE"" PRIMARY KEY AUTOINCREMENT,
                                    ""USERID"" INTEGER NOT NULL,
                                    ""COURSEID"" INTEGER NOT NULL,
                                    ""NOTETITLE"" TEXT NOT NULL,
                                    ""NOTECONTENT"" TEXT NOT NULL,
                                    ""CREATIONDATE"" TEXT NOT NULL,
                                    ""MODIFICATIONDATE"" TEXT NOT NULL,
                                    CONSTRAINT ""FK_A_NOTE_T_USER_USERID"" FOREIGN KEY (""USERID"") REFERENCES ""T_USER"" (""USERID"") ON DELETE CASCADE,
                                    CONSTRAINT ""FK_A_NOTE_A_COURSE_COURSEID"" FOREIGN KEY (""COURSEID"") REFERENCES ""A_COURSE"" (""COURSEID"") ON DELETE CASCADE
                                );
                                
                                CREATE INDEX IF NOT EXISTS ""IX_A_NOTE_USERID_COURSEID"" ON ""A_NOTE"" (""USERID"", ""COURSEID"");
                            ");
                            
                            // Create A_MONTHLY_SUMMARY table if it doesn't exist
                            db.Database.ExecuteSqlRaw(@"
                                CREATE TABLE IF NOT EXISTS ""A_MONTHLY_SUMMARY"" (
                                    ""SUMMARYID"" INTEGER NOT NULL CONSTRAINT ""PK_A_MONTHLY_SUMMARY"" PRIMARY KEY AUTOINCREMENT,
                                    ""USERID"" INTEGER NOT NULL,
                                    ""YEAR"" INTEGER NOT NULL,
                                    ""MONTH"" INTEGER NOT NULL,
                                    ""SUMMARY"" TEXT NOT NULL,
                                    ""WHATLEARNED"" TEXT NOT NULL,
                                    ""WHATPRESENTED"" TEXT NOT NULL,
                                    ""CREATIONDATE"" TEXT NOT NULL,
                                    ""MODIFICATIONDATE"" TEXT NOT NULL,
                                    CONSTRAINT ""FK_A_MONTHLY_SUMMARY_T_USER_USERID"" FOREIGN KEY (""USERID"") REFERENCES ""T_USER"" (""USERID"") ON DELETE CASCADE
                                );
                                
                                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_A_MONTHLY_SUMMARY_USERID_YEAR_MONTH"" ON ""A_MONTHLY_SUMMARY"" (""USERID"", ""YEAR"", ""MONTH"");
                            ");
                            
                            logger.LogInformation("Tables created successfully using fallback method.");
                        }
                        catch (Exception ex2)
                        {
                            logger.LogError(ex2, "Failed to create tables manually.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to apply database migrations.");
            }

            // Activate user zsuzs@gmail.com if needed (one-time script)
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<GdeDbContext>();
                    var user = db.T_USER.FirstOrDefault(u => u.EMAIL == "zsuzs@gmail.com");
                    if (user != null && !user.ACTIVE)
                    {
                        user.ACTIVE = true;
                        user.MODIFICATIONDATE = DateTime.UtcNow;
                        db.SaveChanges();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("User zsuzs@gmail.com has been activated.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail if database doesn't exist yet or other issues
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogWarning($"Could not activate user: {ex.Message}");
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gde1.API v1");
                    //c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
                    c.EnableDeepLinking();
                });
            }

            app.UseHttpsRedirection();

            // >>> statikus fájlok kiszolgálása
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".mp4"] = "video/mp4";   // biztos, ami biztos

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider
                // RequestPath nélkül a wwwroot gyökerét szolgáljuk ki
            });

            app.UseCors(myAllowSpecificOrigins);

            app.UseAuthorization();

            // Rate Limiter
            app.UseRateLimiter();

            app.MapControllers();

            app.Run();
        }
    }
}
