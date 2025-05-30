# AI Coding Guidelines for This Repository

This project allows AI tools (Copilot, Claude, etc.) to assist with coding.
All AI-generated contributions must follow these rules for consistency, clarity, and maintainability.

---

## 🧠 Core Principles

- **Write testable code** — always think in terms of clean input/output.
- **Start with interfaces** — define contracts first, logic later.
- **Keep logic modular** — use small, composable methods.
- **Avoid hardcoded or monolithic logic** — aim for flexibility and reuse.
- **Favor immutability and stateless design**.

---

## ✍️ C# Coding 

### Insert a line break after any closing brace

Be sure to insert a line break after any closing brace `}` that precedes non-brace code.
This is currently a typical issue with AI-generated code.  So be extra careful to follow this rule.

Example:
```csharp
public class MyClass
{
    public void MyMethod()
    {
        // Some logic here
    } // <- Insert an extra line break here

    public void AnotherMethod()
    {
        // More logic here
    }
}
```
### Prefer operators elegantly placed at the beginning of the line

Example:
```csharp
return condition
    && First()
    || Second();
````

### Other Suggestions

- Use `readonly record struct` for small immutable data.
- Prefer `ReadOnlySpan<T>`, `Memory<T>`, `StringSegment` to reduce allocations.
- Use `field`, collection expressions, and other modern C# features when appropriate.

---

## 📐 Partial Class Strategy (High Priority)

Partial classes are critical for AI productivity and regeneration safety.

* Use `partial` classes for any type exceeding \~250 lines or with multiple concerns.
* Split files by function (e.g., `MyType._.cs`, `MyType.Parser.cs`, `MyType.Renderer.cs`).
* The main file (`_.cs`) should include constructor, disposal, and XML docs.

### Why Partial Classes?

When a class or static class becomes large or contains distinct conceptual regions,
prefer splitting it into **partial classes across multiple files**.

When in doubt, it's better to have a class with a single method per file as it's easier to regenerate
than a large monolithic class that is easy to cause corruption in.

With tests, it's strongly recommended to use folders and partial classes to keep things organized and easy to iterate.

---

## ✅ Development Flow

1. **Define the interface**

   * Describe what, not how.
   * Ensure it can be mocked and tested in isolation.

2. **Write the test**

   * Use realistic inputs and edge cases.
   * Use `NSubstitute` (not Moq) for mocking.
   * Use `Verify` for snapshot tests (e.g., SVG, JSON).

3. **Write the logic**

   * Keep it simple and direct.
   * Avoid cleverness unless necessary.

---

## 🔍 AI Coding Rules

* Reuse existing types and interfaces.
* Avoid boilerplate, scaffolding, and speculative refactors.
* Never edit `.editorconfig`, `.github/`, or directive files without instruction.
* Add `TODO:` comments for stubbed logic or placeholders.
* If unsure, generate a draft and ask before committing.

---

This document is a living guide. Treat it as the AI’s coding compass — for quality, consistency, and safe collaboration.
