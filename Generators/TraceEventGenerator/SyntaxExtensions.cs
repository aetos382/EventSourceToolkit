using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.Tracing;

internal static class SyntaxExtensions
{
    extension(SyntaxNode node)
    {
        public NodeLocationInfo CreateLocationInfo()
        {
            var location = node.GetLocation();

            return new NodeLocationInfo(node.Span, location.GetLineSpan(), location.GetMappedLineSpan());
        }
    }

    extension(MemberDeclarationSyntax node)
    {
        public bool HasPartialModifier => node.Modifiers.Any(SyntaxKind.PartialKeyword);

        public bool HasFileModifier => node.Modifiers.Any(SyntaxKind.FileKeyword);
    }

    extension(TypeDeclarationSyntax node)
    {
        public IEnumerable<MethodDeclarationSyntax> GetMethods()
        {
            foreach (var member in node.Members)
            {
                if (member.IsKind(SyntaxKind.MethodDeclaration))
                {
                    yield return (MethodDeclarationSyntax)member;
                }
            }
        }
    }

    extension(MethodDeclarationSyntax node)
    {
        public bool ReturnsVoid
        {
            get
            {
                var returnType = node.ReturnType;
                if (!returnType.IsKind(SyntaxKind.PredefinedType))
                {
                    return false;
                }

                if (!((PredefinedTypeSyntax)returnType).Keyword.IsKind(SyntaxKind.VoidKeyword))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsStatic => node.Modifiers.Any(SyntaxKind.StaticKeyword);
    }
}
