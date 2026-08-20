using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Text;
using GeneratedCode;

namespace EventProducerProject;

partial class SampleEventSource
{
    /// <inheritdoc />
    public partial void Foo(int i)
    {
        if (!this.IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            return;
        }

        this.WriteEvent(1, i);
    }

    /// <inheritdoc />
    public partial void Bar(string message)
    {
        if (!this.IsEnabled(EventLevel.Error, EventKeywords.None))
        {
            return;
        }

        this.WriteEvent(2, message);
    }
}
