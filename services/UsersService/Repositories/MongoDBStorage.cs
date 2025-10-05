using MongoDB.Driver;
using UsersService.Models;
using UsersService.Configurations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace UsersService.Repositories
{
    public class MongoDBStorage : IDatabaseStorage
    {
        private readonly IMongoCollection<User> UsersCollection;
        private readonly IMongoDatabase db;
        private readonly MongoDBSettings settings;

        public MongoDBStorage(IMongoDatabase db, IOptions<MongoDBSettings> settings)
        {
            this.db = db;
            this.settings = settings.Value;
            UsersCollection = db.GetCollection<User>(this.settings.UsersCollectionName);
        }

        public async Task CreateUserAsync(User user)
        {
            await UsersCollection.InsertOneAsync(user);
        }

        public async Task<User?> GetUserByIdentifierAsync(string identifier)
        {
            var filter = Builders<User>.Filter.Eq("_id", ObjectId.Parse(identifier));
            return await UsersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var filter = Builders<User>.Filter.Eq("Email", email);
            return await UsersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User> UpdateUserAsync(string id, User user)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            await UsersCollection.ReplaceOneAsync(filter, user);
            return user;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            var result = await UsersCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
    }
}