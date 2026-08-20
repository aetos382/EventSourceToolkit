using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using GeneratedCode;
using static EventProducerProject.SampleEventSource;

namespace EventProducerProject;

// SampleEventSource から 1:1 で生成する。
// 複数のソースをサポートすると、シグネチャの衝突に備えてマングリングが必要になる。
// どうしてもリスナーを束ねたければ、別途 aggregated listener を手書きして DI すればいい。
[GeneratedEventListener("SampleEvents")]
public abstract class SampleEventListenerBase :
    EventListener
{
    // コンシューマー側の Source Generator で使うために SampleEventSource から属性を転記する
    [Event(1, Level = EventLevel.Informational, Keywords = Keywords.Request)]
    protected virtual void Foo(int i)
    {
    }

    [Event(2, Level = EventLevel.Error)]
    protected virtual void Bar(string message)
    {
    }
}
