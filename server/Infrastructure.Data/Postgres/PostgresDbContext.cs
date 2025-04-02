using System;
using System.Collections.Generic;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Postgres;

public partial class PostgresDbContext : DbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Onetimepassword> Onetimepasswords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Onetimepassword>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("onetimepassword_pkey");

            entity.ToTable("onetimepassword");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("createdat");
            entity.Property(e => e.Expiresat)
                .HasDefaultValueSql("(CURRENT_TIMESTAMP + '00:15:00'::interval)")
                .HasColumnName("expiresat");
            entity.Property(e => e.Isused)
                .HasDefaultValue(false)
                .HasColumnName("isused");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
