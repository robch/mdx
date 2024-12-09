## README

# MDCC - Markdown Context Creator CLI

MDCC is a command-line tool that takes file pattern inputs and outputs the contents of those files in a markdown-friendly format.

## Features
- Supports glob patterns for specifying multiple files.
- Outputs filenames as markdown headers followed by content in code blocks.
- Handles relative and absolute file paths efficiently.
- Provides usage information when no arguments are specified.

## Installation
To build the project, ensure you have .NET SDK 8.0 installed. Then, navigate to the project directory and run:

```bash
 dotnet build
```

## Usage
```plaintext
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]]

OPTIONS:

  --contains REGEX             Match only files and lines that contain the specified regex pattern

  --file-contains REGEX        Match only files that contain the specified regex pattern
  --file-not-contains REGEX    Exclude files that contain the specified regex pattern

  --line-contains REGEX        Match only lines that contain the specified regex pattern
  --lines-before N             Include N lines before matching lines (default 0)
  --lines-after N              Include N lines after matching lines (default 0)
  --lines N                    Include N lines both before and after matching lines

  --line-numbers               Include line numbers in the output
  --remove-all-lines REGEX     Remove lines that contain the specified regex pattern

EXAMPLES:

  mdcc file1.cs
  mdcc file1.md file2.md

  mdcc "**/*.cs" "*.md" --line-numbers

  mdcc "**" --contains "(?i)LLM" --lines 2
```