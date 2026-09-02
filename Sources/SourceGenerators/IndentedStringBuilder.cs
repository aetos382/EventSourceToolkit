using System;
using System.Collections.Generic;
using System.Text;

namespace Aetos.EventSourceToolkit.SourceGenerators;

internal sealed class IndentedStringBuilder
{
    private const string IndentScopeDescription = "(indent)";

    private readonly string _indent;
    private readonly string _newLine;
    private readonly StringBuilder _stringBuilder;
    private readonly Stack<ScopeInfo> _openScopes;

    private bool _atLineStart = true;
    private int _indentationLevel;

    public IndentedStringBuilder(
        string indent = "    ",
        string newLine = "\n")
    {
        this._indent = indent;
        this._newLine = newLine;

        this._stringBuilder = new StringBuilder();
        this._openScopes = new Stack<ScopeInfo>();
    }

    public BlockScope Block(
        string? header = null,
        string? close = "}")
    {
        if (!string.IsNullOrEmpty(header))
        {
            this.AppendLine(header);
        }

        this.AppendLine("{");

        return this.PushScope(close, header ?? "{");
    }

    /// <summary>
    /// <paramref name="condition" /> が <see langword="true" /> の場合のみブロックを開く。
    /// <see langword="false" /> の場合は何も出力せず、インデントも変えない。
    /// </summary>
    public BlockScope BlockIf(
        bool condition,
        string? header = null,
        string? close = "}")
    {
        return condition ? this.Block(header, close) : default;
    }

    public BlockScope Indent()
    {
        return this.PushScope(null, IndentScopeDescription);
    }

    public IndentedStringBuilder AppendLine()
    {
        this.AppendNewLine();
        return this;
    }

    public IndentedStringBuilder AppendLine(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this.AppendNewLine();
            return this;
        }

        var lines = value!.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            this.AppendCore(line);
            this.AppendNewLine();
        }

        return this;
    }

    public IndentedStringBuilder Append(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            this.AppendCore(value!);
        }

        return this;
    }

    public string Build()
    {
        if (this._openScopes.Count != 0)
        {
            throw new InvalidOperationException(
                $"The scope '{this._openScopes.Peek().Description}' has not been closed.");
        }

        return this._stringBuilder.ToString();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this._stringBuilder.ToString();
    }

    private BlockScope PushScope(
        string? close,
        string description)
    {
        this._openScopes.Push(new ScopeInfo(close, description));
        ++this._indentationLevel;

        return new BlockScope(this, this._openScopes.Count);
    }

    private void CloseScope(
        int depth)
    {
        if (this._openScopes.Count != depth)
        {
            throw new InvalidOperationException(
                $"The scope at depth {depth} is being closed while the current depth is {this._openScopes.Count}.");
        }

        var scope = this._openScopes.Pop();
        --this._indentationLevel;

        if (scope.Close is not null)
        {
            this.AppendLine(scope.Close);
        }
    }

    private void AppendCore(string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        if (this._atLineStart)
        {
            this.AppendIndent();
        }

        this._stringBuilder.Append(value);

        this._atLineStart = value[value.Length - 1] is '\n' or '\r';
    }

    private void AppendNewLine()
    {
        this._stringBuilder.Append(this._newLine);
        this._atLineStart = true;
    }

    private void AppendIndent()
    {
        for (var i = 0; i < this._indentationLevel; ++i)
        {
            this._stringBuilder.Append(this._indent);
        }
    }

    private readonly struct ScopeInfo
    {
        public ScopeInfo(
            string? close,
            string description)
        {
            this.Close = close;
            this.Description = description;
        }

        /// <summary>
        /// スコープを閉じるときに出力する文字列。<see langword="null" /> の場合は何も出力しない。
        /// </summary>
        public string? Close { get; }

        public string Description { get; }
    }

    internal readonly ref struct BlockScope
    {
        private readonly IndentedStringBuilder _builder;
        private readonly int _depth;

        internal BlockScope(
            IndentedStringBuilder builder,
            int depth)
        {
            this._builder = builder;
            this._depth = depth;
        }

        public void Dispose()
        {
            // default(BlockScope) は何もしないスコープ。BlockIf が条件不成立の場合に返す
            this._builder?.CloseScope(this._depth);
        }
    }
}
