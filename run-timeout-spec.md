### `mdx run` `--timeout` spec

**Objective**: 
We aim to add a `--timeout MILLISECONDS` option to the `mdx run` command. This option will ensure that the 'run' operation does not exceed the specified duration. If the operation exceeds the given time limit, it will attempt to terminate the process gracefully, and if that fails, it will send a ctrl-c signal and attempt to kill the process again.

**Purpose**:
Users need the ability to limit the runtime of commands executed via the `mdx run` command. This can be useful for preventing long-running or potentially hanging processes from consuming resources indefinitely.

**Developer Notes**:
- Implement the `--timeout MILLISECONDS` option in the `mdx run` command.
- Ensure the command tries to terminate the process gracefully first.
- If initial termination fails, send a ctrl-c signal.
- If the process is still running after the ctrl-c signal, force kill the process.
- Add logic to handle edge cases, such as invalid timeout values or processes that cannot be terminated.

### Help Updates

#### `mdx help run options`

```
MDX RUN

  Use the 'mdx run' command to execute scripts or commands and create markdown from the output.

USAGE: mdx run [COMMAND1 [COMMAND2 [...]]] [...]

OPTIONS

  SCRIPT

    --script [COMMAND]            Specify the script or command to run
                                  (On Windows, the default is cmd. On Linux/Mac, the default is bash)

    --cmd [COMMAND]               Specify the script or command to run
    --bash [COMMAND]              Specify the script or command to run
    --powershell [COMMAND]        Specify the script or command to run

  AI PROCESSING

    --instructions "..."          Apply the specified instructions to command output (uses AI CLI).
    --built-in-functions          Enable built-in functions (AI CLI can use file system).

  TIMEOUT

    --timeout MILLISECONDS        Set a maximum runtime for the command. If the command runs longer than the specified duration,
                                  it will attempt to terminate the process. If that fails, it will send a ctrl-c signal and try again.

  OUTPUT

    --save-output [FILE]          Save command output to the specified template file.
    --save-alias ALIAS            Save current options as an alias (usable via --{ALIAS}).

SEE ALSO

  mdx help run
  mdx help run examples

```

#### `mdx help run examples`

```
MDX RUN

  Use the 'mdx run' command to execute scripts or commands and create markdown from the output.

USAGE: mdx run [COMMAND1 [COMMAND2 [...]]] [...]

EXAMPLES

  EXAMPLE 1: Run a simple command and process the output

    mdx run "echo Hello, World!" --instructions "translate strings to german"

  EXAMPLE 2: Run a script using PowerShell and process the output

    mdx run --powershell "Get-Process" --instructions "list running processes"

  EXAMPLE 3: Run a bash script and apply multi-step AI instructions

    mdx run --bash "ls -la" --instructions @step1-instructions.txt @step2-instructions.txt

  EXAMPLE 4: Run multiple commands

    mdx run "echo Hello, World!" "echo Goodbye, World!"

  EXAMPLE 5: Run a command with a timeout

    mdx run "long-running-command" --timeout 5000 --instructions "summarize the output"
    
SEE ALSO

  mdx help run
  mdx help options

```

