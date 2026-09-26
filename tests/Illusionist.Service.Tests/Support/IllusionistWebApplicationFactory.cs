using Microsoft.AspNetCore.Mvc.Testing;

namespace Illusionist.Service.Tests.Support;

/// <summary>
/// Hosts the real <see cref="Program"/> in-process (an ASP.NET Core <c>TestServer</c>, not
/// Kestrel) for every test that needs to exercise MCP or REST end to end. The self-check runs for
/// real at startup, exactly as in production.
/// </summary>
public sealed class IllusionistWebApplicationFactory : WebApplicationFactory<Program>;
