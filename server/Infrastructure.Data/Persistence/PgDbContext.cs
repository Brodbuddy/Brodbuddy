using System;
using System.Collections.Generic;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Persistence;

public partial class PgDbContext : DbContext
{
    public PgDbContext(DbContextOptions<PgDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<DeviceRegistry> DeviceRegistries { get; set; }

    public virtual DbSet<DeviceTelemetry> DeviceTelemetries { get; set; }

    public virtual DbSet<Feature> Features { get; set; }

    public virtual DbSet<OneTimePassword> OneTimePasswords { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<TokenContext> TokenContexts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VerificationContext> VerificationContexts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("devices_pkey");

            entity.ToTable("devices");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Browser)
                .HasMaxLength(255)
                .HasColumnName("browser");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastSeenAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("last_seen_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Os)
                .HasMaxLength(255)
                .HasColumnName("os");
        });

        modelBuilder.Entity<DeviceRegistry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("device_registry_pkey");

            entity.ToTable("device_registry");

            entity.HasIndex(e => new { e.UserId, e.DeviceId }, "device_registry_user_id_device_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceRegistries)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("device_registry_device_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.DeviceRegistries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("device_registry_user_id_fkey");
        });

        modelBuilder.Entity<DeviceTelemetry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("device_telemetry_pkey");

            entity.ToTable("device_telemetry");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId)
                .HasMaxLength(255)
                .HasColumnName("device_id");
            entity.Property(e => e.Distance).HasColumnName("distance");
            entity.Property(e => e.RisePercentage).HasColumnName("rise_percentage");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        });

        modelBuilder.Entity<Feature>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("features_pkey");

            entity.ToTable("features");

            entity.HasIndex(e => e.Name, "features_name_key").IsUnique();

            entity.HasIndex(e => e.Name, "idx_features_name");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(false)
                .HasColumnName("is_enabled");
            entity.Property(e => e.LastModifiedAt).HasColumnName("last_modified_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<OneTimePassword>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("one_time_passwords_pkey");

            entity.ToTable("one_time_passwords");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.Token).HasColumnName("token");

            entity.HasOne(d => d.ReplacedByToken).WithMany(p => p.InverseReplacedByToken)
                .HasForeignKey(d => d.ReplacedByTokenId)
                .HasConstraintName("refresh_tokens_replaced_by_token_id_fkey");
        });

        modelBuilder.Entity<TokenContext>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("token_contexts_pkey");

            entity.ToTable("token_contexts");

            entity.HasIndex(e => e.RefreshTokenId, "token_contexts_refresh_token_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false)
                .HasColumnName("is_revoked");
            entity.Property(e => e.RefreshTokenId).HasColumnName("refresh_token_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Device).WithMany(p => p.TokenContexts)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("token_contexts_device_id_fkey");

            entity.HasOne(d => d.RefreshToken).WithOne(p => p.TokenContext)
                .HasForeignKey<TokenContext>(d => d.RefreshTokenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("token_contexts_refresh_token_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TokenContexts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("token_contexts_user_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
        });

        modelBuilder.Entity<VerificationContext>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("verification_contexts_pkey");

            entity.ToTable("verification_contexts");

            entity.HasIndex(e => e.UserId, "idx_verification_contexts_user_id");

            entity.HasIndex(e => new { e.UserId, e.OtpId }, "verification_contexts_user_id_otp_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OtpId).HasColumnName("otp_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Otp).WithMany(p => p.VerificationContexts)
                .HasForeignKey(d => d.OtpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("verification_contexts_otp_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.VerificationContexts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("verification_contexts_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
