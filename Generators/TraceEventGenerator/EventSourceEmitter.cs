using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using Aetos.Tracing.Models;

namespace Aetos.Tracing;

internal static class EventSourceEmitter
{
    public static void EmitEventSourceMethod(
        SourceProductionContext context,
        EventSourceMethodInfo methodInfo)
    {
        var codeBuilder = new IndentedStringBuilder();

        if (methodInfo.NamespaceSegments.Count != 0)
        {
            var namespaceName = string.Join(".", methodInfo.NamespaceSegments);
            codeBuilder.AppendLine($"namespace {namespaceName}");
            codeBuilder.AppendLine("{");
            codeBuilder.Indent();
        }

        foreach (var containingType in methodInfo.ContainingTypes)
        {
            codeBuilder.AppendLine($"partial {containingType.KindKeyword} {containingType.Name}");
            codeBuilder.AppendLine("{");
            codeBuilder.Indent();
        }

        codeBuilder.AppendLine($"{methodInfo.AccessibilityKeyword} partial void {methodInfo.MethodName}(");
        codeBuilder.Indent();

        var parameters = methodInfo.Parameters;
        var lastParameterIndex = parameters.Count - 1;
        for (var i = 0; i <= lastParameterIndex; ++i)
        {
            var parameter = parameters[i];
            var delimiter = i < lastParameterIndex ? "," : ")";

            codeBuilder.AppendLine($"{parameter.FullyQualifiedTypeName} {parameter.Name}{delimiter}");
        }

        codeBuilder.Unindent();
        codeBuilder.AppendLine("{");
        codeBuilder.Indent();

        while (codeBuilder.IndentationLevel != 0)
        {
            codeBuilder.Unindent();
            codeBuilder.AppendLine("}");
        }

        var code = codeBuilder.ToString();

        var fileNameSegment = new List<string>();

        fileNameSegment.AddRange(methodInfo.NamespaceSegments);
        fileNameSegment.AddRange(methodInfo.ContainingTypes.Select(static x => x.Name));
        fileNameSegment.Add(methodInfo.MethodName);
        fileNameSegment.Add("g.cs");

        var fileName = string.Join(".", fileNameSegment);

        context.AddSource(fileName, code);
    }
}
