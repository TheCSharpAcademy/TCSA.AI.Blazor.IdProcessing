using Microsoft.EntityFrameworkCore;

namespace TCSA.AI.Blazor.IdProcessing.Data
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class Guest
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime DateOfBirth { get; set; }

        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime CheckInDate { get; set; }

        public string? Address { get; set; }
        public string? Country { get; set; }
    }

    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string dateString = reader.GetString();

            // Try parsing the date in the MM/dd/yyyy format
            if (DateTime.TryParseExact(dateString, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            // If parsing fails, try ISO 8601 format
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out result))
            {
                return result;
            }

            // If both parsing attempts fail, return DateTime.MinValue or handle as needed
            return DateTime.MinValue;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd")); // Write dates in ISO 8601 format
        }
    }


    public class GuestsContext : DbContext
    {
        public GuestsContext(DbContextOptions<GuestsContext> options) : base(options) { }

        public DbSet<Guest> Guests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Guest>(entity =>
            {
                entity.HasKey(g => g.Id); 

                entity.Property(g => g.FirstName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(g => g.LastName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(g => g.DateOfBirth)
                      .IsRequired();

                entity.Property(g => g.Address)
                      .HasMaxLength(200);

                entity.Property(g => g.Country)
                      .HasMaxLength(50);

                entity.Property(g => g.CheckInDate)
                      .IsRequired();
            });
        }
    }
}
