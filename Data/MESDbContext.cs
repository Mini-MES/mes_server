using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Data
{
    public class MESDbContext : DbContext
    {
        public MESDbContext(DbContextOptions<MESDbContext> options) : base(options)
        {
        }

        // Master Data
        public DbSet<User> Users { get; set; }
        public DbSet<ProcessMaster> ProcessMasters { get; set; }
        public DbSet<ProductMaster> ProductMasters { get; set; }
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<BadReasonMaster> BadReasonMasters { get; set; }
        public DbSet<BOM> BOMs { get; set; }

        // Production
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<Lot> Lots { get; set; }
        public DbSet<Tool> Tools { get; set; }

        // History
        public DbSet<Performance> Performances { get; set; }
        public DbSet<ToolHistory> ToolHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // BOM 복합키 설정
            modelBuilder.Entity<BOM>()
                .HasKey(b => new { b.ProductID, b.MaterialID });

            // BOM 관계 설정
            modelBuilder.Entity<BOM>()
                .HasOne(b => b.Product)
                .WithMany(p => p.BOMs) // 양방향 설정
                .HasForeignKey(b => b.ProductID) 
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<BOM>()
                .HasOne(b => b.Material)       
                .WithMany()                   
                .HasForeignKey(b => b.MaterialID) 
                .OnDelete(DeleteBehavior.Restrict);

            // Performance 관계 설정
            modelBuilder.Entity<Performance>()
                .HasOne(p => p.Lot)
                .WithMany()
                .HasForeignKey(p => p.LotID)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<Performance>()
                .HasOne(p => p.Process)
                .WithMany()
                .HasForeignKey(p => p.ProcessID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Performance>()
                .HasOne(p => p.Tool)
                .WithMany()
                .HasForeignKey(p => p.ToolID)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<Performance>()
                .HasOne(p => p.BadReason)
                .WithMany()
                .HasForeignKey(p => p.ReasonCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Performance>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserID)
                .OnDelete(DeleteBehavior.Restrict); 

            // ToolHistory 관계 설정
            modelBuilder.Entity<ToolHistory>()
                .HasOne(t => t.Tool)
                .WithMany()
                .HasForeignKey(t => t.ToolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Lot 관계 설정
            modelBuilder.Entity<Lot>()
                .HasOne(l => l.WorkOrder)
                .WithMany(wo => wo.Lots) // 양방향 설정
                .HasForeignKey(l => l.OrderID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lot>()
                .HasOne(l => l.CurrentProcess)
                .WithMany()
                .HasForeignKey(l => l.CurrentProcessID)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkOrder 관계 설정
            modelBuilder.Entity<WorkOrder>()
                .HasOne(wo => wo.Product)
                .WithMany()
                .HasForeignKey(wo => wo.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}
