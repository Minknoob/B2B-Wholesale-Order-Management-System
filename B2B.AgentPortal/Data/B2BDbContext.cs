using B2B.AgentPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace B2B.AgentPortal.Data;

public class B2BDbContext : DbContext
{
    public B2BDbContext(DbContextOptions<B2BDbContext> options)
        : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();

    public DbSet<Agent> Agents => Set<Agent>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Item", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Item_Price", "[Price] >= 0");
                table.HasCheckConstraint("CK_Item_StockQuantity", "[StockQuantity] >= 0");
            });

            entity.HasKey(e => e.ItemID).HasName("PK_Item");

            entity.Property(e => e.ItemID)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ItemName)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(e => e.Size)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0);
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.ToTable("Agent", "dbo");

            entity.HasKey(e => e.AgentID).HasName("PK_Agent");

            entity.Property(e => e.AgentID)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.AgentName)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.Phone)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Order", "dbo", table =>
            {
                table.HasCheckConstraint(
                    "CK_Order_OrderStatus",
                    "[OrderStatus] IN (N'Pending',N'Confirmed',N'Shipped',N'Delivered',N'Cancelled')");
            });

            entity.HasKey(e => e.OrderID).HasName("PK_Order");

            entity.Property(e => e.OrderID)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.OrderDate)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("(SYSDATETIME())");

            entity.Property(e => e.OrderStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(OrderStatuses.Pending);

            entity.HasOne(e => e.Agent)
                .WithMany(a => a.Orders)
                .HasForeignKey(e => e.AgentID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Order_Agent");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetail", "dbo", table =>
            {
                table.HasCheckConstraint("CK_OrderDetail_Quantity", "[Quantity] > 0");
                table.HasCheckConstraint("CK_OrderDetail_UnitAmount", "[UnitAmount] >= 0");
            });

            entity.HasKey(e => e.ID).HasName("PK_OrderDetail");

            entity.Property(e => e.ID)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UnitAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(e => e.OrderID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_OrderDetail_Order");

            entity.HasOne(e => e.Item)
                .WithMany(i => i.OrderDetails)
                .HasForeignKey(e => e.ItemID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_OrderDetail_Item");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Users_Role", "[Role] IN (N'Admin',N'Agent')");
                table.HasCheckConstraint("CK_Users_FailedLoginCount", "[FailedLoginCount] >= 0");
                table.HasCheckConstraint(
                    "CK_Users_RoleAgent",
                    "([Role] = N'Admin' AND [AgentID] IS NULL) OR ([Role] = N'Agent' AND [AgentID] IS NOT NULL)");
            });

            entity.HasKey(e => e.UserID).HasName("PK_Users");

            entity.Property(e => e.UserID)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(e => e.IsLocked)
                .HasDefaultValue(false);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.FailedLoginCount)
                .HasDefaultValue(0);

            entity.HasIndex(e => e.UserName, "UQ_Users_UserName")
                .IsUnique();

            entity.HasIndex(e => e.Email, "UQ_Users_Email")
                .IsUnique();

            entity.HasOne(e => e.Agent)
                .WithMany(a => a.Users)
                .HasForeignKey(e => e.AgentID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Users_Agent");
        });
    }
}
