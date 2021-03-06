Known issues & workarounds
==========================

## OpenSSL dependency on OS X
OS X "El Capitan" (10.11) comes with 0.9.8 version of OpenSSL. .NET Core depends on versions >= 1.0.1 of OpenSSL. You can update the version by using [Homebrew](https://brew.sh), [MacPorts](https://www.macports.org/) or manually. The important bit is that you need to have the required OpenSSL version on the path when you work with .NET Core. 

With Homebrew, you can run the following commands to get this done: 

```console
brew update
brew install openssl
brew link --force openssl
```

MacPorts doesn't have the concept of linking, so it is reccomended that you uninstall 0.9.8 version of OpenSSL using the following command:

```console
sudo port upgrade openssl
sudo port -f uninstall openssl @0.9.8
```

You can verify whether you have the right version using the  `openssl version` command from the Terminal. 

## Users of zsh (z shell) don't get `dotnet` on the path after install
There is a problem with the way `path_helper` interacts with `zsh` which makes `dotnet` not appear on the path even though 
the install places the file properly in the `/etc/paths.d/` directory. 

**Issues tracking this:**

* [#1567](https://github.com/dotnet/cli/issues/1567)

**Workaround:** symlink the `dotnet` binary in the installation directory to a place in the global path, e.g. `/usr/local/bin`. 
The command you can use is:

```console
ln -s /usr/local/share/dotnet/dotnet /usr/local/bin
```

## `dotnet restore` times out on Win7 x64
If you have Virtual Box and you try to use the CLI on a Win7 x64 machine, `dotnet restore` will be really slow and will eventually time out without doing much restoring. 

**Issues tracking this:** 

* [#1732](https://github.com/dotnet/cli/issues/1732)

**Affects:** `dotnet restore`

**Workaround:** disable the VirtualBox network interface and do the restore.   

## Resolving the Standard library packages
The StdLib package is on a MyGet feed. In order to restore it, a MyGet feed needs to be added 
to the NuGet feeds, either locally per application or in a central location. 

**Issues tracking this:** 

* [#535](https://github.com/dotnet/cli/issues/535)

**Affects:** `dotnet restore`

**Workaround:** update to the latest bits and run `dotnet new` in an empty directory. This will 
now drop a `nuget.config` file that you can use in other applications. 

If you cannot update, you can use the following `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!--To inherit the global NuGet package sources remove the <clear/> line below -->
    <clear />
    <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
    <add key="api.nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

## Uninstalling/reinstalling the PKG on OS X
OS X doesn't really have an uninstall capacity for PKGs like Windows has for 
MSIs. There is, however, a way to remove the bits as well as the "recipe" for 
dotnet. More information can be found on [this SuperUser question](http://superuser.com/questions/36567/how-do-i-uninstall-any-apple-pkg-package-file).

# What is this document about? 
This document outlines the known issues and workarounds for the current state of 
the CLI tools. Issues will also have a workaround and affects sections if necessary. You can use this page to 
get information and get unblocked.

# What is a "known issue"?
A "known issue" is a major issue that block users in doing their everyday tasks and that affect all or 
most of the commands in the CLI tools. If you want to report or see minor issues, you can use the [issues list](https://github.com/dotnet/cli/issues). 

