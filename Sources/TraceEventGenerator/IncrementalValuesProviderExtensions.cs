using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal static class IncrementalValuesProviderExtensions
{
    extension<T>(IncrementalValuesProvider<T?> provider)
    {
        public IncrementalValuesProvider<T> WhereNotNull()
        {
            return provider
                .Where(static item => item is not null)
                .Select(static (item, _) => item!);
        }
    }
}
