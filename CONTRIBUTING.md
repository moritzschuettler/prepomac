# Contributing

Thanks for helping with PrePoMax for macOS. Bug reports, test results on other Macs and pull requests are all welcome.

## Reporting a bug

Open an [issue](https://github.com/moritzschuettler/prepomac/issues/new/choose) with the bug report form. The most
useful reports name:

- the steps that lead to the problem,
- a small model that shows it (.pmx, or the CAD file),
- the macOS version and the chip (Apple Silicon or Intel),
- what the output window or the terminal says (`PPM_TRACE=1 ./run.sh` writes the exceptions with their stack traces).

A problem that also happens in PrePoMax on Windows belongs to the [original project](https://gitlab.com/MatejB/PrePoMax).

## Building

Install the tools and build as the [README](README.md#install) describes, then:

```sh
cd macOS
./build.sh              # Debug build (./build.sh Release for Release)
./run.sh                # starts the Debug build
```

The first clone carries the whole history of PrePoMax (about 260 MB); `git clone --depth 1` is quicker if you do not
need it.

## Making a change

1. Fork the repository and create a branch for your change.
2. The shared PrePoMax sources are in the folders of the repository root; the projects in `macOS/` compile them and
   replace only what depends on Windows. **The macOS projects list the shared files one by one**, so a new source file
   in a shared folder must also be added to the `.csproj` of its folder in `macOS/`, or it is not compiled.
3. Keep the shared sources as close to the original as possible: a change that is not about macOS is easier to bring
   back to the original project.
4. User actions go through command classes (`PrePoMax/Commands`), so that a model can be rebuilt from its history;
   call the `...Command` methods of the controller rather than the actions themselves.
5. Run the test suite (below) and say in the pull request which tests you ran.
6. Open a pull request against `main` and describe what it changes and why. The build check runs on every pull request.

## Tests

The Python test suite runs every test script without a window:

```sh
cd APIs/Python/Test_Scripts
python3 Run_Tests.py --exe ../../../macOS/PrePoMax/bin/Debug/net10.0/PrePoMax.com --settings <settings file>
```

The automation server tests need a settings file with an access token, see the [README](README.md#testing).
77 of the 79 scripts pass at the moment; the two failing ones are listed under
[Known limitations](README.md#known-limitations).

## License

PrePoMax is licensed under the GNU General Public License v3, and so is everything contributed to this port.
