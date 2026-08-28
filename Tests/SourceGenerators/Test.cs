using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Aetos.EventSourceToolkit.SourceGenerators;

namespace Aetos.EventSourceToolkit.Tests.SourceGenerators;

internal sealed class Test : CSharpSourceGeneratorTest<EventSourceGenerator, DefaultVerifier>;
