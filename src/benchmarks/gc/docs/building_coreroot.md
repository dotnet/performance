# Building Core_Root from the Runtime Repo

First, you need a clone of https://github.com/dotnet/runtime.
Note that it may be on an arbitrary commit (including one you might be currently working on).

Once you are ready to build the `Core_Root`, follow these steps.

## Building with Default Settings

These steps will assume you want to build the `Core_Root` for the machine you are working on.
This means the targeted OS and Architecture will be deduced by the runtime from your machine's specs.

If you want to build for another OS/Architecture, use the `-os/--os` and `-arch/--arch` flags when calling the first two build scripts. For the third one (Core_Root), prepend your target architecture with a `-`(e.g. `-x64`) and the OS with the `-os`(e.g. `-os Linux`) flag on both, Windows and Linux.

### Build CoreCLR and Libraries

From the root of the `runtime` repo, issue the following command:

On Windows
```powershell
.\build.cmd -s clr+libs -lc Release -rc Release
```

On Linux
```sh
./build.sh -s clr+libs -lc Release -rc Release
```

Breaking down this command into parts to explain their meaning:

* **-s clr+libs**: This tells the script to build the CoreCLR and Libraries subsets.
* **-lc Release**: Build the libraries under Release configuration.
* **-rc Release**: Build the runtime under Release configuration.

If for some reason you can't build them together, already have one built, etc,
you can also ask the script to build them separately. For example, for just the runtime:

```powershell
.\build.cmd -s clr -c Release
```

### Build the Core_Root

At this point, the stage has been set to generate the `Core_Root`.
This time, move to the tests directory: `/runtime/src/tests/`. There, issue the following command:

On Windows
```powershell
.\build.cmd Release generatelayoutonly
```

On Linux
```sh
./build.sh -release -generatelayoutonly
```

Once that's finished, you can find your new _Core_Root_ within the artifacts path. For example:
`/runtime/artifacts/tests/coreclr/Linux.x64.Release/Tests/Core_Root`

Substitute `Linux` and `x64` with the OS and Architecture you are targeting.

## Building for ARM/ARM64

On Windows, this process is almost the same as for any other architecture. Specify it as explained in the above section.

On Linux, there are additional steps to take. You need to have a `ROOTFS_DIR` and specify the `--cross` (`-cross` for `build-test.sh`) flag when calling the build scripts.

Detailed instructions on how to generate the _ROOTFS_ on Linux and how to cross-build can be found [here](https://github.com/dotnet/runtime/blob/master/docs/workflow/building/coreclr/cross-building.md).

Another alternative is to use Docker containers. They allow for a more straightforward and less complicated setup and you can find the instructions to use them [here](https://github.com/dotnet/runtime/blob/master/docs/workflow/building/coreclr/linux-instructions.md). They allow for both, normal and cross building.
