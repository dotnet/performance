# GC Infrastructure MCP Server

A Model Context Protocol (MCP) Server that provides AI assistants with powerful tools to manage and execute .NET runtime performance testing workflows. This server exposes the full GC.Infrastructure toolkit through standardized MCP interfaces, enabling AI-driven automation of garbage collection performance benchmarking and analysis.

## Overview

The GC Infrastructure MCP Server bridges AI assistants and the comprehensive GC.Infrastructure performance testing suite. It provides programmatic access to run GCPerfSim benchmarks, execute microbenchmarks, perform ASP.NET performance testing, build CoreCLR runtimes, and manage version control operations - all through a standardized MCP interface.

### Key Features

- **Performance Benchmarking**: Execute GCPerfSim scenarios, microbenchmarks, and ASP.NET benchmarks
- **Runtime Management**: Build CoreCLR and generate CoreRun executables for testing
- **Version Control Integration**: Git operations for branch management and workflow automation  
- **Test Suite Orchestration**: Create and run comprehensive test suites with automated analysis
- **AI-Driven Workflows**: Enable AI assistants to manage complex performance testing scenarios

## Architecture

The server implements the Model Context Protocol specification and exposes tools across several domains:

### Tool Categories

1. **GCPerfSim Tools** (`GCPerfsim.cs`)
   - Run GCPerfSim benchmarks with various configurations
   - Analyze benchmark results and generate reports
   - Compare performance across different runtime versions

2. **Microbenchmark Tools** (`Microbenchmarks.cs`)
   - Execute BenchmarkDotNet-based microbenchmarks
   - Perform statistical analysis of benchmark results

3. **ASP.NET Benchmark Tools** (`AspNetBenchmarks.cs`)
   - Run web application performance benchmarks using Crank
   - Analyze web performance metrics and throughput

4. **Runtime Build Tools** (`CoreRun.cs`)
   - Build CoreCLR runtime and base class libraries
   - Generate CoreRun executables for performance testing

5. **Version Control Tools** (`VersionControl.cs`)
   - Git branch management and switching
   - Repository state management for testing workflows

6. **Test Suite Management** (`Run.cs`)
   - Create comprehensive test suites
   - Execute multi-stage performance testing workflows
   - Aggregate results across different benchmark types

## Prerequisites

Before running the MCP Server, ensure your environment meets these requirements:

### System Requirements

- **Operating System**: Windows (required for local scenario execution)
- **Administrative Privileges**: Required for registry modifications and system-level operations
- **.NET SDK**: .NET 9.0 or later

### Dependencies

1. **Performance Repository**: Clone the dotnet/performance repository

   ```powershell
   git clone https://github.com/dotnet/performance C:\performance\
   ```

2. **Crank Tool**: Install for ASP.NET benchmarking

   ```powershell
   dotnet tool install Microsoft.Crank.Controller --version "0.2.0-*" --global
   ```

3. **Corporate Network Access**: Required for ASP.NET scenarios (GCPerfSim and microbenchmarks can run offline)

### Build Requirements

Build the infrastructure components:

```powershell
cd C:\performance\src\benchmarks\gc\GC.Infrastructure\GC.Infrastructure
dotnet build -c Release
```

## Getting Started in VS Code

### Method 1: VS Code Settings Configuration

1. **Install MCP Extension**: Install the "MCP Client" extension in VS Code

2. **Configure MCP Server**: Add the following configuration to your VS Code `settings.json`:

```json
{
  "mcp.servers": {
    "gc-infrastructure": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Users\\musharm\\source\\repos\\performance\\src\\benchmarks\\gc\\GC.Infrastructure\\GC.Infrastructure.MCPServer\\GC.Infrastructure.MCPServer.csproj"
      ],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

3. **Start the Server**:
   - Open VS Code Command Palette (`Ctrl+Shift+P`)
   - Run: `MCP: Connect to Server`
   - Select `gc-infrastructure`

### Method 2: Direct Terminal Launch

1. **Open Terminal**: Launch PowerShell as Administrator in VS Code

2. **Navigate to Project Directory**:

```powershell
cd "C:\Users\musharm\source\repos\performance\src\benchmarks\gc\GC.Infrastructure\GC.Infrastructure.MCPServer"
```

3. **Run the Server**:

```powershell
dotnet run
```

The server will start and listen for MCP client connections via stdio transport.

### Method 3: Build and Run Executable

1. **Build the Project**:

```powershell
cd "C:\Users\musharm\source\repos\performance\src\benchmarks\gc\GC.Infrastructure\GC.Infrastructure.MCPServer"
dotnet build -c Release
```

2. **Run the Executable**:

```powershell
cd "C:\performance\artifacts\bin\GC.Infrastructure.MCPServer\Release\net9.0"
.\GC.Infrastructure.MCPServer.exe
```

## Available MCP Tools

Once connected, the following tools become available to AI assistants:

### Runtime Management

- `build_clr_libs` - Build CoreCLR runtime and base class libraries
- `generate_corerun` - Generate CoreRun executable for performance testing

### Performance Benchmarking

- `run_gcperfsim_command` - Execute GCPerfSim benchmark scenarios
- `run_gcperfsim_analyze_command` - Analyze GCPerfSim results
- `run_gcperfsim_compare_command` - Compare GCPerfSim results across versions

### Microbenchmark Testing

- `run_microbenchmarks_command` - Execute BenchmarkDotNet microbenchmarks  
- `run_microbenchmarks_analyze_command` - Analyze microbenchmark results

### Web Performance Testing

- `run_aspnetbenchmarks_command` - Execute ASP.NET performance benchmarks
- `run_aspnetbenchmarks_analyze_command` - Analyze web performance results

### Test Suite Management

- `run_run_command` - Execute comprehensive test suites
- `run_createsuites_command` - Create new test suite configurations
- `run_run-suite_command` - Run individual test suites

### Version Control

- `checkout_branch` - Switch Git branches for testing different versions

## Usage Examples

### Example 1: Running a GCPerfSim Benchmark

```yaml
# Configuration: normal_server.yaml
name: "Normal Server Scenario"
environment:
  server_garbage_collection: true
  
