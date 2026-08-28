using System;
using System.Text;

namespace Aetos.Tracing;

internal sealed class IndentedStringBuilder
{
    private readonly string _indent;
    private readonly string _newLine;
    private readonly StringBuilder _stringBuilder;

    public IndentedStringBuilder(
        string indent = "    ",
        string newLine = "\n")
    {
        this._indent = indent;
        this._newLine = newLine;

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
        if (this.IndentationLevel == 0)
        {
            throw new InvalidOperationException();
        }

        --this.IndentationLevel;
        return this;
    }

    public IndentedStringBuilder AppendLineWithIndent()
    {
        this.AppendCore("", true, true);
        return this;
    }

    public IndentedStringBuilder AppendLineWithoutIndent()
    {
        this.AppendCore("", false, true);
        return this;
    }

    public IndentedStringBuilder AppendLineWithIndent(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var lines = value!.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            foreach (var line in lines)
            {
                this.AppendCore(line, true, true);
            }
        }

        return this;
    }

    public IndentedStringBuilder AppendWithIndent(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            this.AppendCore(value, true, false);
        }

        return this;
    }

    public IndentedStringBuilder AppendWithoutIndent(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            this.AppendCore(value, false, false);
        }

        return this;
    }

    private void AppendCore(string? value, bool indent, bool newLine)
    {
        if (indent)
        {
            this.AddIndent();
        }

        this._stringBuilder.Append(value);

        if (newLine)
        {
            this._stringBuilder.Append(this._newLine);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this._stringBuilder.ToString();
    }

    private void AddIndent()
    {
        for (var i = 0; i < this.IndentationLevel; ++i)
        {
            this._stringBuilder.Append(this._indent);
        }
    }
}
