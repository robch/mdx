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

`mdcc`

```plaintext
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]
   OR: mdcc web search "TERMS" [...]
   OR: mdcc web get "URL" [...]

OPTIONS

  FILE/LINE FILTERING

    --exclude PATTERN              Exclude files that match the specified pattern

    --contains REGEX               Match only files and lines that contain the specified regex pattern
    --file-contains REGEX          Match only files that contain the specified regex pattern
    --file-not-contains REGEX      Exclude files that contain the specified regex pattern

    --line-contains REGEX          Match only lines that contain the specified regex pattern
    --remove-all-lines REGEX       Remove lines that contain the specified regex pattern

  LINE FORMATTING

    --lines N                      Include N lines both before and after matching lines
    --lines-after N                Include N lines after matching lines (default 0)
    --lines-before N               Include N lines before matching lines (default 0)

    --line-numbers                 Include line numbers in the output

  AI PROCESSING

    --file-instructions "..."      Apply the specified instructions to each file (uses AI CLI)
    --EXT-file-instructions "..."  Apply the specified instructions to each file with the specified extension

    --instructions "..."           Apply the specified instructions to command output (uses AI CLI)

    --built-in-functions           Enable built-in functions (AI CLI can use file system)
    --threads N                    Limit the number of concurrent file processing threads

  OUTPUT

    --save-page-output [FILE]      Save each file output to the specified template file
                                   (e.g. {filePath}/{fileBase}-output.md)

    --save-output [FILE]           Save command output to the specified template file
    --save-options [FILE]          Save current options to the specified file

SEE ALSO

  mdcc help examples

  mdcc help web search
  mdcc help web search examples

  mdcc help web get
  mdcc help web get examples
```

`mdc web search`

```plaintext
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdcc web search "TERMS" [...]

OPTIONS

  BROWSER/HTML

    --headless                         Run in headless mode (default: false)
    --strip                            Strip HTML tags from downloaded content (default: false)

  SEARCH ENGINE

    --bing                             Use Bing search engine
    --google                           Use Google search engine (default)
    --get                              Download content from search results (default: false)
    --max NUMBER                       Maximum number of search results (default: 10)

  AI PROCESSING

    --page-instructions "..."          Apply the specified instructions to each page (uses AI CLI)
    --SITE-page-instructions "..."     Apply the specified instructions to each page (for matching SITEs)

    --instructions "..."               Apply the specified instructions to command output (uses AI CLI)

    --built-in-functions               Enable built-in functions (AI CLI can use file system)
    --threads N                        Limit the number of concurrent file processing threads

  OUTPUT

    --save-page-output [FILE]          Save each web page output to the specified template file
                                       (e.g. {filePath}/{fileBase}-output.md)

    --save-output [FILE]               Save command output to the specified template file
    --save-options [FILE]              Save current options to the specified file

SEE ALSO

  mdcc help web search examples

  mdcc help web get
  mdcc help web get examples
```

`mdc web get`

```plaintext
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdcc web get "URL" [...]

OPTIONS

  BROWSER/HTML

    --headless                         Run in headless mode (default: false)
    --strip                            Strip HTML tags from downloaded content (default: false)

  AI PROCESSING

    --page-instructions "..."          Apply the specified instructions to each page (uses AI CLI)
    --SITE-page-instructions "..."     Apply the specified instructions to each page (for matching SITEs)

    --instructions "..."               Apply the specified instructions to command output (uses AI CLI)

    --built-in-functions               Enable built-in functions (AI CLI can use file system)
    --threads N                        Limit the number of concurrent file processing threads

  OUTPUT

    --save-page-output [FILE]          Save each web page output to the specified template file
                                       (e.g. {filePath}/{fileBase}-output.md)

    --save-output [FILE]               Save command output to the specified template file
    --save-options [FILE]              Save current options to the specified file

SEE ALSO

  mdcc help web get examples

  mdcc help web search
  mdcc help web search examples
```

`mdcc help examples`

```plaintext
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

  EXAMPLE 1: Create markdown for one or more files

    mdcc BackgroundInfo.docx
    mdcc Presentation2.pptx
    mdcc ResearchPaper.pdf
    mdcc "../plans/*.md"

  EXAMPLE 2: Find files recursively, exclude certain files

    mdcc "**/*.cs" "**/*.md"
    mdcc "**/*.cs" --exclude "**/bin/" "**/obj/"

  EXAMPLE 3: Filter and format based on file or line content

    mdcc "**/*.js" --file-contains "export"
    mdcc "**/*.cs" --file-contains "public class"
    mdcc "**/*.cs" --remove-all-lines "^\s//"

    mdcc "**/*.md" --contains "TODO" --line-numbers
    mdcc "**/*.md" --contains "(?i)LLM" --lines-after 10

  EXAMPLE 4: Apply AI processing on each found file

    mdcc "**/*.json" --file-instructions "convert the JSON to YAML"
    mdcc "**/*.json" --file-instructions @instructions.md --threads 5

  EXAMPLE 5: Apply AI to specific file types; multi-step instructions

    mdcc --cs-file-instructions @cs-instructions.txt --md-file-instructions @md-instructions.txt
    mdcc --file-instructions @step1-instructions.md @step2-instructions.md

  EXAMPLE 6: Apply AI to the final output

    mdcc "**/*.md" --instructions "Create a markdown summary table for each file"
    mdcc README.md "**/*.cs" --instructions "Output only an updated README.md"

SEE ALSO

  mdcc help
  
  mdcc help web search
  mdcc help web search examples

  mdcc help web get
  mdcc help web get examples
```

`mdcc help web search examples`

```plaintext
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

  EXAMPLE 1: Create markdown for web search URLs

    mdcc web search "Rob Chambers Microsoft"
    mdcc web search "Rob Chambers Microsoft" --bing

  EXAMPLE 2: Create markdown for web page content

    mdcc web search "Rob Chambers Microsoft" --max 5 --get --strip
    mdcc web search "yaml site:learnxinyminutes.com" --max 1 --get --strip

  EXAMPLE 3: Apply AI processing on each web page

    mdcc web search "web components" --get --strip --page-instructions "reformat markdown"

  EXAMPLE 4: Apply AI multi-step instructions

    mdcc web search "how to fly kite" --page-instructions @step1-instructions.txt @step2-instructions.txt

  EXAMPLE 5: Apply AI to the final output

    mdcc web search "how to fly kite" --instructions "Create a markdown summary from all pages"

SEE ALSO

  mdcc help web search

  mdcc help web get
  mdcc help web get examples
```

`mdcc help web get examples`

```plaintext
MDCC - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

  EXAMPLE 1: Create markdown for web page content

    mdcc web get https://example.com
    mdcc web get https://mbers.us/bio --strip

  EXAMPLE 2: Apply AI processing on each web page

    mdcc web get https://example.com https://mbers.us/bio --page-instructions "what's the title of this page?"

  EXAMPLE 3: Apply AI multi-step instructions

    mdcc web get https://learnxinyminutes.com/yaml/ --page-instructions @step1-instructions.txt @step2-instructions.txt

  EXAMPLE 4: Apply AI to the final output

    mdcc web get https://example.com https://mbers.us/bio --instructions "style example.com as the other site"    

SEE ALSO

  mdcc help web get

  mdcc help web search
  mdcc help web search examples
```
