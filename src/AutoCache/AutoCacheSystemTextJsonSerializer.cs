using System;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Json;
using Volo.Abp.Json.SystemTextJson;

namespace AutoCache;

public class AutoCacheSystemTextJsonSerializer : IJsonSerializer, ITransientDependency
{
    private readonly IOptions<AbpSystemTextJsonSerializerModifiersOptions> _modifiersOptions;
    private readonly AbpSystemTextJsonSerializer _abpSystemTextJsonSerializer;
    protected AbpSystemTextJsonSerializerOptions Options { get; }
    private readonly static Type AutoCacheWrapperType = typeof(AutoCacheWrapper<>);

    public AutoCacheSystemTextJsonSerializer(IOptions<AbpSystemTextJsonSerializerOptions> options,
        IOptions<AbpSystemTextJsonSerializerModifiersOptions> modifiersOptions,
        AbpSystemTextJsonSerializer abpSystemTextJsonSerializer)
    {
        _modifiersOptions = modifiersOptions;
        _abpSystemTextJsonSerializer = abpSystemTextJsonSerializer;
        Options = options.Value;
    }

    public string Serialize(object obj, bool camelCase = true, bool indented = false)
    {
        if (obj != null)
        {
            var objType = obj.GetType();
            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == AutoCacheWrapperType)
            {
                return JsonSerializer.Serialize(obj, CreateJsonSerializerOptions(camelCase, indented));
            }
        }

        return _abpSystemTextJsonSerializer.Serialize(obj, camelCase, indented);
    }

    public T Deserialize<T>(string jsonString, bool camelCase = true)
    {
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == AutoCacheWrapperType)
        {
            return JsonSerializer.Deserialize<T>(jsonString, CreateJsonSerializerOptions(camelCase))!;
        }

        return _abpSystemTextJsonSerializer.Deserialize<T>(jsonString, camelCase);
    }

    public object Deserialize(Type type, string jsonString, bool camelCase = true)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == AutoCacheWrapperType)
        {
            return JsonSerializer.Deserialize(jsonString, type, CreateJsonSerializerOptions(camelCase))!;
        }

        return _abpSystemTextJsonSerializer.Deserialize(type, jsonString, camelCase);
    }

    private static readonly ConcurrentDictionary<object, JsonSerializerOptions> JsonSerializerOptionsCache = new();

    protected virtual JsonSerializerOptions CreateJsonSerializerOptions(bool camelCase = true, bool indented = false)
    {
        return JsonSerializerOptionsCache.GetOrAdd(new { camelCase, indented, Options.JsonSerializerOptions },
            _ => new JsonSerializerOptions(Options.JsonSerializerOptions)
            {
                PropertyNamingPolicy = camelCase ? JsonNamingPolicy.CamelCase : null,
                WriteIndented = indented,
                TypeInfoResolver = new AutoCacheAbpIoJsonTypeInfoResolver(_modifiersOptions)
            });
    }
}