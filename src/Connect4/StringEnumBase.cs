#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Connect4
{
    [JsonConverter(typeof(StringEnumBaseConverter))]
    public abstract class StringEnumBase
    {
        public static explicit operator string(StringEnumBase stringEnum) => stringEnum.Value;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected StringEnumBase() { } // Value must be set in derived class implicit conversion operator
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Value { get; protected set; }
        public static Dictionary<string, string?> GetAllEnums(Type typeName) => GetFieldsForType(typeName);
        public static string[] GetAllEnumNames(Type typeName) => GetFieldsForType(typeName).Keys.ToArray();
        public static string?[] GetAllEnumValues(Type typeName) => GetFieldsForType(typeName).Values.ToArray();
        public override string ToString() => Value;
        public override bool Equals(object? obj) => obj switch
        {
            StringEnumBase objEnum => objEnum.Value == Value,
            string objString => objString == Value,
            _ => false
        };
        public override int GetHashCode() => Value.GetHashCode();

        private static Dictionary<string, string?> GetFieldsForType(Type typeName)
        {
            return typeName
                      .GetFields(BindingFlags.Public | BindingFlags.Static)
                      .Where(f => f.FieldType == typeof(string))
                      .ToDictionary(f => f.Name,
                                    f => f.GetValue(null) as string);
        }

        // converter that maps StringEnumBase-derived entities to/from JSON, and which works for all (?) collection type usages (e.g. List<T>, etc) 
        public class StringEnumBaseConverter : JsonConverter
        {
            // bootstrapper that (once, and only when called/needed) scans app dependencies for all classes deriving from StringEnumBase
            private static Lazy<IServiceCollection> ServiceCollection { get; } = new Lazy<IServiceCollection>(GetServiceCollection);
            private static Lazy<IServiceProvider> ServiceProvider { get; } = new Lazy<IServiceProvider>(ServiceCollection.Value.BuildServiceProvider);
            private static IServiceCollection GetServiceCollection() =>
                new ServiceCollection()
                    .Scan(scan => // extension method exposed in Scrutor Nuget dependency
                                  //scan.FromApplicationDependencies() // currently throws exception because it tries to load Select.Html.dep which is an unsupported multi-file assembly of Select.HtmlToPdf.dll 
                        scan.FromApplicationDependencies(assembly => assembly.FullName.StartsWith("MI.") || assembly.FullName.StartsWith("taskrunner") || assembly.FullName.StartsWith("miprocessor"))
                            .AddClasses(classes => classes.AssignableTo<StringEnumBase>())
                                .AsSelfWithInterfaces() // AsSelf() might work, too
                                .WithTransientLifetime()
                    );

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                => writer.WriteValue(((StringEnumBase)value).Value);

            // overrides default creation handling, which doesn't work for some generic types.
            public override object? ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                // find the info on the concrete type we want to instantiate
                var typeName = objectType.Name;
                var concreteTypeToUse = ServiceCollection.Value
                    .FirstOrDefault(sc => sc.ImplementationType.Name == typeName)
                    .ImplementationType;

                // kindly ask the service provider to get an instance of that type for us
                var classInstance = (StringEnumBase)ServiceProvider.Value.GetRequiredService(concreteTypeToUse);

                // shove the string value into the Value property and return
                classInstance.Value = (string)reader.Value;
                return classInstance;
            }

            public override bool CanConvert(Type objectType) => typeof(StringEnumBase).IsAssignableFrom(objectType);
        }
    }
}
