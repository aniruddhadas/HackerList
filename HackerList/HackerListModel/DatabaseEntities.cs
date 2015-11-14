namespace HackerListModel
{
    using System.Data.Entity;
    using System.Data.Entity.SqlServer;
    
    public class DatabaseEntities : DbContext
    {
        public static SqlProviderServices ProviderInstance = SqlProviderServices.Instance;

        static DatabaseEntities()
        {
            Database.SetInitializer<DatabaseEntities>(new NullDatabaseInitializer<DatabaseEntities>());
        }

        public DatabaseEntities(string connectionString) : base(connectionString)
        {
        }

        public DatabaseEntities() : base("name=DatabaseEntities")
        {
            this.Database.CompatibleWithModel(throwIfNoMetadata: false);
        }

        public DbSet<List> Lists { get; set; }

        public DbSet<ListEntry> ListEntries { get; set; }
    }
}
