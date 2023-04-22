using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace ServiceSelf
{
    sealed class NamedPipeClient
    {
        private readonly string pipeName;
        private NamedPipeClientItem clientItem;

        public bool CanWrite => this.clientItem.CanWrite;

        public NamedPipeClient(string pipeName)
        {
            this.pipeName = pipeName;
            this.clientItem = new NamedPipeClientItem(pipeName);
            this.KeepAliveAsync();
        }

        private async void KeepAliveAsync()
        {
            while (true)
            {
                using (this.clientItem)
                {
                    await this.clientItem.ConnectAsync();
                    await this.clientItem.ErrorTask;
                }
                this.clientItem = new NamedPipeClientItem(pipeName);
            }
        }

        public bool Write(ReadOnlySpan<byte> data)
        {
            return this.clientItem.Write(data);
        }


        public static NamedPipeClient Current { get; }

        static NamedPipeClient()
        {
#if NET6_0_OR_GREATER
            var processId = Environment.ProcessId;
#else
            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
            var name = $"{nameof(ServiceSelf)}_{processId}";
            Current = new NamedPipeClient(name);
        }


        private class NamedPipeClientItem : IDisposable
        {
            private readonly NamedPipeClientStream clientStream;
            private readonly TaskCompletionSource<object?> errorTaskSource = new();

            public bool CanWrite { get; private set; }
            public Task ErrorTask => this.errorTaskSource.Task;


            public NamedPipeClientItem(string pipeName)
            {
                this.clientStream = new(pipeName);
            }

            public async Task ConnectAsync()
            {
                await this.clientStream.ConnectAsync();
                this.CanWrite = true;
            }

            public bool Write(ReadOnlySpan<byte> data)
            {
                if (this.CanWrite == false)
                {
                    return false;
                }

                try
                {
                    this.clientStream.Write(data);
                    return true;
                }
                catch (Exception)
                {
                    this.errorTaskSource.TrySetResult(null);
                    return false;
                }
            }

            public void Dispose()
            {
                this.clientStream.Dispose();
            }
        }
    }
}
