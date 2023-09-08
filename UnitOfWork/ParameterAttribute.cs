using System;
using System.Data;

namespace Nacencom.Infrastructure.UnitOfWork
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ParameterAttribute : Attribute
    {
        public string Name { get; set; } = default!;
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public int Size { get; set; } = -1;

        public ParameterAttribute()
        {
        }

        public ParameterAttribute(string name, DbType dbType, ParameterDirection direction, int size)
        {
            Name = name;
            DbType = dbType;
            Direction = direction;
            Size = size;
        }
    }
}
