# CodeHeroes.CodeAnalysis.Style

A collection of Roslyn code analyzers (and fix providers) aimed at dealing with mundane styling concerns. The goal is to automate as much as possible so that code reviews can be more focused and contained.

The analyzers so far are:

* Trailing whitespace analyzer: flags any superfluous trailing whitespace in code, single-line comments, or multi-line comments. Does not yet support documentation comments.