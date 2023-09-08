using System.Reflection;

namespace Nacencom.Infrastructure.Utils
{
    public class AssemblyUtils
    {
        public static Assembly[] GetAssemblies(params string[] prefix)
        {
            if (prefix?.Any() != true)
                prefix = new[] { "Nacencom" };

            return _().ToArray(); IEnumerable<Assembly> _()
            {
                var asm = Assembly.GetEntryAssembly()!;
                yield return asm;
                foreach (var an in asm.GetReferencedAssemblies().Where(an => prefix.Any(p => an.FullName.StartsWith(p))))
                    yield return Assembly.Load(an);
            }
        }
    }
}
