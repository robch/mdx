## README

# MDCC - Markdown Context Creator CLI

MDCC is a command-line tool that takes file pattern inputs and outputs the contents of those files in a markdown-friendly format. It offers various options to include or exclude files and lines based on regular expressions, line numbers, and other criteria.

## Features
- Supports glob patterns for specifying multiple files.
- Outputs filenames as markdown headers followed by content in code blocks.
- Handles relative and absolute file paths efficiently.
- Allows filtering of files and lines based on regular expressions.
- Provides options to include or exclude specific lines and add line numbers.
- Capable of handling multiple threads for file processing.

## Installation
To build the project, ensure you have .NET SDK 8.0 installed. Then, navigate to the project directory and run:

```bash
dotnet build
```

## Usage
```plaintext
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]

OPTIONS

  --contains REGEX             Match only files and lines that contain the specified regex pattern

  --file-contains REGEX        Match only files that contain the specified regex pattern
  --file-not-contains REGEX    Exclude files that contain the specified regex pattern
  --exclude PATTERN            Exclude files that match the specified pattern

  --line-contains REGEX        Match only lines that contain the specified regex pattern
  --lines-before N             Include N lines before matching lines (default 0)
  --lines-after N              Include N lines after matching lines (default 0)
  --lines N                    Include N lines both before and after matching lines

  --line-numbers               Include line numbers in the output
  --remove-all-lines REGEX     Remove lines that contain the specified regex pattern

  --file-instructions "..."    Apply the specified instructions to each file using AI CLI (e.g., @file)
  --threads N                  Limit the number of concurrent file processing threads (default <number_of_processors>)

@ARGUMENTS

  Arguments starting with @ (e.g. @file) will use file content as argument.
  Arguments starting with @@ (e.g. @@file) will use file content as arguments line by line.

EXAMPLES

  mdcc file1.cs
  mdcc file1.md file2.md
  mdcc @@filelist.txt

  mdcc "src/**/*.cs" "*.md"
  mdcc "src/**/*.js" --contains "export"
  mdcc "src/**" --contains "(?i)LLM" --lines 2
  mdcc "src/**" --file-not-contains "TODO" --exclude "drafts/*"
  mdcc "*.cs" --remove-all-lines "^\s*//"

  mdcc "**/*.json" --file-instructions "convert the JSON to YAML"
  mdcc "**/*.json" --file-instructions @instructions.md --threads 5
  mdcc "**/*.cs" --file-instructions @step1-instructions.md @step2-instructions.md
```

## Example
Suppose you have the following files:

- `example1.cs`
- `example2.md`
- `filelist.txt` (contains a list of files)

You can run MDCC with different options to filter and format the content of these files as shown in the examples above.
