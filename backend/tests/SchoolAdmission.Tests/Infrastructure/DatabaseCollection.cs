namespace SchoolAdmission.Tests.Infrastructure;

// Every test class that touches PostgreSQL must carry [Collection(DatabaseCollection.Name)],
// so admission_test is recreated exactly once before any of them runs.
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
