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

project.json
```json
﻿{
    "version": "1.0.0-*",
    "compilationOptions": {
        "emitEntryPoint": true
    },

    "dependencies": {

    },

    "frameworks": {
      "net46": {
        "dependencies": {
          "ClassLibrary1": {
            "target": "msbuildProject"
          },
          "ClassLibrary2": {
            "target": "msbuildProject"
          },
          "ClassLibrary3": {
            "target": "msbuildProject"
          }
        }
      }
    }
}

```

Fragment file:
```json
{
	"version": 2,
	"exports": {
		"ClassLibrary1/1.0.0": {
			"type": "msbuildProject",
			"framework": ".NETFramework,Version=v4.5.2",
			"compile": {
				"bin/Debug/ClassLibrary1.dll": {}
			},
			"runtime": {
				"bin/Debug/ClassLibrary1.dll": {}
			}
		},
		"ClassLibrary2/1.0.0": {
			"type": "msbuildProject",
			"framework": ".NETFramework,Version=v4.6",
			"compile": {
				"../../bin/Debug/ClassLibrary2.dll": {}
			},
			"runtime": {
				"bin/Debug/ClassLibrary2.dll": {}
			}
		},
		"ClassLibrary3/1.0.0": {
			"type": "msbuildProject",
			"framework": ".NETFramework,Version=v4.6",
			"compile": {
				"c:/bin/Debug/ClassLibrary3.dll": {}
			},
			"runtime": {
				"bin/Debug/ClassLibrary3.dll": {}
			}
		}
	}
}
```

Main lock file:

```json
{
	"locked": false,
	"version": 2,
	"targets": {
		".NETFramework,Version=v4.6": {
			"ClassLibrary1/1.0.0": {
				"type": "msbuildProject"
			},
			"ClassLibrary2/1.0.0": {
				"type": "msbuildProject"
			},
			"ClassLibrary3/1.0.0": {
				"type": "msbuildProject"
			}
		},
		".NETFramework,Version=v4.6/win7-x64": {
			"ClassLibrary1/1.0.0": {
				"type": "msbuildProject"
			},
			"ClassLibrary2/1.0.0": {
				"type": "msbuildProject"
			},
			"ClassLibrary3/1.0.0": {
				"type": "msbuildProject"
			}
		},
		".NETFramework,Version=v4.6/win7-x86": {
			"ClassLibrary1/1.0.0": {
				"type": "msbuildProject"
			},
			"ClassLibrary2/1.0.0": {
				"type": "msbuildProject"
			},
			"ClassLibrary3/1.0.0": {
				"type": "msbuildProject"
			}
		}
	},
	"libraries": {
		"ClassLibrary1/1.0.0": {
			"type": "msbuildProject",
			"msbuildProject": "../../ClassLibrary1/ClassLibrary1.csproj"
		},
		"ClassLibrary2/1.0.0": {
			"type": "msbuildProject",
			"msbuildProject": "../../ClassLibrary2/ClassLibrary2.csproj"
		},
		"ClassLibrary3/1.0.0": {
			"type": "msbuildProject",
			"msbuildProject": "../../ClassLibrary3/ClassLibrary3.csproj"
		}
	},
	"projectFileDependencyGroups": {
		"": [],
		".NETFramework,Version=v4.6": [
			"ClassLibrary1",
			"ClassLibrary2",
			"ClassLibrary3"
		],
		".NETStandardApp,Version=v1.5": [
			"NETStandard.Library >= 1.0.0-rc2-23826"
		]
	}
}
```

## Merging logic

- option A: fragment lock file has the same format as the lock file and merging is structural (fragment/tfm1/rid1/ProjectFoo overwrites main/tfm1/rid1/ProjectFoo)
    - PROs:
        - transparent for all clients that consume the lock file
        - encapsulates dependency graph resolution in Nuget.
        - reduces many "across tool-chain" bugs
    - CONs
        - Nuget and Nuget MSBuild tasks have to be smarter about legacy projects and how to resolve their dependency cones (what is compatible with what, when should restore exit with error, etc)
- option B: fragment lock file is structurally and semantically different feom the lock file, and clients need to finish dependency resolution on their own
    - PROs:
        - 
    - CONs
        - force each client to re-implement part of nuget's dependency resolution logic. It means that whenever nuget changes, clients break and misbehave.

## CLI project model changes
The msbuild projects cannot be represeted as PackageDescription CLI objects. PackageDescription leaks a Project object which wraps over project.json. The whole CLI codebase took a dependency on the assumption that PackageDescription.Package is project.json.

Ideally a new library type has to be introduced ("legacyProject"). This describes projects that provide their assets up front, just like packages. The only difference from nuget packages is that legacy projects resolve their asset path from the project root, not from the nuget package cache.

## Unresolved issues
- framework / runtime compatibility issues: 
    - what happens if main lock file shows csproj ProjectA as targeting framework Foo, but the fragment shows ProjectA as targetting framework Bar
    - same as above but for runtimes
    - Does the csproj have explicit dependencies? Then how is the csproj dependency cone resolved against the main lock file dependency cone? (A_xproj -> B_csproj -> C_xproj). How is C's cone of locally resolved dependencies merged with A's cone of locally resolved dependencies? 
- fragment format / CLI design: what library type should csproj library dependencies appear as?
    - what are the valid elements for a csproj dependency in project.json, and lock file? 

## Discussion topics:

```
David's view:
- The fragment file is never a true lock file, it has no targets.
- The lock file has holes that need to be filed.
- Compatibility needs worked out by nuget not by the merging
- These types of projects need to be identified separately, "legacyProject" is a bad name. "msbuildProject" is better.

```
