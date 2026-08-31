using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.EventSourceToolkit;

[Embedded]
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

                if (node.HasFileModifier)
                {
                    return "file";
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

        /// <summary>
        /// この型に対して、別のファイルから partial パートを追加できるかどうかを取得します。
        /// この型自身とそれを包含するすべての型が partial であり、かつ file ローカル型でない場合に <see langword="true" /> になります。
        /// </summary>
        public bool CanBeAugmented => node.FindAugmentationBlocker() is null;

        /// <summary>
        /// partial パートの追加を妨げている型を、この型自身から外側に向かって探します。
        /// </summary>
        /// <returns>妨げている型とその理由。妨げているものがない場合は <see langword="null" />。</returns>
        public AugmentationBlocker? FindAugmentationBlocker()
        {
            var currentNode = node;

            while (true)
            {
                if (!currentNode.HasPartialModifier)
                {
                    return new AugmentationBlocker(currentNode, AugmentationBlockerReason.NotPartial);
                }

                // file 修飾子を持つ型はファイルごとに別の型として扱われるため、他のファイルからパートを追加できない
                if (currentNode.HasFileModifier)
                {
                    return new AugmentationBlocker(currentNode, AugmentationBlockerReason.FileLocal);
                }

                if (currentNode.Parent is not TypeDeclarationSyntax enclosingTypeNode)
                {
                    return null;
                }

                currentNode = enclosingTypeNode;
            }
        }
    }

    extension(MethodDeclarationSyntax node)
    {
        public bool ReturnsVoid
        {
            get
            {
                // C# では void や System.Void に対して using で別名を付けられないので
                // symbol を見なくても syntax だけで判定ができる。
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
