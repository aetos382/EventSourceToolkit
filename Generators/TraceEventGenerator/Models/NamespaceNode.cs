using System;
using System.Collections.Generic;
using System.Text;

namespace Aetos.Tracing.Models;

internal sealed class NamespaceNode :
    IOutputNode,
    IEquatable<NamespaceNode>
{
    private readonly string _namespaceName;
    private readonly IndentedStringBuilder _builder;

    public NamespaceNode(
        string namespaceName,
        IndentedStringBuilder builder)
    {
        this._namespaceName = namespaceName;
        this._builder = builder;
    }

    public void Write()
    {
    }

    public bool Equals(NamespaceNode other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this._namespaceName == other._namespaceName;
    }
}
