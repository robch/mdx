# MDX - AI-Powered Markdown Generator CLI

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
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

Welcome to MDX, the AI-Powered Markdown Generator!

Using MDX, you can:

  - Convert files to markdown
  - Search the web and convert search results to markdown
  - Get web pages and convert them to markdown

  AND ... You can apply AI processing to the output!

USAGE: mdx FILE1 [FILE2 [...]] [...]
   OR: mdx PATTERN1 [PATTERN2 [...]] [...]
   OR: mdx web search "TERMS" ["TERMS2" [...]] [...]
   OR: mdx web get "URL" ["URL2" [...]] [...]

EXAMPLES

  EXAMPLE 1: Create markdown for one or more files

    mdx BackgroundInfo.docx
    mdx Presentation2.pptx
    mdx *.pdf

  EXAMPLE 2: Find files recursively and create markdown

    mdx **/*.cs

  EXAMPLE 3: Create markdown from a web search

    mdx web search "yaml site:learnxinyminutes.com" --max 1 --get --strip

SEE ALSO

  mdx help examples
  mdx help options
```

`mdx help examples`

```plaintext
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdx FILE1 [FILE2 [...]] [...]
   OR: mdx PATTERN1 [PATTERN2 [...]] [...]
   OR: mdx web search "TERMS" ["TERMS2" [...]] [...]
   OR: mdx web get "URL" ["URL2" [...]] [...]

EXAMPLES

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

  EXAMPLE 7: Save each file output to a specified template file

    mdx "**/*.cs" --save-file-output "outputs/{fileBase}.md"

SEE ALSO

  mdx help options

  mdx help web search
  mdx help web search examples
  mdx help web search options

  mdx help web get
  mdx help web get examples
  mdx help web get options
```

`mdx help options`

```plaintext
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

USAGE: mdx FILE1 [FILE2 [...]] [...]
   OR: mdx PATTERN1 [PATTERN2 [...]] [...]
   OR: mdx web search "TERMS" ["TERMS2" [...]] [...]
   OR: mdx web get "URL" ["URL2" [...]] [...]

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
  mdx help web search options

  mdx help web get
  mdx help web get examples
  mdx help web get options
```

`mdx help web search`

```plaintext
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

MDX WEB SEARCH

  Use the 'mdx web search' command to search the web and create markdown from the results.

USAGE: mdx web search "TERMS" ["TERMS2" [...]] [...]

EXAMPLES

  EXAMPLE 1: Create markdown for web search URL results

    mdx web search "Azure AI" --google
    mdx web search "Azure AI" --bing

  EXAMPLE 2: Create markdown for web search result content

    mdx web search "yaml site:learnxinyminutes.com" --max 1 --get --strip

SEE ALSO

  mdx help web search examples
  mdx help web search options

  mdx help web get
  mdx help web get examples
  mdx help web get options
```

`mdx help web search examples`

```plaintext
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

MDX WEB SEARCH

  Use the 'mdx web search' command to search the web and create markdown from the results.

USAGE: mdx web search "TERMS" ["TERMS2" [...]] [...]

EXAMPLES

  EXAMPLE 1: Create markdown for web search URL results

    mdx web search "Azure AI"
    mdx web search "Azure AI" --bing
    mdx web search "Azure AI" --exclude youtube.com reddit.com

  EXAMPLE 2: Create markdown for web search result content

    mdx web search "Azure AI" --max 5 --get --strip
    mdx web search "yaml site:learnxinyminutes.com" --max 1 --get --strip

  EXAMPLE 3: Apply AI processing on each web page

    mdx web search "web components" --get --strip --page-instructions "reformat markdown"

  EXAMPLE 4: Apply AI multi-step instructions

    mdx web search "how to fly kite" --get --strip --page-instructions @step1-instructions.txt @step2-instructions.txt

  EXAMPLE 5: Apply AI to the final output

    mdx web search "how to fly kite" --max 2 --get --strip --instructions "Create a markdown summary from all pages"

SEE ALSO

  mdx help web search
  mdx help web search options

  mdx help web get
  mdx help web get examples
  mdx help web get options
```

`mdx help web search options`

```plaintext
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

MDX WEB SEARCH

  Use the 'mdx web search' command to search the web and create markdown from the results.

USAGE: mdx web search "TERMS" ["TERMS2" [...]] [...]

OPTIONS

  BROWSER/HTML

    --headless                         Run in headless mode (default: false)
    --strip                            Strip HTML tags from downloaded content (default: false)

  SEARCH ENGINE

    --bing                             Use Bing search engine
    --google                           Use Google search engine (default)
    --get                              Download content from search results (default: false)

    --exclude REGEX                    Exclude URLs that match the specified regular expression
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

  mdx help web search
  mdx help web search examples

  mdx help web get
  mdx help web get examples
  mdx help web get options
```

`mdx help web get`

```plaintext
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

MDX WEB GET

  Use the 'mdx web get' command to create markdown from one or more web pages.

USAGE: mdx web get "URL" ["URL2" [...]] [...]

EXAMPLES

  EXAMPLE 1: Create markdown from a web page, keeping HTML tags

    mdx web get "https://learnxinyminutes.com/docs/yaml/"

  EXAMPLE 2: Create markdown from a web page, stripping HTML tags

    mdx web get "https://learnxinyminutes.com/docs/yaml/" --strip

SEE ALSO

  mdx help web get options
  mdx help web get examples

  mdx help web search
  mdx help web search examples
  mdx help web search options
```

`mdx help web get examples`

```plaintext
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

MDX WEB GET

  Use the 'mdx web get' command to create markdown from one or more web pages.

USAGE: mdx web get "URL" ["URL2" [...]] [...]

EXAMPLES

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
  mdx help web get options

  mdx help web search
  mdx help web search examples
  mdx help web search options
```

`mdx help web get options`

```plaintext
MDX - The AI-Powered Markdown Generator CLI, Version 1.0.0
Copyright(c) 2024, Rob Chambers. All rights reserved.

MDX WEB GET

  Use the 'mdx web get' command to create markdown from one or more web pages.

USAGE: mdx web get "URL" ["URL2" [...]] [...]

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

  mdx help web get
  mdx help web get examples

  mdx help web search
  mdx help web search examples
  mdx help web search options
```
