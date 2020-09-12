using System;
using System.Buffers;
using System.Text;

namespace KdSoft.MailSlot.TestServer
{
    public static class Extensions
    {
        public static string GetString(this Encoding encoding, ref ReadOnlySequence<byte> bytes) {
            var decoder = encoding.GetDecoder();
            var preProcessedBytes = 0;
            var processedCharacters = 0;
            var totalLength = bytes.Length;  // max possible character count
            Span<char> characterSpan = totalLength > 1024 ? new char[totalLength] : stackalloc char[(int)totalLength];

            foreach (var segment in bytes) {
                preProcessedBytes += segment.Length;
                var isLast = (preProcessedBytes == totalLength);
                var emptyCharSlice = characterSpan.Slice(processedCharacters, characterSpan.Length - processedCharacters);
                var charCount = decoder.GetChars(segment.Span, emptyCharSlice, isLast);
                processedCharacters += charCount;
            }

            var finalCharacters = characterSpan.Slice(0, processedCharacters);
            return new string(finalCharacters);
        }
    }
}
