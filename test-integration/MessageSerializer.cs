using System.Text;
using El.ServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EL.ServiceBus.IntegrationTests
{
    public class MessageSerializer : IMessageSerializer
    {
        public T Deserialize<T>(string serializedMessage)
        {
            return JsonConvert.DeserializeObject<T>(serializedMessage);
        }

        public byte[] Serialize<T>(T message)
        {
            return Encoding.UTF8.GetBytes(SerializeObject(message));
        }

        private static string SerializeObject<T>(T message)
        {
            return JsonConvert.SerializeObject(message, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            });
        }
    }
}