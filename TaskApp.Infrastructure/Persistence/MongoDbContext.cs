using MongoDB.Driver;
using TaskApp.Domain.Entities;

namespace TaskApp.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        MongoClient client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<TaskItem> Tasks => _database.GetCollection<TaskItem>("tasks");
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
}
