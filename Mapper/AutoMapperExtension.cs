using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace Nacencom.Infrastructure.Mapper
{
    public static class AutoMapperExtension
    {
        private static IMapper _mapper;

        internal static IServiceCollection AddMapperInstance(this IServiceCollection services, IMapper mapper)
        {
            _mapper = mapper;
            return services;
        }

        public static T MapTo<T>(this object source)
        {
            return _mapper.Map<T>(source);
        }

        public static object MapTo(this object source, Type destinationType)
        {
            return _mapper.Map(source, source.GetType(), destinationType);
        }

        public static T MapTo<T>(this object source, Action<IMappingOperationOptions> opts)
        {
            return _mapper.Map<T>(source, opts);
        }

        public static T MapTo<T>(this object source, T dest)
        {
            return _mapper.Map(source, dest);
        }

        public static IQueryable<T> ProjectTo<T>(this IQueryable source, object parameters = null, params Expression<Func<T, object>>[] membersToExpand)
        {
            return _mapper.ProjectTo(source, parameters, membersToExpand);
        }

        public static IQueryable<T> ProjectTo<T>(this IQueryable source, IDictionary<string, object> parameters, params string[] membersToExpand)
        {
            return _mapper.ProjectTo<T>(source, parameters, membersToExpand);
        }
    }
}
