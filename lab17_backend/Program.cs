using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Reflection.Emit;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationContext>(options => options.UseSqlite("Data Source=usercacheapp.db"));
builder.Services.AddTransient<UserService>();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.MapGet("/user/{id}", async (int id, UserService userService) =>
{
    User? user = await userService.GetUser(id);
    if (user != null) return $"User {user.Name}  Id={user.Id}  Age={user.Age}";
    return "User not found";
});
app.MapGet("/", () => "Inpit Id (1-3)");

app.Run();


public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}
public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) =>
        Database.EnsureCreated();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Oleg", Age = 11 },
                new User { Id = 2, Name = "Soler", Age = 16 },
                new User { Id = 3, Name = "Sasha", Age = 22 }
        );
    }
}
public class UserService
{
    ApplicationContext db;
    IMemoryCache cache;
    public UserService(ApplicationContext context, IMemoryCache memoryCache)
    {
        db = context;
        cache = memoryCache;
    }
    public async Task<User?> GetUser(int id)
    {
        cache.TryGetValue(id, out User? user);
        if (user == null)
        {
            user = await db.Users.FirstOrDefaultAsync(p => p.Id == id);
            if (user != null)
            {
                Console.WriteLine($"{user.Name} извлечен из базы данных");
                cache.Set(user.Id, user, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(20)));
            }
        }
        else
        {
            Console.WriteLine($"{user.Name} извлечен из кэша");
        }
        return user;
    }
}