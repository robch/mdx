# MDX - Markdown Context Creator CLI

MDX is a command-line tool that helps build markdown files from various sources. It can process files, search the web, and apply AI processing to create markdown content. The tool supports file and line filtering, line formatting, AI processing, and output options. It can be used to create markdown files for documentation, research, and other purposes.

## Features
- Integrates AI processing for applying instructions to files, pages, or command outputs.
- Supports glob patterns for specifying multiple files.
- Outputs filenames as markdown headers followed by content in code blocks.
- Handles relative and absolute file paths efficiently.
- Allows filtering of files and lines based on regular expressions.
- Provides options to include or exclude specific lines and add line numbers.
- Capable of handling multiple threads for file processing.
- Supports web search and retrieval with markdown formatting.
- Allows headless browsing and HTML stripping for web content.
- Enables saving output and configuration options to specified files.

## Installation
To build the project, ensure you have .NET SDK 8.0 installed. Then, navigate to the project directory and run:

```bash
dotnet build
```

## Usage

`mdx`

```plaintext
MDX - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdx [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]
   OR: mdx web search "TERMS" [...]
   OR: mdx web get "URL" [...]

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

  mdx help examples

  mdx help web search
  mdx help web search examples

  mdx help web get
  mdx help web get examples
```

`mdc web search`

```plaintext
MDX - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdx web search "TERMS" [...]

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

  mdx help web search examples

  mdx help web get
  mdx help web get examples
```

`mdc web get`

```plaintext
MDX - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdx web get "URL" [...]

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

  mdx help web get examples

  mdx help web search
  mdx help web search examples
```

`mdx help examples`

```plaintext
MDX - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

  EXAMPLE 1: Create markdown for one or more files

    mdx BackgroundInfo.docx
    mdx Presentation2.pptx
    mdx ResearchPaper.pdf
    mdx "../plans/*.md"

  EXAMPLE 2: Find files recursively, exclude certain files

    mdx "**/*.cs" "**/*.md"
    mdx "**/*.cs" --exclude "**/bin/" "**/obj/"

  EXAMPLE 3: Filter and format based on file or line content

    mdx "**/*.js" --file-contains "export"
    mdx "**/*.cs" --file-contains "public class"
    mdx "**/*.cs" --remove-all-lines "^\s//"

    mdx "**/*.md" --contains "TODO" --line-numbers
    mdx "**/*.md" --contains "(?i)LLM" --lines-after 10

  EXAMPLE 4: Apply AI processing on each found file

    mdx "**/*.json" --file-instructions "convert the JSON to YAML"
    mdx "**/*.json" --file-instructions @instructions.md --threads 5

  EXAMPLE 5: Apply AI to specific file types; multi-step instructions

    mdx --cs-file-instructions @cs-instructions.txt --md-file-instructions @md-instructions.txt
    mdx --file-instructions @step1-instructions.md @step2-instructions.md

  EXAMPLE 6: Apply AI to the final output

    mdx "**/*.md" --instructions "Create a markdown summary table for each file"
    mdx README.md "**/*.cs" --instructions "Output only an updated README.md"

SEE ALSO

  mdx help
  
  mdx help web search
  mdx help web search examples

  mdx help web get
  mdx help web get examples
```

`mdx help web search examples`

```plaintext
MDX - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

  EXAMPLE 1: Create markdown for web search URLs

    mdx web search "Rob Chambers Microsoft"
    mdx web search "Rob Chambers Microsoft" --bing

  EXAMPLE 2: Create markdown for web page content

    mdx web search "Rob Chambers Microsoft" --max 5 --get --strip
    mdx web search "yaml site:learnxinyminutes.com" --max 1 --get --strip

  EXAMPLE 3: Apply AI processing on each web page

    mdx web search "web components" --get --strip --page-instructions "reformat markdown"

  EXAMPLE 4: Apply AI multi-step instructions

    mdx web search "how to fly kite" --page-instructions @step1-instructions.txt @step2-instructions.txt

  EXAMPLE 5: Apply AI to the final output

    mdx web search "how to fly kite" --instructions "Create a markdown summary from all pages"

SEE ALSO

  mdx help web search

  mdx help web get
  mdx help web get examples
```

`mdx help web get examples`

```plaintext
MDX - Markdown Context Creator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

  EXAMPLE 1: Create markdown for web page content

    mdx web get https://example.com
    mdx web get https://mbers.us/bio --strip

  EXAMPLE 2: Apply AI processing on each web page

    mdx web get https://example.com https://mbers.us/bio --page-instructions "what's the title of this page?"

  EXAMPLE 3: Apply AI multi-step instructions

    mdx web get https://learnxinyminutes.com/yaml/ --page-instructions @step1-instructions.txt @step2-instructions.txt

  EXAMPLE 4: Apply AI to the final output

    mdx web get https://example.com https://mbers.us/bio --instructions "style example.com as the other site"    

SEE ALSO

  mdx help web get

  mdx help web search
  mdx help web search examples
```
