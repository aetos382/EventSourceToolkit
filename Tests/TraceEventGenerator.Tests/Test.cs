using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Aetos.Tracing.Tests;

internal sealed class Test : CSharpSourceGeneratorTest<EventSourceGenerator, DefaultVerifier>;
