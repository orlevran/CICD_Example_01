namespace UsersService.Configurations
{
    public class MongoDBSettings
    {
        public required string ConnectionString { get; set; }
        public required string DatabaseName { get; set; }
        public required string UsersCollectionName { get; set; }
    }
}