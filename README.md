# CodeHeroes.CodeAnalysis.Style

A collection of Roslyn code analyzers (and fix providers) aimed at dealing with mundane styling concerns. The goal is to automate as much as possible so that code reviews can be more focused and contained.

You can install via NuGet:
```
> Install-Package CodeHeroes.CodeAnalysis.Style
```

# Analyzers

## Trailing whitespace (`CH0001`)

Flags any superfluous trailing whitespace in code, single-line comments, or multi-line comments. Does not yet support documentation comments. Includes an automated fix to remove detected whitespace.

## Using directives within namespace (`CH0002`)

Flags any using directives that are outside a namespace declaration, assuming a namespace declaration exists in that file. Includes an automated fix to move the using directive inside the topmost namespace declaration.

## Using directives sort order (`CH0003`)

Flags any using directives that are incorrectly sorted. All using directives are expected to be in alphabetical order, but with `System` namespaces first. Includes an automated fix to rearrange using directives to adhere to the required sort order.

## Single namespace per file (`CH0004`)

Flags any files that include more than one namespace within.

## Single top-level type per file (`CH0005`)

Flags any files that include more than one top-level type. Delegates are excepted.

## Used identifiers cannot have discard names (`CH0006`)

Flags any used identifiers whose names consist solely of underscore characters.

## Unused lambda parameters must have discard names (`CH0007`)

Flags any unused lambda parameters whose names do not consist solely of underscore characters.