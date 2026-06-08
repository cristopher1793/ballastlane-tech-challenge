using MongoDB.Driver;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Interfaces.Repositories;
using TaskApp.Infrastructure.Persistence;

namespace TaskApp.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly IMongoCollection<TaskItem> _collection;

    public TaskRepository(MongoDbContext context)
    {
        _collection = context.Tasks;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        List<TaskItem> results = await _collection.Find(Builders<TaskItem>.Filter.Empty).ToListAsync();
        return results;
    }

    public async Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(string userId)
    {
        FilterDefinition<TaskItem> filter = Builders<TaskItem>.Filter.Eq(t => t.UserId, userId);
        List<TaskItem> results = await _collection.Find(filter).ToListAsync();
        return results;
    }

    public async Task<IEnumerable<string>> GetAllLabelsAsync()
    {
        List<TaskItem> tasks = await _collection.Find(Builders<TaskItem>.Filter.Empty).ToListAsync();
        return tasks.SelectMany(t => t.Labels).Distinct().OrderBy(l => l).ToList();
    }

    public async Task<IEnumerable<string>> GetAllLabelsByUserIdAsync(string userId)
    {
        FilterDefinition<TaskItem> filter = Builders<TaskItem>.Filter.Eq(t => t.UserId, userId);
        List<TaskItem> tasks = await _collection.Find(filter).ToListAsync();
        return tasks.SelectMany(t => t.Labels).Distinct().OrderBy(l => l).ToList();
    }

    public async Task<TaskItem?> GetByIdAsync(string id)
    {
        FilterDefinition<TaskItem> filter = Builders<TaskItem>.Filter.Eq(t => t.Id, id);
        TaskItem? result = await _collection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        await _collection.InsertOneAsync(task);
        return task;
    }

    public async Task<TaskItem> UpdateAsync(TaskItem task)
    {
        FilterDefinition<TaskItem> filter = Builders<TaskItem>.Filter.Eq(t => t.Id, task.Id);
        await _collection.ReplaceOneAsync(filter, task);
        return task;
    }

    public async Task DeleteAsync(string id)
    {
        FilterDefinition<TaskItem> filter = Builders<TaskItem>.Filter.Eq(t => t.Id, id);
        await _collection.DeleteOneAsync(filter);
    }
}
