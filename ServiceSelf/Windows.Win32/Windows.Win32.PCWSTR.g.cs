// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

#pragma warning disable CS1591,CS1573,CS0465,CS0649,CS8019,CS1570,CS1584,CS1658,CS0436,CS8981
using global::System;
using global::System.Diagnostics;
using global::System.Diagnostics.CodeAnalysis;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;
using global::System.Runtime.Versioning;
using winmdroot = global::Windows.Win32;
namespace Windows.Win32
{
	namespace Foundation
	{
		/// <summary>
		/// A pointer to a null-terminated, constant character string.
		/// </summary>
		[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
		internal unsafe readonly partial struct PCWSTR
			: IEquatable<PCWSTR>
		{
			/// <summary>
			/// A pointer to the first character in the string. The content should be considered readonly, as it was typed as constant in the SDK.
			/// </summary>
			internal readonly char* Value;

			internal PCWSTR(char* value) => this.Value = value;

			public static explicit operator char*(PCWSTR value) => value.Value;

			public static implicit operator PCWSTR(char* value) => new PCWSTR(value);

			public bool Equals(PCWSTR other) => this.Value == other.Value;

			public override bool Equals(object obj) => obj is PCWSTR other && this.Equals(other);

			public override int GetHashCode() => unchecked((int)this.Value);


			/// <summary>
			/// Gets the number of characters up to the first null character (exclusive).
			/// </summary>
			internal int Length
			{
				get
				{
					char* p = this.Value;
					if (p is null)
						return 0;
					while (*p != '\0')
						p++;
					return checked((int)(p - this.Value));
				}
			}


			/// <summary>
			/// Returns a <see langword="string"/> with a copy of this character array, up to the first null character (exclusive).
			/// </summary>
			/// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
			public override string ToString() => this.Value is null ? null : new string(this.Value);


			/// <summary>
			/// Returns a span of the characters in this string, up to the first null character (exclusive).
			/// </summary>
			internal ReadOnlySpan<char> AsSpan() => this.Value is null ? default(ReadOnlySpan<char>) : new ReadOnlySpan<char>(this.Value, this.Length);


			private string DebuggerDisplay => this.ToString();
		}
	}
}
