namespace ResolveOps.Messaging.Options;

public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMqConsumer";

    public ushort PrefetchCount { get; set; } = 10;
    public int RetryLimit { get; set; } = 3;
}
