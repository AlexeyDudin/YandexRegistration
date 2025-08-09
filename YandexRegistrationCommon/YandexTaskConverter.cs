using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YandexRegistrationCommon.Infrastructure.APIHelper;
using YandexRegistrationModel;

namespace YandexRegistrationCommon
{
    public class YandexTaskConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ISmsActivator);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // Load the JSON into a JObject
            var jsonObject = JObject.Load(reader);

            if (!string.IsNullOrEmpty(jsonObject["PhoneNumber"]?.Value<string>()))
            {
                return jsonObject.ToObject<ManualSmsHelper>(serializer);
            }
            else if (!string.IsNullOrEmpty(jsonObject["MainUrl"]?.Value<string>()))
            {
                return jsonObject.ToObject<VacSMSHelper>(serializer);
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
