# Coding Standards

## Naming

Use PascalCase for classes, methods, properties, enums, and public API members. Use camelCase for private fields, parameters, and local variables.

Serialized fields should normally remain private:

    [SerializeField] private float movementSpeed;

Avoid public fields unless a field intentionally represents a public API. Expose state through focused properties, methods, interfaces, or events.

## Dependencies

- Prefer explicit dependencies over runtime discovery.
- Avoid FindObjectOfType-style dependency discovery when possible.
- Avoid GameObject.Find.
- Keep portal integrations behind ProjectGatefront.Platform interfaces.
- Keep editor-only code in editor-only assemblies.

## Design

- Avoid magic numbers; use named constants or configuration assets.
- Prefer ScriptableObjects for tunable balance data.
- Use object pooling for recurring gameplay entities.
- Keep MonoBehaviours focused on Unity lifecycle and scene integration.
- Prefer composition over giant manager classes.
- Do not turn any class into a generic dumping ground.
- Add abstractions only where they create a real boundary or test seam.

## Events and access

Use private, protected, interfaces, and events according to ownership. Event publishers own their events; subscribers must unsubscribe when their lifetime requires it. Do not expose mutable collections as public state.

## Documentation

Document non-obvious intent, constraints, and tradeoffs. Do not restate obvious code with meaningless comments. Public APIs that are easy to misuse should explain expectations and failure behavior.

## Performance

Avoid avoidable allocations in per-frame or high-frequency paths. Prefer centralized or event-driven updates where practical, but do not build premature frameworks. Profile before making complex optimizations.
