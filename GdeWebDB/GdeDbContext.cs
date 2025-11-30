using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GdeWebDB.Entities;
using Microsoft.EntityFrameworkCore;

//# Powershell
//dotnet tool install --global dotnet-ef

//# STARTUP projekt (GdeWebAPI) – IDE KELL A Design csomag!
//dotnet add GdeWebAPI package Microsoft.EntityFrameworkCore.Design

//# DbContext projekt (GdeWebDB) – EF és Sqlite provider
//dotnet add GdeWebDB package Microsoft.EntityFrameworkCore
//dotnet add GdeWebDB package Microsoft.EntityFrameworkCore.Sqlite
//dotnet add GdeWebDB package Microsoft.EntityFrameworkCore.Tools

// Migráció létrehozása és alkalmazása SQLite adatbázisra
//dotnet ef migrations add InitialSqlite -p GdeWebDB -s GdeWebAPI
//dotnet ef database update -p GdeWebDB -s GdeWebAPI

//Magyarázat:
//-p GdeWebDB → a projekt, ahol a GdeDbContext van
//-s GdeWebAPI → a startup projekt, ahol a Program.cs van (ez tölti be a konfigurációt és indítja az EF-et)

// Migration listázás
//dotnet ef migrations list -p GdeWebDB -s GdeWebAPI

// Teljes visszatekerés nullára (ha nem engedi törölni)
//dotnet ef database update 0 -p GdeWebDB -s GdeWebAPI

// Remove migration
//dotnet ef migrations remove -p GdeWebDB -s GdeWebAPI

namespace GdeWebDB
{
    public class GdeDbContext : DbContext
    {
        public DbSet<User> T_USER => Set<User>();
        public DbSet<Role> T_ROLE => Set<Role>();
        public DbSet<UserRole> K_USER_ROLES => Set<UserRole>();
        public DbSet<AuthToken> T_AUTHENTICATION => Set<AuthToken>();

        public DbSet<Course> A_COURSE => Set<Course>();
        public DbSet<Quiz> A_QUIZ => Set<Quiz>();
        public DbSet<Note> A_NOTE => Set<Note>();
        public DbSet<MonthlySummary> A_MONTHLY_SUMMARY => Set<MonthlySummary>();


