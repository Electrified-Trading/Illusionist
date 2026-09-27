using System.Runtime.InteropServices;

namespace Illusionist.Service.SelfCheck;

/// <summary>
/// This process's own operating system and CPU architecture, formatted as
/// <c>{os}-{architecture}</c> (e.g. <c>windows-x64</c>, <c>linux-x64</c>, <c>linux-arm64</c>) -- the
/// same shape a generator version's own <see cref="Illusionist.Service.Generators.IGeneratorVersion.ReferencePlatform"/>
/// uses, so the two are directly comparable.
/// </summary>
public static class HostPlatform
{
	/// <summary>This process's own platform identifier, computed once.</summary>
	public static string Current { get; } = Describe();

	private static string Describe()
	{
		var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows"
			: RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux"
			: RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
			: "other";

		var architecture = RuntimeInformation.ProcessArchitecture switch
		{
			Architecture.X64 => "x64",
			Architecture.X86 => "x86",
			Architecture.Arm64 => "arm64",
			Architecture.Arm => "arm",
			var other => other.ToString().ToLowerInvariant(),
		};

		return $"{os}-{architecture}";
	}
}
