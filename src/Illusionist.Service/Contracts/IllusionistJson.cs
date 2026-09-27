using System.Text.Encodings.Web;
using System.Text.Json;

namespace Illusionist.Service.Contracts;

/// <summary>The one <see cref="Utf8JsonWriter"/> configuration every hand-written JSON body in this service uses.</summary>
internal static class IllusionistJson
{
	public static readonly JsonWriterOptions WriterOptions = new()
	{
		Indented = false,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};
}
