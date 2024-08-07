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
		[DebuggerDisplay("{Value}")]
		[global::System.CodeDom.Compiler.GeneratedCode("Microsoft.Windows.CsWin32", "0.3.106+a37a0b4b70")]
		internal unsafe readonly partial struct PWSTR
			: IEquatable<PWSTR>
		{
			internal readonly char* Value;

			internal PWSTR(char* value) => this.Value = value;

			public static implicit operator char*(PWSTR value) => value.Value;

			public static implicit operator PWSTR(char* value) => new PWSTR(value);

			public static bool operator ==(PWSTR left, PWSTR right) => left.Value == right.Value;

			public static bool operator !=(PWSTR left, PWSTR right) => !(left == right);

			public bool Equals(PWSTR other) => this.Value == other.Value;

			public override bool Equals(object obj) => obj is PWSTR other && this.Equals(other);

			public override int GetHashCode() => unchecked((int)this.Value);


			/// <inheritdoc cref="PCWSTR.ToString()"/>
			public override string ToString() => new PCWSTR(this.Value).ToString();

			public static implicit operator PCWSTR(PWSTR value) => new PCWSTR(value.Value);


			/// <inheritdoc cref="PCWSTR.Length"/>
			internal int Length => new PCWSTR(this.Value).Length;


			/// <summary>
			/// Returns a span of the characters in this string, up to the first null character (exclusive).
			/// </summary>
			internal Span<char> AsSpan() => this.Value is null ? default(Span<char>) : new Span<char>(this.Value, this.Length);


			private string DebuggerDisplay => this.ToString();
		}
	}
}
