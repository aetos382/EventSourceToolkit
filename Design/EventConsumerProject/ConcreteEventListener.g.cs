#nullable enable

using System;
using System.Diagnostics.Tracing;

namespace EventConsumerProject;

// GeneratedEventListenerAttribute がついているクラスから派生しているクラスが対象
partial class ConcreteEventListener
{
    private bool _started;

    /// <inheritdoc />
    protected override void OnEventSourceCreated(
        EventSource eventSource)
    {
        if (!this._started)
        {
            return;
        }

        // 基底クラスの GeneratedEventListenerAttribute から得る
        if (eventSource.Name == "SampleEvents")
        {
            this.EnableEvents(eventSource);
        }
    }

    // OnEventSourceCreated は基底クラスのコンストラクタから呼ばれるので、ConcreteListener の初期化が終わっていないうちに実行されうる。
    // そこでフィールドに触るとぬるぽで死ぬので、明示的に Start させる。
    // MetricListener も同じ方式。
    public void Start(bool throwIfAlreadyStarted = true)
    {
        if (Interlocked.Exchange(ref this._started, true) && throwIfAlreadyStarted)
        {
            throw new InvalidOperationException("already started.");
        }

        foreach (var eventSource in EventSource.GetSources())
        {
            // 基底クラスの GeneratedEventListenerAttribute から得る
            if (eventSource.Name == "SampleEvents")
            {
                this.EnableEvents(eventSource);
                break;
            }
        }
    }

    private void EnableEvents(EventSource eventSource)
    {
        // 手書きコードでオーバーライドされているメソッドから、対応する EventAttribute を収集し、その和を取って有効化する
        this.EnableEvents(eventSource, EventLevel.Informational);
    }

    /// <inheritdoc />
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // 基底クラスの GeneratedEventListenerAttribute から得る
        if (eventData.EventSource.Name == "SampleEvents")
        {
            // 手書きコードでオーバーライドされているメソッドについてのみ分岐を生成する
            if (eventData.EventId == 1)
            {
                this.Foo((int)eventData.Payload[0]);
                return;
            }
        }
    }
}
