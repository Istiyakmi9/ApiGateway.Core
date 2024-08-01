namespace ApiGateway.Core.Interface
{
    public interface IKafkaServiceHandler
    {
        Task ScheduledJobManager();
    }
}
