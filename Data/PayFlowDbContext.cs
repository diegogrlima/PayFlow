using Microsoft.EntityFrameworkCore;

namespace PayFlow.Data
{
    public class PayFlowDbContext(DbContextOptions<PayFlowDbContext> options) : DbContext(options)
    {

    }
}
