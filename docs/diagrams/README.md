# Architecture diagrams

Standalone diagram sources, kept outside `ARCHITECTURE.md` so they are easier to
read, zoom, and edit. The same diagrams remain inline in
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) for readers browsing the docs on GitHub.

| File | Diagram |
| --- | --- |
| [`01-layers.mmd`](01-layers.mmd) | Clean Architecture layers and request flow |
| [`02-request-sequence.mmd`](02-request-sequence.mmd) | `GET /api/v1/breweries` request sequence |
| [`03-security-pipeline.mmd`](03-security-pipeline.mmd) | Middleware pipeline and security controls |

## How to view

**Option 1 - open `index.html` (easiest):**

Double-click [`index.html`](index.html), or right-click it in VS Code and choose
*Reveal in File Explorer* then open it. All three diagrams render with zoom
controls. No server is needed.

The diagram definitions are embedded in `index.html`, so it works from a
`file://` path. It does need an internet connection the first time, because
Mermaid is loaded from a CDN.

**Option 2 - paste into the online editor:**

Copy the contents of any `.mmd` file into <https://mermaid.live>. Useful for
editing, since it re-renders as you type.

**Option 3 - VS Code:**

Install the *Markdown Preview Mermaid Support* extension, or any Mermaid
preview extension that handles `.mmd` files.

## Editing

The `.mmd` files are plain text Mermaid. Two things to watch for:

- Use `<br/>` for line breaks inside node labels, not `\n`.
- In the `.mmd` files, do **not** HTML-escape characters such as `<` and `>` —
  they are parsed as raw Mermaid. Write `IReadOnlyList of Brewery` rather than
  `IReadOnlyList&lt;Brewery&gt;`. Inside `index.html` the same text *is* escaped,
  because there it lives in an HTML document.

If you change a diagram, update all three places so they stay in sync: the
`.mmd` file, the embedded copy in `index.html`, and the matching ```mermaid
block in [`ARCHITECTURE.md`](../../ARCHITECTURE.md).
