using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Sponsor> Sponsors => Set<Sponsor>();
    public DbSet<EventSponsor> EventSponsors => Set<EventSponsor>();
    public DbSet<WeatherLog> WeatherLogs => Set<WeatherLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users: email must be unique
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        // User 1-1 Organiser / Participant
        modelBuilder.Entity<Organiser>()
            .HasOne(o => o.User).WithOne(u => u.Organiser)
            .HasForeignKey<Organiser>(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Participant>()
            .HasOne(p => p.User).WithOne(u => u.Participant)
            .HasForeignKey<Participant>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Participant>()
            .HasIndex(p => p.Email).IsUnique();

        // Organiser 1-M Events (restrict: avoids multiple cascade paths in SQL Server)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany(o => o.Events)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Event 1-M Categories (deleting an event deletes its categories, per the plan)
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Category 1-M Enrolments (restrict: a category with enrolments cannot be deleted)
        modelBuilder.Entity<Enrolment>()
            .HasOne(en => en.Category).WithMany(c => c.Enrolments)
            .HasForeignKey(en => en.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Participant 1-M Enrolments
        modelBuilder.Entity<Enrolment>()
            .HasOne(en => en.Participant).WithMany(p => p.Enrolments)
            .HasForeignKey(en => en.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bib numbers are unique; a participant can enrol in a category only once
        modelBuilder.Entity<Enrolment>()
            .HasIndex(en => en.BibNumber).IsUnique();
        modelBuilder.Entity<Enrolment>()
            .HasIndex(en => new { en.ParticipantId, en.CategoryId }).IsUnique();

        // Enrolment 1-1 Payment and Result
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Enrolment).WithOne(en => en.Payment)
            .HasForeignKey<Payment>(p => p.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment).WithOne(en => en.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Result captured by an Organiser
        modelBuilder.Entity<Result>()
            .HasOne(r => r.CapturedByOrganiser).WithMany(o => o.CapturedResults)
            .HasForeignKey(r => r.CapturedByOrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Event <-> Sponsor link table
        modelBuilder.Entity<EventSponsor>()
            .HasOne(es => es.Event).WithMany(e => e.EventSponsors)
            .HasForeignKey(es => es.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventSponsor>()
            .HasOne(es => es.Sponsor).WithMany(s => s.EventSponsors)
            .HasForeignKey(es => es.SponsorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventSponsor>()
            .HasIndex(es => new { es.EventId, es.SponsorId }).IsUnique();

        // Event 1-M WeatherLogs
        modelBuilder.Entity<WeatherLog>()
            .HasOne(w => w.Event).WithMany(e => e.WeatherLogs)
            .HasForeignKey(w => w.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}