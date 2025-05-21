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

    public virtual DbSet<Feature> Features { get; set; }

    public virtual DbSet<FeatureUser> FeatureUsers { get; set; }

    public virtual DbSet<OneTimePassword> OneTimePasswords { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SourdoughAnalyzer> SourdoughAnalyzers { get; set; }

    public virtual DbSet<TokenContext> TokenContexts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAnalyzer> UserAnalyzers { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

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
            entity.Property(e => e.CreatedByIp)
                .HasMaxLength(45)
                .HasColumnName("created_by_ip");
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
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
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
            entity.Property(e => e.Fingerprint)
                .HasMaxLength(255)
                .HasColumnName("fingerprint");
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
            entity.Property(e => e.RolloutPercentage).HasColumnName("rollout_percentage");
        });

        modelBuilder.Entity<FeatureUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("feature_users_pkey");

            entity.ToTable("feature_users");

            entity.HasIndex(e => new { e.FeatureId, e.UserId }, "feature_users_feature_id_user_id_key").IsUnique();

            entity.HasIndex(e => e.FeatureId, "idx_feature_users_feature_id");

            entity.HasIndex(e => e.UserId, "idx_feature_users_user_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.FeatureId).HasColumnName("feature_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Feature).WithMany(p => p.FeatureUsers)
                .HasForeignKey(d => d.FeatureId)
                .HasConstraintName("feature_users_feature_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.FeatureUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("feature_users_user_id_fkey");
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

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "idx_roles_name");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<SourdoughAnalyzer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sourdough_analyzers_pkey");

            entity.ToTable("sourdough_analyzers");

            entity.HasIndex(e => e.ActivationCode, "idx_sourdough_analyzers_activation_code");

            entity.HasIndex(e => e.MacAddress, "idx_sourdough_analyzers_mac_address");

            entity.HasIndex(e => e.ActivationCode, "sourdough_analyzers_activation_code_key").IsUnique();

            entity.HasIndex(e => e.MacAddress, "sourdough_analyzers_mac_address_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActivatedAt).HasColumnName("activated_at");
            entity.Property(e => e.ActivationCode)
                .HasMaxLength(12)
                .HasColumnName("activation_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.FirmwareVersion)
                .HasMaxLength(50)
                .HasColumnName("firmware_version");
            entity.Property(e => e.IsActivated)
                .HasDefaultValue(false)
                .HasColumnName("is_activated");
            entity.Property(e => e.LastSeen).HasColumnName("last_seen");
            entity.Property(e => e.MacAddress)
                .HasMaxLength(17)
                .HasColumnName("mac_address");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
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

        modelBuilder.Entity<UserAnalyzer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_analyzers_pkey");

            entity.ToTable("user_analyzers");

            entity.HasIndex(e => e.AnalyzerId, "idx_user_analyzers_analyzer_id");

            entity.HasIndex(e => e.UserId, "idx_user_analyzers_user_id");

            entity.HasIndex(e => new { e.UserId, e.AnalyzerId }, "user_analyzers_user_id_analyzer_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AnalyzerId).HasColumnName("analyzer_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.IsOwner)
                .HasDefaultValue(true)
                .HasColumnName("is_owner");
            entity.Property(e => e.Nickname)
                .HasMaxLength(255)
                .HasColumnName("nickname");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Analyzer).WithMany(p => p.UserAnalyzers)
                .HasForeignKey(d => d.AnalyzerId)
                .HasConstraintName("user_analyzers_analyzer_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserAnalyzers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_analyzers_user_id_fkey");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_roles_pkey");

            entity.ToTable("user_roles");

            entity.HasIndex(e => e.RoleId, "idx_user_roles_role_id");

            entity.HasIndex(e => e.UserId, "idx_user_roles_user_id");

            entity.HasIndex(e => new { e.UserId, e.RoleId }, "user_roles_user_id_role_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UserRoleCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("user_roles_created_by_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("user_roles_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_roles_user_id_fkey");
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
