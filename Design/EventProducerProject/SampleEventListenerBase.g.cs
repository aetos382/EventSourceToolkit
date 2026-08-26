using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using GeneratedCode;

namespace EventProducerProject;

// SampleEventSource から 1:1 で生成する。
// 複数のソースをサポートすると、シグネチャの衝突に備えてマングリングが必要になる。
// どうしてもリスナーを束ねたければ、別途 aggregated listener を手書きして DI すればいい。
[GeneratedEventListenerMarker]
public abstract class SampleEventListenerBase :
    EventListener
{
    // コンシューマー側の Source Generator で使うために SampleEventSource から属性を転記する
    [GeneratedEvent(1, EventLevel.Informational, Keywords.Request)]
    protected virtual void Foo(int i)
    {
    }

    [GeneratedEvent(2, EventLevel.Error)]
    protected virtual void Bar(string message)
    {
    }
}
