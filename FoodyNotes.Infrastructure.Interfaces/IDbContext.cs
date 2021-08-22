using System.Threading.Tasks;
using FoodyNotes.Entities.Authentication.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodyNotes.Infrastructure.Interfaces
{
  public interface IDbContext
  {
    DbSet<User> Users { get; }
    Task<int> UpdateAndSaveUser(User user);
    Task<int> SaveChangesAsync();
  }
}