using AutoMapper;
using System.Reflection;

namespace Nacencom.Infrastructure.Mapper
{
    internal class MappingProfile : Profile
    {
        private readonly IEnumerable<Type> _types;

        public MappingProfile()
        {

        }

        public MappingProfile(params Assembly[] assemblies)
        {
            _types = assemblies.SelectMany(x => x.GetTypes());
            ApplyIMap();
            ApplyIMapFrom();
            ApplyIMapTo();
            ApplyIMapFromTo();
        }

        private void ApplyIMap()
        {
            var instances = _types
                .Where(t => typeof(IMap).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(Activator.CreateInstance)
                .Cast<IMap>();

            foreach (var instance in instances)
            {
                instance.Mapping(this);
            }
        }

        private void ApplyIMapFrom()
        {
            foreach (var (Source, Target) in TypeFilter(_types, typeof(IMapFrom<>)))
            {
                CreateMap(Source, Target);
            }
        }

        private void ApplyIMapTo()
        {
            foreach (var (Source, Target) in TypeFilter(_types, typeof(IMapTo<>)))
            {
                CreateMap(Target, Source);
            }
        }

        private void ApplyIMapFromTo()
        {
            foreach (var (Source, Target) in TypeFilter(_types, typeof(IMapFromTo<>)))
            {
                CreateMap(Source, Target).ReverseMap();
            }
        }

        private static IEnumerable<(Type Source, Type Target)> TypeFilter(IEnumerable<Type> types, Type fromType)
        {
            foreach (var type in types)
            {
                foreach (var imapType in type.GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == fromType))
                {
                    var sourceType = imapType.GetGenericArguments()[0];
                    if (sourceType == null) continue;
                    yield return (sourceType, type);
                }
            }
        }
    }
}
