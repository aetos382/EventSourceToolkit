using System;
using System.Diagnostics.Tracing;

namespace StandardEventSourceTest;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        using var source = new TestEventSource();
        using var listener = new TestEventListener();

        listener.EnableEvents(source, EventLevel.LogAlways);

        source.Test(2, true, "hello", TestEnum8.A, TestEnum32.B, TestEnum64.C);
    }

    private sealed class TestEventSource : EventSource
    {
        public void Test(
            int a, bool b, string c, TestEnum8 d, TestEnum32 e, TestEnum64 f)
        {
            if (!this.IsEnabled())
            {
                return;
            }

            unsafe
            {
                c ??= "";

                fixed (char* pc = c)
                {
                    EventData* data = stackalloc EventData[6];

                    data[0].DataPointer = (IntPtr)(&a);
                    data[0].Size = 4;

                    int bi = b ? 1 : 0;
                    data[1].DataPointer = (IntPtr)(&bi);
                    data[1].Size = 4;

                    data[2].DataPointer = (IntPtr)pc;
                    data[2].Size = (c.Length + 1) * 2;

                    int di = (int)d;
                    data[3].DataPointer = (IntPtr)(&di);
                    data[3].Size = 4;

                    data[4].DataPointer = (IntPtr)(&e);
                    data[4].Size = 4;

                    data[5].DataPointer = (IntPtr)(&f);
                    data[5].Size = 8;

                    this.WriteEventWithRelatedActivityIdCore(1, null, 6, data);
                }
            }
        }
    }

    private sealed class TestEventListener : EventListener
    {
        /// <inheritdoc />
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventSource is not TestEventSource)
            {
                return;
            }

            Console.WriteLine();
        }
    }

    private enum TestEnum8 : byte { A, B, C }
    private enum TestEnum32 : int { A, B, C }
    private enum TestEnum64 : long { A, B, C }
}
