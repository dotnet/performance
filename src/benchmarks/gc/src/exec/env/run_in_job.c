/* Licensed to the .NET Foundation under one or more agreements.
   The .NET Foundation licenses this file to you under the MIT license.
   See the LICENSE file in the project root for more information. */

/*
Usage:
    ./runinjob.exe --memory-mb 123 -- C:/.../program.exe [its args...]
    Runs job some/program.exe with 123MB.
    Note the program to run should be an absolute path.

To compile:
    build.py should compile this. To compile standalone:

    # C4255, C4668: Appear in windows headers
    # C4710: Notifies that some functions are not inlined
    # C4202: Doesn't let us initialize structs with non-constant expressions
    # C5045: Informational spectre mitigation
    cl /Wall /WX /wd4255 /wd4668 /wd4710 /wd4204 /wd5045 ./run_in_job.c
*/

#include <assert.h>
#include <stdio.h>
#include <string.h>
#include <windows.h>

#include "./util.h"

#define MAX_ERROR_TEXT_SIZE 255

static int fail_last_error(const char* desc, DWORD error_code)
{
    fprintf(stderr, "Failure in %s -- error code %lu\n", desc, error_code);
    char error_text[MAX_ERROR_TEXT_SIZE + 1];
    DWORD f = FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, error_code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), error_text, MAX_ERROR_TEXT_SIZE, NULL);
    return f == 0 ? fail("Couldn't format error message\n") : fail(error_text);
}

static JOBOBJECT_EXTENDED_LIMIT_INFORMATION get_limit_info(const size_t memoryLimitBytes)
{
    JOBOBJECT_BASIC_LIMIT_INFORMATION basic = (JOBOBJECT_BASIC_LIMIT_INFORMATION)
    {
        .PerProcessUserTimeLimit = 0,
            .PerJobUserTimeLimit = 0,
            // If flags are not set, fields are ignored
            // TODO: setting JOB_OBJECT_LIMIT_WORKINGSET causes it to fail with error code 87 "The parameter is incorrect."
            .LimitFlags =
                JOB_OBJECT_LIMIT_JOB_MEMORY |
                JOB_OBJECT_LIMIT_PROCESS_MEMORY |
                // This is a convenience that kills the job when runinjob is killed
                JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
            // TODO: these are ignored right now, see above
            .MinimumWorkingSetSize = kb_to_bytes(10.0), // 10k is a good lower bound.
            .MaximumWorkingSetSize = memoryLimitBytes,
            .ActiveProcessLimit = 0,
            .Affinity = 0,
            .PriorityClass = 0,
            .SchedulingClass = 0
    };
    return (JOBOBJECT_EXTENDED_LIMIT_INFORMATION)
    {
        .BasicLimitInformation = basic,
        .IoInfo = 0,
        .ProcessMemoryLimit = memoryLimitBytes,
        .JobMemoryLimit = memoryLimitBytes,
        .PeakProcessMemoryUsed = memoryLimitBytes,
        .PeakJobMemoryUsed = memoryLimitBytes,
    };
}

int createJobObject(HANDLE* hJobPtr, const size_t memoryLimitBytes, const double cpuRateHardCap)
{
    HANDLE hJob = CreateJobObject(NULL, "experimental job object");
    *hJobPtr = hJob;
    if (hJob == NULL)
    {
        return fail_last_error("CreateJobObject", GetLastError());
    }

    if (memoryLimitBytes != 0)
    {
        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = get_limit_info(memoryLimitBytes);

        if (!SetInformationJobObject(hJob, JobObjectExtendedLimitInformation, &limitInfo, sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION)))
        {
            return fail_last_error("SetInformationJobObject for JobObjectExtendedLimitInformation", GetLastError());
        }
    }

    if (cpuRateHardCap != 0.0)
    {
        // From https://docs.microsoft.com/en-us/windows/desktop/api/winnt/ns-winnt-_jobobject_cpu_rate_control_information
        // "Set CpuRate to a percentage times 100. For example, to let the job use 20% of the CPU, set CpuRate to 20 times 100, or 2,000.""
        const DWORD cpuRatePercentageTimes100 = (DWORD) (cpuRateHardCap * 100.0 * 100.0);
        JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuRateInfo = (JOBOBJECT_CPU_RATE_CONTROL_INFORMATION) {
            // Will give us 'invalid parameter' if we're missing JOB_OBJECT_CPU_RATE_CONTROL_ENABLE
            .ControlFlags = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP,
            .CpuRate = cpuRatePercentageTimes100,
        };
        if (!SetInformationJobObject(hJob, JobObjectCpuRateControlInformation, &cpuRateInfo, sizeof(JOBOBJECT_CPU_RATE_CONTROL_INFORMATION)))
        {
            return fail_last_error("SetInformationJobObject for JobObjectCpuRateControlInformation", GetLastError());
        }
    }

    return 0;
}


