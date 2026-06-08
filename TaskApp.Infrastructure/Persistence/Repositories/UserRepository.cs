using MongoDB.Driver;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Interfaces.Repositories;
using TaskApp.Infrastructure.Persistence;

namespace TaskApp.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public UserRepository(MongoDbContext context)
    {
        _collection = context.Users;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        List<User> results = await _collection.Find(Builders<User>.Filter.Empty).ToListAsync();
        return results;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, id);
        User? result = await _collection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Email, email);
        User? result = await _collection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Username, username);
        User? result = await _collection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public async Task<User> CreateAsync(User user)
    {
        await _collection.InsertOneAsync(user);
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
        await _collection.ReplaceOneAsync(filter, user);
        return user;
    }
}
