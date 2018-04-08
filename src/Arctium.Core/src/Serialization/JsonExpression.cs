// Copyright (c) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aq.ExpressionJsonSerializer;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace Arctium.Core.Serialization
{
    public static class JsonExpression
    {
        static JsonSerializerSettings serializerSettings;

        static JsonExpression()
        {
            serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
        }

        public static void AddConverter(Assembly typeAssembly)
        {
            serializerSettings.Converters.Add(new ExpressionJsonConverter(typeAssembly));
        }

        public static void AddConverter(Type type)
        {
            serializerSettings.Converters.Add(new ExpressionJsonConverter(Assembly.GetAssembly(type)));
        }

        public static string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, serializerSettings);

        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, serializerSettings);
    }
}
