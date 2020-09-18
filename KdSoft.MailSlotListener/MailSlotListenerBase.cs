using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace KdSoft.MailSlot {
    /// <summary>
    /// Base class for mail slot listeners.
    /// </summary>
    public abstract class MailSlotListenerBase {
        /// <summary>
        /// Pipe instance used.
        /// </summary>
        [CLSCompliant(false)]
        protected readonly Pipe _pipe;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mailslotName">Name of mail slot.</param>
        /// <param name="messageSeparator">Byte value that separates messages.</param>
        public MailSlotListenerBase(string mailslotName, byte messageSeparator) {
            if (mailslotName == null)
                throw new ArgumentNullException(nameof(mailslotName));
            this.MessageSeparator = messageSeparator;
            _pipe = new Pipe();
            Task = Listen(mailslotName, _pipe.Writer);
        }

        /// <summary>
        /// Message separator. Delimits individual mailslot messages.
        /// Depends on agreement between sender and listener.
        /// </summary>
        public byte MessageSeparator { get; }

        /// <summary>
        /// Listener Task that can be awaited.
        /// </summary>
        public Task Task { get; }

        /// <summary>
        /// Abstract method that needs to be implemented.
        /// </summary>
        /// <param name="mailslotName">Name of mail slot.</param>
        /// <param name="writer">PipeWriter to use.</param>
        /// <returns>Listener task.</returns>
        protected abstract Task Listen(string mailslotName, PipeWriter writer);

        /// <summary>
        /// Helper method to parse individual messages out of the buffer.
        /// </summary>
        /// <param name="buffer">Buffer to parse, gets updated when a complete message is retrieved.</param>
        /// <param name="msgBytes">The message retrieved.</param>
        /// <returns><c>true</c> if a new complete message could be parsed, <c>false</c> otherwise.</returns>
        protected bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> msgBytes) {
            var pos = buffer.PositionOf(MessageSeparator);
            if (pos == null) {
                msgBytes = default;
                return false;
            }

            msgBytes = buffer.Slice(0, pos.Value);

            var nextStart = buffer.GetPosition(1, pos.Value);
            buffer = buffer.Slice(nextStart);
            return true;
        }
    }
}
