# CodeHeroes.CodeAnalysis.Style

A collection of Roslyn code analyzers (and fix providers) aimed at dealing with mundane styling concerns. The goal is to automate as much as possible so that code reviews can be more focused and contained.

You can install via NuGet:
```
> Install-Package CodeHeroes.CodeAnalysis.Style
```

# Analyzers

## Trailing Whitespace (`CH0001`)

Flags any superfluous trailing whitespace in code, single-line comments, or multi-line comments. Does not yet support documentation comments. Includes an automated fix to remove detected whitespace.

## Using Directives within Namespace (`CH0002`)

Flags any using directives that are outside a namespace declaration. Includes an automated fix to move the using directive inside the topmost namespace declaration.

## Using Directives Sort Order (`CH0003`)

Flags any using directives that are incorrectly sorted. All using directives are expected to be in alphabetical order, but with `System` namespaces first. Includes an automated fix to rearrange using directives to adhere to the required sort order.

## Single Namespace per File (`CH0004`)

Flags any files that include more than one namespace within.

## Single Top-level Type per File (`CH0005`)

Flags any files that include more than one top-level type. Delegates are excepted.

## Reserve Underscore Identifiers for Discard Semantics (`CH0006`)

Flags any used identifiers whose names consist solely of underscore characters. Note that unused method and lambda arguments can be named solely with underscores.