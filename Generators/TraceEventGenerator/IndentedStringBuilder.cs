using System;
using System.Text;

namespace Aetos.Tracing;

internal sealed class IndentedStringBuilder
{
    private readonly string _indent;
    private readonly StringBuilder _stringBuilder;

    public IndentedStringBuilder(string indent = "    ")
    {
        this._indent = indent;
        this._stringBuilder = new StringBuilder();
    }

    public int IndentationLevel { get; private set; }

    public IndentedStringBuilder Indent()
    {
        ++this.IndentationLevel;
        return this;
    }

    public IndentedStringBuilder Unindent()
    {
        --this.IndentationLevel;
        return this;
    }

    public IndentedStringBuilder AppendLine(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            this.AppendLineCore(value);
        }

        return this;
    }

    public IndentedStringBuilder AppendLine()
    {
        this.AppendLineCore(string.Empty);
        return this;
    }

    public IndentedStringBuilder Append(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            this._stringBuilder.Append(value);
        }

        return this;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this._stringBuilder.ToString();
    }

    private void AppendLineCore(string value)
    {
        for (var i = 0; i < this.IndentationLevel; ++i)
        {
            this._stringBuilder.Append(this._indent);
        }

        this._stringBuilder.AppendLine(value);
    }
}
