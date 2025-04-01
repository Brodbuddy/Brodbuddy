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

    public virtual DbSet<PizzaOrder> PizzaOrders { get; set; }

    public virtual DbSet<SourdoughLog> SourdoughLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<PizzaOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pizza_orders_pkey");

            entity.ToTable("pizza_orders");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number");
            entity.Property(e => e.Toppings).HasColumnName("toppings");
        });

        modelBuilder.Entity<SourdoughLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sourdough_logs_pkey");

            entity.ToTable("sourdough_logs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.RisingTime).HasColumnName("rising_time");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
