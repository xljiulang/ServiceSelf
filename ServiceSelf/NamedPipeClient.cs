using Google.Protobuf;
using System;
using System.Buffers;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceSelf
{
    /// <summary>
    /// 命名管道客户端
    /// </summary>
    sealed class NamedPipeClient : IDisposable
    {
        /// <summary>
        /// 当前实例
        /// </summary>
        private Instance instance;

        /// <summary>
        /// 关闭tokenSource
        /// </summary>
        private readonly CancellationTokenSource disposeTokenSource = new();

        /// <summary>
        /// 当前是否支持写入
        /// </summary>
        public bool CanWrite => this.instance.CanWrite;

        /// <summary>
        /// 命名管道客户端
        /// </summary>
        /// <param name="pipeName"></param>
        public NamedPipeClient(string pipeName)
        {
            this.instance = new Instance(pipeName);
            this.RunRefreshInstanceAsync(pipeName);
        }

        /// <summary>
        /// 自动刷新实例
        /// </summary>
        /// <param name="pipeName"></param>
        private async void RunRefreshInstanceAsync(string pipeName)
        {
            try
            {
                while (true)
                {
                    await this.RefreshInstanceAsync(pipeName);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 刷新实例
        /// </summary>
        /// <param name="pipeName"></param>
        /// <returns></returns>
        private async Task RefreshInstanceAsync(string pipeName)
        {
            using (this.instance)
            {
                var cancellationToken = this.disposeTokenSource.Token;
                await this.instance.ConnectAsync(cancellationToken);
                await this.instance.WaitForClosedAsync(cancellationToken);
            }
            this.instance = new Instance(pipeName);
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
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.disposeTokenSource.Cancel();
            this.disposeTokenSource.Dispose();
        }

        /// <summary>
        /// NamedPipeClient实例
        /// </summary>
        private class Instance : IDisposable
        {
            private readonly NamedPipeClientStream clientStream;
            private readonly TaskCompletionSource<object?> closeTaskSource = new();

            /// <summary>
            /// 当前是否能写入
            /// </summary>
            public bool CanWrite { get; private set; }


            public Instance(string pipeName)
            {
                this.clientStream = new(pipeName);
            }

            /// <summary>
            /// 连接管道
            /// </summary>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            public async Task ConnectAsync(CancellationToken cancellationToken = default)
            {
                await this.clientStream.ConnectAsync(cancellationToken);
                this.CanWrite = true;
            }

            /// <summary>
            /// 等待关闭
            /// </summary>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            public Task WaitForClosedAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.Register(OperationCanceled);
                return this.closeTaskSource.Task;

                void OperationCanceled()
                {
                    var exception = new OperationCanceledException(cancellationToken);
                    this.closeTaskSource.TrySetException(exception);
                }
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
                    this.CanWrite = false;
                    this.closeTaskSource.TrySetResult(null);
                    return false;
                }
            }

            public void Dispose()
            {
                this.CanWrite = false;
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