#define MAX_COMMAND_LINE_SIZE 1024

typedef struct Args
{
    size_t memoryLimitBytes; // 0 for do not set memory limit
    double cpuRateHardCap; // 0 for do not set cap
    const char* commandName;
    BOOL affinitize;
    char fullCommandLine[MAX_COMMAND_LINE_SIZE];
} Args;

static int parse_args(const int argc, char** argv, Args* args)
{
    if (argc <= 2)
    {
        return fail("Usage: runinjob.exe --memory-mb 123 --cpu-rate-hard-cap 2.0 -- C:/.../program.exe [its args...]\n");
    }

    ZeroMemory(args, sizeof(Args));

    // argv[0] will be the path to this script
    for (int i = 1; i < argc; i++)
    {
        const char* arg = argv[i];

        if (streq(arg, "--memory-mb"))
        {
            i++;
            assert(i < argc);
            double memoryLimitMb = 0.0;
            const int err = parse_double(argv[i], &memoryLimitMb, 1, 100000);
            if (err) return err;
            args->memoryLimitBytes = mb_to_bytes(memoryLimitMb);
        }
        else if (streq(arg, "--cpu-rate-hard-cap"))
        {
            i++;
            assert(i < argc);
            // If the limit is > 1, SetInformationJobObject will fail. 
            const int err = parse_double(argv[i], &args->cpuRateHardCap, 0.01, 1);
            if (err) return err;
        }
        else if (streq(arg, "--affinitize"))
        {
            args->affinitize = TRUE;
        }
        else if (streq(arg, "--"))
        {
            i++;
            assert(i < argc);
            args->commandName = argv[i];
            // Note: fullCommandLine includes commandName
            for (; i < argc; i++)
            {
                const int err = strcat_s(args->fullCommandLine, MAX_COMMAND_LINE_SIZE, argv[i])
                    || strcat_s(args->fullCommandLine, MAX_COMMAND_LINE_SIZE, " ");
                if (err)
                {
                    return fail("Not enough space for command");
                }
            }
        }
        else
        {
            return fail("Unrecognized argument %s", arg);
        }
    }

    if (args->commandName == NULL)
    {
        return fail("Must provide a command to run.");
    }

    return 0;
}

int main(const int argc, char **argv)
{
    // start the process suspended.
    STARTUPINFOA si;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);

    PROCESS_INFORMATION pi;
    ZeroMemory(&pi, sizeof(pi));

    Args args;
    const int parse_args_err = parse_args(argc, argv, &args);
    if (parse_args_err) return parse_args_err;

    if (!CreateProcessA(
        args.commandName,
        args.fullCommandLine,
        /*lpProcessAttributes*/ NULL,
        /*lpThreadAttributes*/ NULL,
        /*bInheritHandles*/ FALSE,
        /*dwCreationFlags*/ CREATE_SUSPENDED,
        /*lpEnvironment*/ NULL,
        /*lpCurrentDirectory*/ NULL,
        /*lpStartupInfo*/ &si,
        /*lpProcessInformation*/ &pi))
    {
        return fail_last_error("CreateProcessA", GetLastError());
    }

    const HANDLE hProcess = pi.hProcess;
    assert(hProcess != NULL);
    const HANDLE hMainThread = pi.hThread;

    if (args.memoryLimitBytes || args.cpuRateHardCap)
    {
        HANDLE hJob;
        const int createJobErr = createJobObject(&hJob, args.memoryLimitBytes, args.cpuRateHardCap);
        if (createJobErr)
        { 
            TerminateProcess(hProcess, 1);
            return createJobErr;
        }

        if (!AssignProcessToJobObject(hJob, hProcess))
        {
            TerminateProcess(hProcess, 1);
            return fail_last_error("AssignProcessToJobObject", GetLastError());
        }
    }

    if (args.affinitize)
    {
        SetProcessAffinityMask(hProcess, 0x2);
        SetPriorityClass(hProcess, HIGH_PRIORITY_CLASS);
    }

    ResumeThread(hMainThread);

    int wait_res = WaitForSingleObject(hProcess, /*dwMilliseconds*/ 999999999);
    if (wait_res != WAIT_OBJECT_0)
        return fail("Process exited unusually\n");

    unsigned long exitCode = 1; // initial value shouldn't matter
    GetExitCodeProcess(hProcess, &exitCode);

    printf("PID: %ld\n", pi.dwProcessId);

    return exitCode;
}
