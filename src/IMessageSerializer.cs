using System.Text.Json;

namespace EL.ServiceBus
{
    public interface IMessageSerializer
    {
        string Serialize<T>(T message);

        T Deserialize<T>(string serializedMessage);
    }

    internal class MessageSerializer : IMessageSerializer
    {
        private JsonSerializerOptions jsonOptions;

        public MessageSerializer()
        {
            jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public string Serialize<T>(T message)
        {
            return JsonSerializer.Serialize(message, jsonOptions);
        }

        public T Deserialize<T>(string serializedMessage)
        {
            return JsonSerializer.Deserialize<T>(serializedMessage, jsonOptions);
        }
    }
}
