using Dapper;
using System.Data;
using System.Reflection;

namespace Nacencom.Infrastructure.UnitOfWork
{
    public abstract class CommandQuery
    {
        /// <summary>
        /// The command timeout (in seconds).
        /// </summary>
        protected virtual int? CommandTimeout => null;

        /// <summary>
        /// The type of command to execute. Default is CommandType.StoredProcedure
        /// </summary>
        protected virtual CommandType CommandType => CommandType.Text;

        protected virtual object GetParams()
        {
            var props = GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(t => (Prop: t, Attribute: t.GetCustomAttribute<ParameterAttribute>()))
                .Where(t => t.Attribute != null)
                .Select(t => (t.Prop, Attribute: t.Attribute!))
                .ToList();
            if (props.Count == 0) return null;
            var parameters = new DynamicParameters();
            foreach (var (prop, attribute) in props)
            {
                var name = attribute.Name ?? prop.Name;
                var value = prop.GetValue(this, null);
                if (attribute.Size >= 0)
                {
                    parameters.Add(name, value, attribute.DbType, attribute.Direction, attribute.Size);
                }
                else
                {
                    parameters.Add(name, value);
                }
            }
            return parameters;
        }

        protected abstract string CommandText { get; }
    }
}
