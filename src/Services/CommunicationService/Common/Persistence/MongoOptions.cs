namespace CommunicationService.Common.Persistence;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";
    public const string ConnectionStringName = "Mongo";
    public string Database { get; set; } = string.Empty;
}
