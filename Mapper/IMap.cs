using AutoMapper;

namespace Nacencom.Infrastructure.Mapper
{
    public interface IMap
    {
        void Mapping(Profile profile);
    }

    public interface IMapFrom<T>
    {
    }

    public interface IMapTo<T>
    {
    }

    public interface IMapFromTo<T>
    {
    }
}
