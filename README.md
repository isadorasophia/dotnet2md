# 🪄 dotnet2md

[![LICENSE](https://img.shields.io/github/license/isadorasophia/dotnet2md.svg)](LICENSE)

dotnet2md is a tool that converts .NET metadata and XML documentation to Markdown files.

## Using
The parser itself is an executable which accepts the following arguments:

```shell
$ Parser.exe <xml_path> <out_path> <targets>
```

- `<xml_path>` Source directory. This is the path with the target .xml and all assemblies which will be scanned for metadata information. Full or relative to the executable path.
- `<out_path>` Output path. Full or relative to the executable path.
- `<targets>` Target assembly names. This accepts a list of different targets.

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
You can download the binaries at our [releases](https://github.com/isadorasophia/dotnet2md/release) page. Or through the command line:

**ps1**
```shell
mkdir bin
Invoke-WebRequest release-link-windows -OutFile bin/parser.zip
Expand-Archive bin/parser.zip -DestinationPath bin
Remove-Item bin/parser.zip
```

**sh**
```bash
mkdir bin
curl -sSL release-link-linux | tar -xz --directory=bin
bin/mdbook build
```
