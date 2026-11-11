using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.AddressableAssets;

namespace Framework.JsonConverter
{
    /// <summary>
    /// AssetReference 的 JSON 转换器
    /// 只序列化和反序列化 AssetGUID
    /// </summary>
    public class AssetReferenceConverter : JsonConverter<AssetReference>
    {
        public override void WriteJson(JsonWriter writer, AssetReference value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("assetGUID");
            writer.WriteValue(value.AssetGUID);
            writer.WriteEndObject();
        }

        public override AssetReference ReadJson(JsonReader reader, Type objectType, AssetReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObject = JObject.Load(reader);
            var assetGUID = jObject["assetGUID"]?.Value<string>();

            if (string.IsNullOrEmpty(assetGUID))
            {
                return null;
            }

            // 创建 AssetReference 实例
            var assetReference = new AssetReference(assetGUID);
            return assetReference;
        }
    }

    /// <summary>
    /// AssetReferenceT{T} 的泛型 JSON 转换器
    /// </summary>
    public class AssetReferenceTConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // 检查是否是 AssetReferenceT<> 的实例
            if (!objectType.IsGenericType)
                return false;

            var genericTypeDef = objectType.GetGenericTypeDefinition();
            return genericTypeDef == typeof(AssetReferenceT<>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var assetReference = value as AssetReference;
            if (assetReference == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("assetGUID");
            writer.WriteValue(assetReference.AssetGUID);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObject = JObject.Load(reader);
            var assetGUID = jObject["assetGUID"]?.Value<string>();

            if (string.IsNullOrEmpty(assetGUID))
            {
                return null;
            }

            // 使用反射创建 AssetReferenceT<T> 实例
            var instance = Activator.CreateInstance(objectType, assetGUID);
            return instance;
        }
    }
}
