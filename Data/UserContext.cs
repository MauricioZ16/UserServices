using Microsoft.EntityFrameworkCore;
using UserServices.Model;

namespace UserServices.Data
{
    public class UserContext : DbContext
    {
        public UserContext() { }
        public UserContext(DbContextOptions<UserContext> opitions) : base(opitions) { }
        public DbSet<User> Users { get; set; }
       
    }
}
