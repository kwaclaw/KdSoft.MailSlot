using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.MailSlot {
    /// <summary>
    /// Mail slot listener for callback processing of messages.
    /// </summary>
    public class MailSlotListener: MailSlotListenerBase {
        /// <summary>
        /// Message processing callback delegate type.
        /// </summary>
        /// <param name="msgBytes">Byte sequence to process.</param>
        public delegate void ProcessMessage(ref ReadOnlySequence<byte> msgBytes);

        readonly ProcessMessage _processMessage;
        readonly CancellationToken _cancelToken;
        readonly Task _readTask;

        /// <summary>
        /// Constructor for callback processing of mailslot messages.
        /// </summary>
        /// <param name="mailslotName">Name of mail slot.</param>
        /// <param name="processMessage">Callback method called for each message received.
        ///     Should not throw an exception, otherwise processing will terminate.</param>
        /// <param name="messageSeparator">Byte value that separates messages.</param>
        /// <param name="cancelToken">CancellationToken that cancels both, message processing and listening.</param>
        public MailSlotListener(string mailslotName, ProcessMessage processMessage, byte messageSeparator = 0, CancellationToken cancelToken = default)
            : base(mailslotName, messageSeparator)
        {
            if (processMessage == null)
                throw new ArgumentNullException(nameof(processMessage));
            this._processMessage = processMessage;
            this._cancelToken = cancelToken;
            _readTask = ReadMail(_pipe.Reader);
        }

        async Task ReadMail(PipeReader reader) {
            while (!_cancelToken.IsCancellationRequested) {
                ReadOnlySequence<byte> buffer;
                ReadResult readResult;
                try {
                    readResult = await reader.ReadAsync(_cancelToken);
                    buffer = readResult.Buffer;
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (Exception ex) {
                    reader.Complete(ex);
                    return;
                }

                while (TryReadMessage(ref buffer, out var msgBytes)) {
                    _processMessage(ref msgBytes);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (readResult.IsCompleted || readResult.IsCanceled)
                    return;
            }

            reader.Complete();
        }

        /// <inheritdoc/>
        protected override async Task Listen(string mailslotName, PipeWriter writer) {
            var buffer = new byte[4096];
            using (var server = MailSlot.CreateServer(mailslotName)) {
                while (!_cancelToken.IsCancellationRequested) {
                    try {
                        var memory = writer.GetMemory(5);
                        var count = await server.ReadAsync(buffer, 0, buffer.Length, _cancelToken);
                        if (count == 0)
                            break;

                        var bufferMemory = new Memory<byte>(buffer, 0, count);
                        bufferMemory.CopyTo(memory);
                        writer.Advance(count);

                        var writeResult = await writer.FlushAsync(_cancelToken);
                        if (writeResult.IsCompleted)
                            break;
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
    }
}
