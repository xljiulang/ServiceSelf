using System;
using System.Diagnostics.CodeAnalysis;

namespace ServiceSelf
{
    sealed class Command : IEquatable<Command>
    {
        private readonly string value;

        public static Command Stop { get; } = new("Stop");
        public static Command Start { get; } = new("Start");
        public static Command Logs { get; } = new("Logs");

        private Command(string value)
        {
            this.value = value;
        }

        public static bool TryParse(string? value, [MaybeNullWhen(false)] out Command command)
        {
            if (value == null)
            {
                command = null;
                return false;
            }

            command = new(value);
            return command.Equals(Stop) || command.Equals(Start) || command.Equals(Logs);
        }

        public override string ToString()
        {
            return this.value;
        }

        public bool Equals(Command? other)
        {
            return other != null && string.Equals(other.value, this.value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is Command cmd && this.Equals(cmd);
        }

        public override int GetHashCode()
        {
            return string.GetHashCode(this.value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
