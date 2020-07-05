using System;

namespace Unispect
{
    public static class Log
    {
        public static string LogText { get; private set; }

        public enum MessageType
        {
            None,
            Information,
            Warning,
            Error,
            Exception
        }

        public class MessageAddedEventArgs : EventArgs
        {
            public string Message { get; }
            public MessageType Type { get; }

            public MessageAddedEventArgs(string text, MessageType messageType)
            {
                Message = text;
                Type = messageType;
            }
        }

        public static event EventHandler<MessageAddedEventArgs> LogMessageAdded;

        public static void Add(string text)
        {
            AppendLine(text);
        }

        public static void Info(string text)
        {
            AppendLine(text, MessageType.Information);
        }

        public static void Warn(string text)
        {
            AppendLine(text, MessageType.Warning);
        }

        public static void Error(string text)
        {
            AppendLine(text, MessageType.Error);
        }

        public static void Exception(string text, Exception ex)
        {
            var msg = !string.IsNullOrEmpty(text)
                ? $"{text}{Environment.NewLine}{ex.Message}"
                : ex.Message;

            AppendLine(msg, MessageType.Exception);
        }

        public static void AppendLine(string text, MessageType type = MessageType.None)
        {
            LogText +=
                $"[{DateTime.Now:HH:mm:ss.ff}] " +
                $"{(type == MessageType.None ? "" : $"[{Enum.GetName(typeof(MessageType), type)}]")} " +
                $"{text}" + Environment.NewLine;

            LogMessageAdded?.Invoke(null, new MessageAddedEventArgs(text, type));
        }

    }
}