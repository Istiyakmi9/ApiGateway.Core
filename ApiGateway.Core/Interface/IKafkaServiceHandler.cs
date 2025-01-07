using Confluent.Kafka;

namespace ApiGateway.Core.Interface
{
    public interface IKafkaServiceHandler
    {
        Task DailyJobManager(ConsumeResult<Ignore, string> result);
    }
}
