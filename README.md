# 🪄 dotnet2md

![Build Status](https://github.com/isadorasophia/dotnet2md/actions/workflows/ci.yaml/badge.svg)
[![LICENSE](https://img.shields.io/github/license/isadorasophia/dotnet2md.svg)](LICENSE)

dotnet2md is a tool that converts .NET metadata and XML documentation to Markdown files.

## Using
The parser itself is an executable which accepts the following arguments:

```shell
$ Parser.exe <xml_path> <out_path> <targets>
```

- `<xml_path>`
  - Source directory, full or relative to the script. This is the path to the target .xml and all assemblies which will be scanned for metadata information. If your project depends on multiple assemblies, all of them should be reachable within this path (typically, you can use this as your publishing path).
- `<out_path>` 
  - Output path, full or relative to the script. It will create a markdown file for each type and directories according to their namespace hierarchy.
- `<targets>` 
  - Target assembly names. This accepts a list of different assemblies, all separated by spaces. These are the assemblies which the tool will inspect and translate all the public types and members into markdown.
  
#### Example
```shell
$ Parser.exe ../Release/net7.0/publish ../docs assembly1 assembly2
```

## Installing
### Building from source
_From terminal_
1. Open a terminal in the root directory
2. `dotnet restore`
3. `dotnet build`

_From Visual Studio_
1. Open `dotnet2md.sln` with Visual Studio 2022 17.4 or higher version (required for .NET 7)
2. Build!

### Pre-compiled binaries
You can download the binaries at our [releases](https://github.com/isadorasophia/dotnet2md/releases/) page. Or with the command line:

**ps1**
```shell
mkdir bin
Invoke-WebRequest https://github.com/isadorasophia/dotnet2md/releases/download/v0.1/dotnet2md-v0.1-win-x64.zip -OutFile bin/parser.zip
Expand-Archive bin/parser.zip -DestinationPath bin
Remove-Item bin/parser.zip
```

**sh**
```bash
mkdir bin
curl -sSL https://github.com/isadorasophia/dotnet2md/releases/download/v0.1/dotnet2md-v0.1-linux-x64.tar.gz | tar -xz --directory=bin
bin/mdbook build
```

## 📖 mdBook Integration
Did we mention there is mdBook integration? Because we do! 

Suppose your project name is `Assembly1`, you can create a `pre_SUMMARY.md` file at the root of the output directory with the following format:

```markdown
# Summary
- [Hello](hello.md)

# Assembly1
<Assembly1-Content>
```

The tool will replace the contents of `<Assembly1-Content>` with the hierarchy of the generated markdown files.
