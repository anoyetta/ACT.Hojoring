using System.Collections.Generic;
using System.Linq;
using RazorEngine.Compilation;
using RazorEngine.Compilation.ReferenceResolver;

public class RazorReferenceResolver : IReferenceResolver
{
    public IEnumerable<CompilerReference> GetReferences(
        TypeContext context,
        IEnumerable<CompilerReference> includeAssemblies = null)
    {
        yield return CompilerReference.From(this.GetType().Assembly);

        var refs = new UseCurrentAssembliesReferenceResolver()
            .GetReferences(context, includeAssemblies)
            .Where(f => !f.GetFile().EndsWith(".winmd"));

        foreach (var r in refs)
        {
            yield return r;
        }
    }
}
