# xproj -> csproj

## Scenario: Add csproj refernece to xproj through UI

1. There are __xproj__ project __A__, and __csproj__ project __B__. The dependency relationship is not established now.
2. User add project __B__ as a reference to __A__ through Visual Studio UI.
3. __WTE__ add project B reference to project A's __xproj file__.
4. __WTE__ walk through the csproj graph and build a __dg__ file.
5. __dotnet restore__ restores projects. Relies on the __dg__ file generated in previous steps it manages to walk through all csproj files. It generated __project.lock.json__ for project A. The lock file has project references to project __B__ as well as all csproj it references. The lock file only provide the path to the csproj files.
6. __WTE__ walk down the entire msbuild project graph. For each csproj file it reachs it generates a __fragment lock file__.
7. After all the __fragment lock files__ are generated, these files are aggregrated into one __project.fragments.lock.json__ file by __WTE__. It placed at the same place as project A's __project.json__.

## Scenario: Build

1. __dotnet build__ looks at the lock file and realizes that it has csproj dependencies.
2. __dotnet build__ looks for the __project.fragment.lock.json__ at the same folder as __project.lock.json__
3. __dotnet build__ builds.
 
## Proposed data formats
Fragment file:
```json
{
  "version" : 2,
  "targets" : {
    "tfm":{
      "ClassLibrary1/1.0.0": {
        "type": "project",
        "framework": "tfm",
        "compile": {
          "bin/{config}/ClassLibrary1.dll": {}
        },
        "runtime": {
          "c:/...bin/Debug/ClassLibrary1.dll": {},
          "c:/../../packages/PackageName1/lib/net451/PackageConfigAssembly1.dll": {},
          "c:/../../packages/PackageName2/lib/451/PackageConfigAssembly1.dll": {},
          "c:/../../../somepath/LooseAssemblyReference1.dll" : {}
        }
      }
    }
  },
  "libraries" : {
    "ClassLibrary1/1.0.0": {
      "type": "project",
      "msbuildProject" : "C:/.../path/to/.csproj"
    }
  }
}
```

Main lock file:

```json
{
  "version" : 2,
  "targets" : {
    "tfm":{
      "..." : "...",
      "ClassLibrary1/1.0.0": {
        "type": "project",
        "dependencies": "// if there's a project.json in the csproj project; or from packages.config"
      },
      "..." : "..."
    }
  },
  "libraries" : {
    "..." : "...",
    "ClassLibrary1/1.0.0": {
      "type" : "project",
      "msbuildProject" : "C:/.../path/to/.csproj"
    },
    "..." : "..."
  }
}
```
