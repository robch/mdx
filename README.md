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
```
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]]
```

## Examples

```bash
mdcc file1.cs
mdcc file1.md file2.md
mdcc "**/*.cs" "*.md"
mdcc "src/**/*.py" "scripts/*.sh"
```

## Author
Rob Chambers

## License
All rights reserved, Copyright(c) 2024.