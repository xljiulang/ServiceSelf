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
	namespace System.Services
	{
		[DebuggerDisplay("{Value}")]
		[global::System.CodeDom.Compiler.GeneratedCode("Microsoft.Windows.CsWin32", "0.3.106+a37a0b4b70")]
		internal readonly partial struct SC_HANDLE
			: IEquatable<SC_HANDLE>
		{
			internal readonly IntPtr Value;

			internal SC_HANDLE(IntPtr value) => this.Value = value;

			internal static SC_HANDLE Null => default;

			internal bool IsNull => Value == default;

			public static implicit operator IntPtr(SC_HANDLE value) => value.Value;

			public static explicit operator SC_HANDLE(IntPtr value) => new SC_HANDLE(value);

			public static bool operator ==(SC_HANDLE left, SC_HANDLE right) => left.Value == right.Value;

			public static bool operator !=(SC_HANDLE left, SC_HANDLE right) => !(left == right);

			public bool Equals(SC_HANDLE other) => this.Value == other.Value;

			public override bool Equals(object obj) => obj is SC_HANDLE other && this.Equals(other);

			public override int GetHashCode() => this.Value.GetHashCode();

			public override string ToString() => $"0x{this.Value:x}";
		}
	}
}
