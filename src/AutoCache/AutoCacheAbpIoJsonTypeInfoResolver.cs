using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using Volo.Abp.Json.SystemTextJson;

namespace AutoCache;

public class AutoCacheAbpIoJsonTypeInfoResolver : AbpDefaultJsonTypeInfoResolver
{
    private static readonly PropertyInfo MemberNameProperty = typeof(JsonPropertyInfo).GetProperty("MemberName",
        BindingFlags.NonPublic | BindingFlags.Instance)!;

    public AutoCacheAbpIoJsonTypeInfoResolver(IOptions<AbpSystemTextJsonSerializerModifiersOptions> options) : base(options)
    {
        Modifiers.Add(jsonTypeInfo =>
        {
            if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return;
            }

            foreach (var property in jsonTypeInfo.Properties)
            {
                if (property.Set != null)
                {
                    continue;
                }

                var memberName = MemberNameProperty?.GetValue(property) as string ?? property.Name;

                var propertyInfo = jsonTypeInfo.Type.GetProperty(memberName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (propertyInfo != null)
                {
                    if (propertyInfo.SetMethod != null)
                    {
                        property.Set = propertyInfo.SetValue;
                    }

                    continue;
                }

                var propertyInfos = jsonTypeInfo.Type.GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                var matchedProperties = propertyInfos
                    .Where(pi => pi.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matchedProperties.Length == 1 && matchedProperties[0].SetMethod != null)
                {
                    property.Set = matchedProperties[0].SetValue;
                }
            }

            var emptyCtor = jsonTypeInfo.Type.GetConstructor(BindingFlags.Instance |
                                                             BindingFlags.Public |
                                                             BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (emptyCtor != null)
            {
                jsonTypeInfo.CreateObject = () => emptyCtor.Invoke(null);
            }
        });
    }
}