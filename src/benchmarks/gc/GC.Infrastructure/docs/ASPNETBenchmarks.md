# ASP.NET Benchmarks

## Troubleshooting

There are four main types of errors while running ASP.NET Benchmarks using crank and these are:

### 1. Inability To Connect To The Server

The typical error message associated with this is of the following form:

```cmd
The specified endpoint url 'http://asp-citrine-win:5001' for 'application' is invalid or not responsive: "No such host is known. (asp-citrine-win:5001)"
```

This is as a result of not being connected to CorpNet. To troubleshoot this issue, ensure you are connected to CorpNet by making sure your VPN is appropriately set:
![image](./images/CorpNetConnected.png)

Additionally, the reason for the error could be because the associated machine is down. Reaching out to the appropriate ASP.NET machine owners is the best option here.

### 2. Incorrect Crank Arguments

Fix arguments by referring to [this](https://github.com/dotnet/crank/blob/main/src/Microsoft.Crank.Controller/README.md) document. If you are still experiencing issues even though you have checked that the crank commands are correct, ensure that you have the latest version of crank.

### 3. Test Failures

The test failures could be one of the following:

#### 1. Build Failures

To confirm this is the case, check the ``*build.log`` file associated with the run. The resolution here is to check with the test owners.

#### 2. Runtime Test Failures

These will show up in the following form:

```psh
[STDERR] GC initialization failed with error 0x8007007E
[STDERR] Failed to create CoreCLR, HRESULT: 0x8007007E
```

This issue specifically indicates a version mismatch between the uploaded binaries and the test binaries. If you are connected to CorpNet, ``errors/`` will shed light on the meaning of errors.

#### 3. Test Failures from the Managed Side of Things

To get more details, check the ``*output.log`` file associated with the run. The resolution is usually to check if the framework version you are trying to run matches with the run and if that doesn't turn out to be the case, reach out to the test owners.

### 4. Missing Artifacts

For the case of missing artifacts such as missing traces, examine the log file for the exception reasons.