        public GdeDbContext(DbContextOptions<GdeDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // T_USER
            mb.Entity<User>(e =>
            {
                e.ToTable("T_USER");
                e.HasKey(x => x.USERID);
                e.Property(x => x.USERID).ValueGeneratedOnAdd();
                e.Property(x => x.GUID).HasConversion(v => v.ToString(), s => Guid.Parse(s));
                e.Property(x => x.PASSWORD).HasMaxLength(200).IsRequired();
                e.Property(x => x.FIRSTNAME).HasMaxLength(50).IsRequired();
                e.Property(x => x.LASTNAME).HasMaxLength(50).IsRequired();
                e.Property(x => x.EMAIL).HasMaxLength(100);
                // SQLite-nál default értéket érdemes app-oldalon beállítani:
                e.Property(x => x.ACTIVE).HasDefaultValue(false);
                e.Property(x => x.USERDATAJSON); // SQLite: TEXT; MSSQL: NVARCHAR(MAX)
                e.Property(x => x.MODIFICATIONDATE);
            });

            // T_ROLE
            mb.Entity<Role>(e =>
            {
                e.ToTable("T_ROLE");
                e.HasKey(x => x.ROLEID);
                e.Property(x => x.ROLEID).ValueGeneratedOnAdd();
                e.Property(x => x.ROLENAME).HasMaxLength(100).IsRequired();
                e.Property(x => x.VALID).HasDefaultValue(false);
                e.Property(x => x.MODIFIER).IsRequired();
            });

            // K_USER_ROLES
            mb.Entity<UserRole>(e =>
            {
                e.ToTable("K_USER_ROLES");
                e.HasKey(x => x.USERROLES);
                e.Property(x => x.USERROLES).ValueGeneratedOnAdd();

                e.HasOne(ur => ur.User)
                 .WithMany(u => u.UserRoles)
                 .HasForeignKey(ur => ur.USERID)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ur => ur.Role)
                 .WithMany(r => r.UserRoles)
                 .HasForeignKey(ur => ur.ROLEID)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // T_AUTHENTICATION
            mb.Entity<AuthToken>(e =>
            {
                e.ToTable("T_AUTHENTICATION");
                e.HasKey(x => x.TOKENID);
                e.Property(x => x.TOKENID).ValueGeneratedOnAdd();

                e.HasOne(t => t.User)
                 .WithMany(u => u.Tokens)
                 .HasForeignKey(t => t.USERID)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // A_COURSE
            mb.Entity<Course>(e =>
            {
                e.ToTable("A_COURSE");
                e.HasKey(x => x.COURSEID);
                e.Property(x => x.COURSEID).ValueGeneratedOnAdd();

                e.Property(x => x.COURSETITLE).IsRequired();
                e.Property(x => x.COURSEDESCRIPTION).IsRequired();

                e.Property(x => x.COURSEFILE).IsRequired();
                e.Property(x => x.COURSEFILETEXT).IsRequired();

                e.Property(x => x.COURSEMEDIA).IsRequired();
                e.Property(x => x.COURSEMEDIATEXT).IsRequired();
                e.Property(x => x.COURSEMEDIADURATION).HasDefaultValue(0);

                e.Property(x => x.COURSESUMMARYKEYWORDS).HasDefaultValue(string.Empty);

                e.Property(x => x.COURSEAIREQUESTJSON); // SQLite: TEXT; MSSQL: NVARCHAR(MAX)
                e.Property(x => x.COURSEAIRESPONSEJSON); // SQLite: TEXT; MSSQL: NVARCHAR(MAX)
                e.Property(x => x.COURSEDB).HasMaxLength(100).IsRequired();

                e.Property(x => x.MODIFICATIONDATE); // SQLite-nál app oldalon töltsd (GETDATE nem elérhető)
            });

            // A_QUIZ
            mb.Entity<Quiz>(e =>
            {
                e.ToTable("A_QUIZ");
                e.HasKey(x => x.QUIZID);
                e.Property(x => x.QUIZID).ValueGeneratedOnAdd();

                e.Property(x => x.COURSEID).HasDefaultValue(0);

                e.Property(x => x.QUIZQUESTION).IsRequired();
                e.Property(x => x.QUIZANSWER1).IsRequired();
                e.Property(x => x.QUIZANSWER2).IsRequired();
                e.Property(x => x.QUIZANSWER3).IsRequired();
                e.Property(x => x.QUIZANSWER4).IsRequired();
                e.Property(x => x.QUIZSUCCESS).IsRequired();

                e.Property(x => x.MODIFICATIONDATE); // töltsd app oldalon
            });

            // A_NOTE
            mb.Entity<Note>(e =>
            {
                e.ToTable("A_NOTE");
                e.HasKey(x => x.NOTEID);
                e.Property(x => x.NOTEID).ValueGeneratedOnAdd();

                e.Property(x => x.USERID).IsRequired();
                e.Property(x => x.COURSEID).IsRequired();
                e.Property(x => x.NOTETITLE).HasMaxLength(200).IsRequired();
                e.Property(x => x.NOTECONTENT); // SQLite: TEXT; MSSQL: NVARCHAR(MAX)
                e.Property(x => x.CREATIONDATE).IsRequired();
                e.Property(x => x.MODIFICATIONDATE).IsRequired();

                e.HasOne(n => n.User)
                 .WithMany()
                 .HasForeignKey(n => n.USERID)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(n => n.Course)
                 .WithMany()
                 .HasForeignKey(n => n.COURSEID)
                 .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: one note per user per course (or allow multiple - remove if needed)
                e.HasIndex(x => new { x.USERID, x.COURSEID });
            });

            // A_MONTHLY_SUMMARY
            mb.Entity<MonthlySummary>(e =>
            {
                e.ToTable("A_MONTHLY_SUMMARY");
                e.HasKey(x => x.SUMMARYID);
                e.Property(x => x.SUMMARYID).ValueGeneratedOnAdd();

                e.Property(x => x.USERID).IsRequired();
                e.Property(x => x.YEAR).IsRequired();
                e.Property(x => x.MONTH).IsRequired();
                e.Property(x => x.SUMMARY); // SQLite: TEXT; MSSQL: NVARCHAR(MAX)
                e.Property(x => x.WHATLEARNED); // SQLite: TEXT; MSSQL: NVARCHAR(MAX)
                e.Property(x => x.WHATPRESENTED); // SQLite: TEXT; MSSQL: NVARCHAR(MAX)
                e.Property(x => x.CREATIONDATE).IsRequired();
                e.Property(x => x.MODIFICATIONDATE).IsRequired();

                e.HasOne(s => s.User)
                 .WithMany()
                 .HasForeignKey(s => s.USERID)
                 .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: one summary per user per month
                e.HasIndex(x => new { x.USERID, x.YEAR, x.MONTH }).IsUnique();
            });



            var seedTime = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);


            // Opcionális seed (a csatolt MSSQL script mintáival szinkronban)
            // Figyelem: MSSQL-ben NEWID()/GETDATE() volt – itt konkrét értékeket adunk.
            mb.Entity<Role>().HasData(
                new Role { ROLEID = 1, ROLENAME = "Admin", VALID = true, MODIFIER = 1, MODIFICATIONDATE = seedTime },
                new Role { ROLEID = 2, ROLENAME = "User", VALID = true, MODIFIER = 1, MODIFICATIONDATE = seedTime }
            );

            // Ha szeretnéd, a minta felhasználót is feltölthetjük ugyanazzal a hash-sel:
            // A csatolt script hash-e (SHA-512) a PASSWORD oszlopban (első user sorban) :contentReference[oaicite:1]{index=1}
            mb.Entity<User>().HasData(
                new User
                {
                    USERID = 1,
                    GUID = Guid.Parse("0d2f1a91-ba24-4203-9b89-2d7f19ac9a7a"), // FIX GUID!
                    PASSWORD = "6e5e3ea6ece1e5388fa856ca7ed3fc18062b6a9666c84a94e956658d3cc15c32cc75086a9856f7afca15b7dc7d7b5352411c44b5e86eaff02266b017d463f2d9", // Barack.88
                    FIRSTNAME = "Dávid",
                    LASTNAME = "Jakab",
                    EMAIL = "zsuzs@gmail.com",
                    ACTIVE = true,
                    USERDATAJSON = "{}", // üres UserData
                    MODIFICATIONDATE = seedTime
                }
            );

            mb.Entity<UserRole>().HasData(
                new UserRole
                {
                    USERROLES = 1,
                    USERID = 1,
                    ROLEID = 1,
                    CREATOR = 1,
                    CREATINGDATE = seedTime
                }
            );
        }
    }
}
