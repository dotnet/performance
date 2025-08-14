# .NET Performance Repository

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

This repository is a specialized .NET performance benchmarking infrastructure with specific timing requirements and network dependencies.

### Bootstrap Environment (CRITICAL FIRST STEPS)

- **NEVER CANCEL: Each step has specific timing requirements**
- Install the specific .NET SDK version required by this repository:

  ```bash
  python3 scripts/dotnet.py install --channels main --dotnet-versions 10.0.100-preview.7.25322.101 --install-dir ./tools/dotnet -v
  ```

  Takes ~15 seconds. NEVER CANCEL.

- Install essential Python dependencies:

  ```bash
  pip3 install gitpython --user
  ```

  Takes ~5 seconds.

- **CRITICAL: Set up environment variables for EVERY bash session:**

  ```bash
  export PATH="$PWD/tools/dotnet:$PATH"
  export DOTNET_ROOT="$PWD/tools/dotnet"
  export DOTNET_CLI_TELEMETRY_OPTOUT='1'
  export DOTNET_MULTILEVEL_LOOKUP='0'
  export UseSharedCompilation='false'
  ```

  **NOTE: These environment variables must be set in every new terminal session. Use $PWD for absolute paths.**

### Build System (NETWORK DEPENDENT - EXPECT FAILURES)

- **CRITICAL: Build system requires access to Microsoft Azure DevOps package feeds which are often unavailable in sandboxed environments**
- Main build command (expect network failures):

  ```bash
  ./eng/common/build.sh --restore --build --verbosity minimal
  ```

  Takes 3-5 minutes before failing due to network connectivity. NEVER CANCEL. Set timeout to 10+ minutes.

- **IMPORTANT: If build fails with "Could not resolve SDK Microsoft.DotNet.Arcade.Sdk" - this is expected in isolated environments**
- The repository uses Microsoft.DotNet.Arcade.Sdk which requires internal Microsoft package feeds

### Scenario Tests (CORE FUNCTIONALITY)

Scenario tests are the primary way to measure performance and ARE functional even when builds fail:

- **Initialize scenario environment (REQUIRED FIRST):**

  ```bash
  cd src/scenarios
  source init.sh -dotnetdir ../../tools/dotnet
  ```

  Sets PYTHONPATH and environment. Takes ~1 second.

- **Run a basic scenario test:**

  ```bash
  cd emptyconsoletemplate
  python3 pre.py default        # Create test project - takes ~1 second
  python3 test.py startup       # Run performance test - takes 5+ minutes. NEVER CANCEL. Set timeout to 15+ minutes.
  python3 post.py              # Cleanup - takes ~1 second
  ```

- **NEVER CANCEL scenario test commands** - they perform complex measurements and can take 5-15 minutes
- Scenarios may fail due to network issues but will provide valuable timing information

### Python Benchmarking Scripts

- **Main benchmarking script:**

  ```bash
  python3 scripts/benchmarks_local.py --allow-non-admin-execution --help
  ```

  Primary tool for local performance testing. Requires `--allow-non-admin-execution` flag in non-admin environments.

- **CRITICAL: Most Python scripts require network access to download .NET runtime builds**
- Scripts are designed for comprehensive performance measurement, not quick validation

### Linting and Validation

- **Install and run markdownlint:**

  ```bash
  npm i -g markdownlint-cli     # Takes ~30 seconds. NEVER CANCEL. Set timeout to 60+ minutes.
  markdownlint "**/*.md"        # Takes ~20 seconds. Shows many existing issues in reports/ directory
  ```

### Validation Scenarios

When making changes, ALWAYS test:

1. **Scenario workflow validation:**

   ```bash
   cd src/scenarios && source init.sh -dotnetdir ../../tools/dotnet
   cd emptyconsoletemplate && python3 pre.py default && python3 post.py
   ```

   Verifies the scenario infrastructure works.

2. **Python script validation:**

   ```bash
   python3 scripts/benchmarks_local.py --allow-non-admin-execution --help
   ```

   Confirms Python environment is properly configured.

3. **Documentation linting:**

   ```bash
   markdownlint "docs/*.md"
   ```

   Validates documentation changes.

## Network Connectivity Limitations

- **EXPECT BUILD FAILURES** in sandboxed environments due to:
  - Inability to access pkgs.dev.azure.com (Microsoft internal package feeds)
  - SSL certificate validation issues
  - Missing Microsoft.DotNet.Arcade.Sdk package

- **WORKAROUND: Focus on scenario tests and Python scripts** which work with the locally installed .NET SDK

## Repository Structure Overview

### Key Directories

- `src/benchmarks/micro/` - BenchmarkDotNet micro-benchmarks
- `src/benchmarks/gc/` - Garbage collection performance tests  
- `src/scenarios/` - End-to-end scenario performance tests (60+ different scenarios)
- `scripts/` - Python automation for benchmarking workflows
- `docs/` - Comprehensive documentation (see docs/README.md)

### Important Files

- `global.json` - Specifies required .NET SDK version (10.0.100-preview.7.25322.101)
- `requirements.txt` - Python dependencies (some may not install due to version conflicts)
- `.markdownlint.json` - Markdown linting configuration
- `src/scenarios/init.sh` - Essential environment setup for scenarios

## Common Tasks Reference

### Repository Root Structure

```text
docs/               # Documentation
eng/               # Build engineering (Arcade-based)
scripts/           # Python automation scripts  
src/benchmarks/    # Performance benchmarks
src/scenarios/     # Scenario tests (primary functionality)
src/tools/         # Supporting tools and harnesses
.github/           # GitHub workflows and templates
tools/dotnet/      # Local .NET SDK installation (created by scripts)
```

### Frequently Used Commands

**CRITICAL: Always set environment variables first in every session:**

```bash
# Essential environment setup (run in every new session)
export PATH="$PWD/tools/dotnet:$PATH"
export DOTNET_ROOT="$PWD/tools/dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT='1'
export DOTNET_MULTILEVEL_LOOKUP='0'
export UseSharedCompilation='false'

# Scenario test pattern (most important workflow)
cd src/scenarios && source init.sh -dotnetdir ../../tools/dotnet
cd [scenario-name] && python3 pre.py [command] && python3 test.py [test-type] && python3 post.py

# View available scenarios
ls src/scenarios/

# Check .NET installation  
dotnet --version    # Should show 10.0.100-preview.7.25322.101

# Lint documentation
markdownlint "docs/*.md"

# Run benchmarks script
python3 scripts/benchmarks_local.py --allow-non-admin-execution --help
```

### Expected Timings (NEVER CANCEL)

- .NET SDK install: ~15 seconds
- npm package installs: 30+ seconds (set timeout to 60+ minutes)
- Scenario pre-commands: ~1 second
- Scenario test commands: 5-15 minutes (set timeout to 30+ minutes)  
- Build attempts: 3-5 minutes (usually fail due to network)
- Markdownlint full repo: ~20 seconds

## Working Around Network Limitations

When network access is limited:

1. **Focus on local scenario tests** - these work with the locally installed SDK
2. **Use existing documentation** rather than trying to build from source
3. **Examine Python scripts** for understanding workflow patterns
4. **Test markdown linting** for documentation changes
5. **Avoid attempting full repository builds** - they require Microsoft internal package feeds

## Critical Reminders

- **NEVER CANCEL long-running operations** - this is a performance measurement repository where timing is critical
- **Always set appropriate timeouts** (60+ minutes for builds, 30+ minutes for scenarios)
- **Network failures are expected** in sandboxed environments - focus on local tooling
- **Scenario tests are the primary functional component** even when builds fail
- **Python environment setup is essential** for most repository operations
