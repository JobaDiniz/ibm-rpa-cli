# RPA CLI command reference
The IBM® RPA Command Line Interface (RPA CLI) is an *unofficial* tool that provides an interface to manage and deploy IBM® RPA projects.

## `rpa project`
*in progress...*

## `rpa env`
*in progress...*

## `rpa bot`
*in progress...*

## `rpa package`
*in progress...*

## `rpa build`
*in progress...*

## `rpa deploy`
Deploys the project to the specified environment.

### Usage
`rpa deploy <target> [options]`

### Arguments
- `target`: The target environment name (alias) to deploy the project.

### Options
- `-v, --verbosity <Detailed|Diagnostic|Minimal|Normal|Quiet>`: Specifies how much output is sent to the console. *Default* is *Normal*.

## `rpa pull`
 Pulls parameters and WAL scripts from the specified environment.

### Usage
`rpa pull <name> [options]`

### Arguments
- `name`: The asset name. To pull several at once, use '*' at the end, e.g 'MyParam*'.

### Options
- `--env`: The alias of the environment to pull data from.
- `--type <parameter|wal>`: The type of the asset to pull, either *parameter* or *wal*. If not provided, assets from all types will be pulled.
- `-v, --verbosity <Detailed|Diagnostic|Minimal|Normal|Quiet>`: Specifies how much output is sent to the console. *Default* is *Normal*.

## `rpa push`
Publishes WAL files as scripts without modification to the specified environment, that is, without bundling dependencies. This is different from [deployment](guide/deploy.md). You can use this command to *migrate* (different than *deploy*) scripts between environments.

### Usage
`rpa push <searchPattern> [options]`

### Arguments
- `searchPattern`: The search string to match against the names of WAL files in the working directory. For all WAL files use `*.wal`.

### Options
- `--env`: The alias of the environment to push WAL files to.
- `-p, --property`: A key-value pair property to specify scripts parameters. Supported: `timeout=[hh:mm:ss]` and `prod=[true|false]`.
- `-v, --verbosity <Detailed|Diagnostic|Minimal|Normal|Quiet>`: Specifies how much output is sent to the console. *Default* is *Normal*.

The following command specifies *10 minutes* of `timeout` and *true* for `set as production (prod)` for *all* the scripts in the working directory: `rpa push *.wal -p:timeout=00:10:00 -p:prod=true`.

## `rpa git`
This command is *hidden* and used internally by RPA CLI to integrate with local git.