using Microsoft.EntityFrameworkCore;
using PayFlow.Entities;

namespace PayFlow.Data
{
    public class PayFlowDbContext(DbContextOptions<PayFlowDbContext> options) : DbContext(options)
    {
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayFlowDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
