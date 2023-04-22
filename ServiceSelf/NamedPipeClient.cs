using Google.Protobuf;
using System;
using System.Buffers;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace ServiceSelf
{
    /// <summary>
    /// 命名管道客户端
    /// </summary>
    sealed class NamedPipeClient
    {
        /// <summary>
        /// 当前实例
        /// </summary>
        private Instance instance;

        /// <summary>
        /// 当前是否支持写入
        /// </summary>
        public bool CanWrite => this.instance.CanWrite;

        /// <summary>
        /// 获取当前进程对应的实例
        /// </summary>
        public static NamedPipeClient Current { get; }

        /// <summary>
        /// 静态构造器
        /// </summary>
        static NamedPipeClient()
        {
#if NET6_0_OR_GREATER
            var processId = Environment.ProcessId;
#else
            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
#endif         
            Current = new NamedPipeClient($"{nameof(ServiceSelf)}_{processId}");
        }

        /// <summary>
        /// 命名管道客户端
        /// </summary>
        /// <param name="pipeName"></param>
        public NamedPipeClient(string pipeName)
        {
            this.instance = new Instance(pipeName);
            this.RunFlushInstance(pipeName);
        }


        /// <summary>
        /// 循环刷新实例
        /// </summary>
        /// <param name="pipeName"></param>
        private async void RunFlushInstance(string pipeName)
        {
            while (true)
            {
                using (this.instance)
                {
                    await this.instance.ConnectAsync();
                    await this.instance.ExceptionTask;
                }
                this.instance = new Instance(pipeName);
            }
        }

        /// <summary>
        /// 带边界写入消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Write(IMessage message)
        {
            using var serializer = new Serializer(message);
            var data = serializer.Serialize();
            return this.instance.Write(data);
        } 

        /// <summary>
        /// NamedPipeClient实例
        /// </summary>
        private class Instance : IDisposable
        {
            private readonly NamedPipeClientStream clientStream;
            private readonly TaskCompletionSource<object?> ExceptionTaskSource = new();

            /// <summary>
            /// 当前是否能写入
            /// </summary>
            public bool CanWrite { get; private set; }

            /// <summary>
            /// 异常等待的任务
            /// </summary>
            public Task ExceptionTask => this.ExceptionTaskSource.Task;


            public Instance(string pipeName)
            {
                this.clientStream = new(pipeName);
            }

            /// <summary>
            /// 连接管道
            /// </summary>
            /// <returns></returns>
            public async Task ConnectAsync()
            {
                await this.clientStream.ConnectAsync();
                this.CanWrite = true;
            }

            /// <summary>
            /// 写入数据
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
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
                    this.ExceptionTaskSource.TrySetResult(null);
                    return false;
                }
            }

            public void Dispose()
            {
                this.clientStream.Dispose();
            }
        }

        /// <summary>
        /// 包含消息长度的序列化
        /// </summary>
        private readonly struct Serializer : IDisposable
        {
            private readonly IMessage message;
            private readonly byte[] buffer;

            public Serializer(IMessage message)
            {
                this.message = message;
                this.buffer = ArrayPool<byte>.Shared.Rent(sizeof(int) + message.CalculateSize());
            }

            public ReadOnlySpan<byte> Serialize()
            {
                var outputStream = new CodedOutputStream(this.buffer);
                outputStream.WriteMessage(this.message);
                return this.buffer.AsSpan(0, (int)outputStream.Position);
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(this.buffer);
            }
        }
    }
}
