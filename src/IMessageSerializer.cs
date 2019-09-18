namespace El.ServiceBus
{
    public interface IMessageSerializer
    {
        string Serialize<T>(T message);

        T Deserialize<T>(string serializedMessage);
    }
}
