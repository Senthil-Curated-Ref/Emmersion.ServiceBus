namespace El.ServiceBus
{
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T message);

        T Deserialize<T>(string serializedMessage);
    }
}
