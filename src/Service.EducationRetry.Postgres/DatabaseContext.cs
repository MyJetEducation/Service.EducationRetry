using Microsoft.EntityFrameworkCore;
using MyJetWallet.Sdk.Postgres;
using MyJetWallet.Sdk.Service;
using Service.EducationRetry.Domain.Models;

namespace Service.EducationRetry.Postgres
{
    public class DatabaseContext : MyDbContext
    {
        public const string Schema = "education";
        private const string EducationRetryTableName = "educationretry";

        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<EducationRetryEntity> AssetsDictionarEntities { get; set; }

        public static DatabaseContext Create(DbContextOptionsBuilder<DatabaseContext> options)
        {
            MyTelemetry.StartActivity($"Database context {Schema}")?.AddTag("db-schema", Schema);

            return new DatabaseContext(options.Options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            SetUserInfoEntityEntry(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void SetUserInfoEntityEntry(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EducationRetryEntity>().ToTable(EducationRetryTableName);
            modelBuilder.Entity<EducationRetryEntity>().Property(e => e.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<EducationRetryEntity>().Property(e => e.Date).IsRequired();
            modelBuilder.Entity<EducationRetryEntity>().Property(e => e.Value);
            modelBuilder.Entity<EducationRetryEntity>().HasKey(e => e.Id);
        }
    }
}