gcperfsim_configurations:
  - name: "normal_server"
    iterations: 5
    
output:
  path: "C:\\InfraRuns\\GCPerfSim\\NormalServer"
```

AI Assistant interaction:

```text
Human: "Run a GCPerfSim benchmark with the normal server configuration"
Assistant: "I'll execute the GCPerfSim benchmark using run_gcperfsim_command with the configuration path..."
```

### Example 2: Building a Runtime and Running Tests

AI workflow:

1. `checkout_branch` - Switch to target runtime branch
2. `build_clr_libs` - Build CoreCLR with specified configuration  
3. `generate_corerun` - Create CoreRun executable
4. `run_gcperfsim_command` - Execute performance benchmarks
5. `run_gcperfsim_analyze_command` - Generate performance analysis

### Example 3: Comprehensive Performance Testing

```yaml
# Configuration: full_suite.yaml
test_suites:
  - gcperfsim
  - microbenchmarks
  - aspnetbenchmarks
  
baseline:
  corerun_path: "C:\\runtime\\baseline\\corerun.exe"
  
comparison:
  corerun_path: "C:\\runtime\\test\\corerun.exe"

output:
  path: "C:\\InfraRuns\\ComparisonRun"
```

The AI assistant can orchestrate the entire workflow:

1. Create test suites with `run_createsuites_command`
2. Execute comprehensive testing with `run_run_command`
3. Analyze results across all benchmark types

## Configuration

### Environment Variables

- `DOTNET_ENVIRONMENT` - Set to "Development" for detailed logging
- `MCP_LOG_LEVEL` - Control MCP server logging verbosity

### Server Configuration

The server automatically configures itself for stdio transport and discovers tools through reflection. No additional configuration files are required.

### Performance Testing Configurations

Benchmark configurations are stored in YAML files and passed to the respective tools. Example configuration locations:

- GCPerfSim: `C:\InfrastructureConfigurations\GCPerfSim\*.yaml`
- Microbenchmarks: `C:\InfrastructureConfigurations\Microbenchmarks\*.yaml`
- ASP.NET: `C:\InfrastructureConfigurations\ASPNetBenchmarks\*.yaml`

## Troubleshooting

### Common Issues

1. **Administrative Privileges Required**
   - Ensure VS Code or terminal is running as Administrator
   - Required for registry modifications and system-level operations

2. **Build Failures**
   - Verify .NET 9.0 SDK is installed
   - Ensure all dependencies are built in Release configuration

3. **MCP Connection Issues**
   - Check that the MCP Client extension is properly installed
   - Verify the server configuration in VS Code settings
   - Ensure the project path is correct in the configuration

4. **Performance Test Failures**
   - Verify CoreCLR runtime is built successfully
   - Check that benchmark configurations exist and are valid
   - Ensure sufficient disk space for output results

### Logging and Diagnostics

The server provides detailed logging to stderr, which can be captured by MCP clients. Enable verbose logging by setting:

```json
{
  "env": {
    "DOTNET_ENVIRONMENT": "Development",
    "MCP_LOG_LEVEL": "Debug"
  }
}
```

## Integration with AI Assistants

The MCP Server enables sophisticated AI-driven performance testing workflows:

### Automated Performance Regression Detection

AI assistants can:

1. Checkout different runtime branches
2. Build and test each version
3. Compare performance metrics
4. Generate regression reports

### Continuous Integration Integration

- Integrate with CI/CD pipelines for automated performance testing
- Generate performance reports for code reviews
- Alert on performance regressions

### Research and Development Support

- Facilitate performance research experiments
- Automate A/B testing of runtime changes
- Generate comprehensive performance analysis reports

## Related Resources

- [GC.Infrastructure Documentation](../README.md)
- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [.NET Performance Repository](https://github.com/dotnet/performance)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Crank Benchmarking Tool](https://github.com/dotnet/crank)
