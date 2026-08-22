using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.Tracing;

internal static class SyntaxExtensions
{
    extension(MemberDeclarationSyntax node)
    {
        public bool HasPartialModifier => node.Modifiers.Any(SyntaxKind.PartialKeyword);

        public bool HasFileModifier => node.Modifiers.Any(SyntaxKind.FileKeyword);

        public string? AccessibilityKeyword
        {
            get
            {
                var modifiers = node.Modifiers;

                if (modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    return "public";
                }

                var hasPrivate = modifiers.Any(SyntaxKind.PrivateKeyword);
                var hasProtected = modifiers.Any(SyntaxKind.ProtectedKeyword);
                var hasInternal = modifiers.Any(SyntaxKind.InternalKeyword);

                if (hasPrivate)
                {
                    return hasProtected ? "private protected" : "private";
                }

                if (hasProtected)
                {
                    return hasInternal ? "protected internal" : "protected";
                }

                if (hasInternal)
                {
                    return "internal";
                }

                return null;
            }
        }
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
