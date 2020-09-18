#if NETSTANDARD2_1

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.MailSlot {
    /// <summary>
    /// Mail slot listener for asynchronous processing of messages.
    /// </summary>
    public class AsyncMailSlotListener: MailSlotListenerBase {
        readonly CancellationToken _listenCancelToken;

        /// <summary>
        /// Constructor for asynchnronous processing of mailslot messages.
        /// </summary>
        /// <param name="mailslotName">Name of mail slot.</param>
        /// <param name="messageSeparator">Byte value that separates messages.</param>
        /// <param name="listenCancelToken">CancellationToken that cancels listening.</param>
        public AsyncMailSlotListener(string mailslotName, byte messageSeparator = 0, CancellationToken listenCancelToken = default)
            : base(mailslotName, messageSeparator)
        {
            this._listenCancelToken = listenCancelToken;
        }

        /// <inheritdoc/>
        protected override async Task Listen(string mailslotName, PipeWriter writer) {
            using (var server = MailSlot.CreateServer(mailslotName)) {
                while (!_listenCancelToken.IsCancellationRequested) {
                    try {
                        var memory = writer.GetMemory(4096);
                        var count = await server.ReadAsync(memory, _listenCancelToken);
                        if (count == 0)
                            break;

                        writer.Advance(count);
                        var writeResult = await writer.FlushAsync(_listenCancelToken);
                        if (writeResult.IsCompleted)
                            return;
                    }
                    catch (OperationCanceledException) {
                        break;
                    }
                    catch (Exception ex) {
                        writer.Complete(ex);
                        return;
                    }
                }

                writer.Complete();
            }
        }

        /// <summary>
        /// Async enumerable returning messages received.
        /// </summary>
        /// <param name="readCancelToken">CancellationToken to cancel reading messages.</param>
        public async IAsyncEnumerable<ReadOnlySequence<byte>> GetNextMessage([EnumeratorCancellation]CancellationToken readCancelToken = default) {
            var reader = _pipe.Reader;
            while (!readCancelToken.IsCancellationRequested) {
                ReadOnlySequence<byte> buffer;
                ReadResult readResult;
                try {
                    readResult = await reader.ReadAsync(readCancelToken);
                    buffer = readResult.Buffer;
                }
                catch (OperationCanceledException) {
                    reader.Complete();
                    break;
                }
                catch (Exception ex) {
                    reader.Complete(ex);
                    break;
                }

                while (TryReadMessage(ref buffer, out var msgBytes)) {
                    yield return msgBytes;
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (readResult.IsCompleted)
                    break;
            }
        }
    }
}

#endif