using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.EventSourceToolkit;

internal static class DiagnosticExtensions
{
    extension(Diagnostic)
    {
        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            BaseTypeDeclarationSyntax node)
        {
            return Diagnostic.Create(descriptor, node.GetLocation(), node.Identifier.ValueText);
        }

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            MethodDeclarationSyntax node)
        {
            return Diagnostic.Create(descriptor, node.GetLocation(), node.Identifier.ValueText);
        }

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            PropertyDeclarationSyntax node)
        {
            return Diagnostic.Create(descriptor, node.GetLocation(), node.Identifier.ValueText);
        }

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            EventDeclarationSyntax node)
        {
            return Diagnostic.Create(descriptor, node.GetLocation(), node.Identifier.ValueText);
        }

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            ConstructorDeclarationSyntax node)
        {
            return Diagnostic.Create(descriptor, node.GetLocation(), node.Identifier.ValueText);
        }
    }
}
