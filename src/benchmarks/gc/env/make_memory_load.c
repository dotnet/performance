/* Licensed to the .NET Foundation under one or more agreements.
   The .NET Foundation licenses this file to you under the MIT license.
   See the LICENSE file in the project root for more information. */

// Open the "x64 native tools command prompt", then:
// cl.exe /Wall /WX /wd4255 /wd4668 /wd4710 /wd4204 /wd5045 ./make_memory_load.c

#include <assert.h> // assert
#include <math.h> // floor
#include <stdio.h> // printf
#include <windows.h>

#include "./util.h"

typedef BOOL bool;

static size_t to_size_t(const DWORDLONG l)
{
    assert(l < SIZE_MAX);
    return (size_t) l;
}

static bool is_multiple(const size_t a, const size_t b)
{
    return a % b == 0;
}

static size_t div_round_up(const size_t a, const size_t b)
{
    assert(b != 0);
    const size_t div = a / b;
    return is_multiple(a, b) ? div : div + 1;
}

// Round 'a' to the nearest 'b', rounding towards 0.
// Example inputs:
//  (1, 2) -> 0
//  (3, 2) -> 2
//  (-1, 2) -> 0
//  (-3, 2) -> -2
static ptrdiff_t round_to_nearest(const ptrdiff_t a, const size_t b)
{
    assert(b > 1);
    const ptrdiff_t bb = (ptrdiff_t) b;
    if (a > 0)
        return ((a + (bb / 2) - 1) / bb) * bb;
    else
        return ((a - (bb / 2) + 1) / bb) * bb;
}

static size_t round_down_to_nearest(const size_t a, const size_t b)
{
    return (a / b) * b;
}

typedef struct Memory
{
    size_t total_physical_bytes;
    size_t available_bytes;
} Memory;

static Memory get_memory_status()
{
    // https://docs.microsoft.com/en-us/windows/desktop/api/sysinfoapi/ns-sysinfoapi-_memorystatusex
    MEMORYSTATUSEX statex;
    statex.dwLength = sizeof(statex);
    bool success = GlobalMemoryStatusEx(&statex);
    assert(success);
    return (Memory) {
        .total_physical_bytes = to_size_t(statex.ullTotalPhys),
        .available_bytes = to_size_t(statex.ullAvailPhys)
    };
}

static Memory change_available_bytes(const Memory mem, const size_t delta_available_bytes)
{
    return (Memory) { .total_physical_bytes = mem.total_physical_bytes, .available_bytes = mem.available_bytes + delta_available_bytes };
}

static double get_memory_used_fraction_from_mem(const Memory m)
{
    return 1.0 - (((double) m.available_bytes) / ((double)m.total_physical_bytes));
}

static size_t get_current_available_bytes()
{
    return get_memory_status().available_bytes;
}

static void print_memory_status()
{
    const Memory mem = get_memory_status();
    puts("\n=======================");
    puts("Memory Status Achieved:");
    puts("=======================");
    printf("Memory Load Achieved: %d%%\n",
           (int) round(100 * get_memory_used_fraction_from_mem(mem)));
    printf("Total Physical Memory: %f GB\n",     bytes_to_gb(mem.total_physical_bytes));
    printf("Available Physical Memory: %f GB\n", bytes_to_gb(mem.available_bytes));
}

typedef struct Args
{
    double desired_mem_usage_fraction;
    bool never_release;
    bool no_readjust;
} Args;

const char* USAGE = "Usage: make_memory_load.exe -percent 50 [-neverRelease] [-noReadjust]\n";

static int parse_args(Args* args, const int argc, char** argv)
{
    double percent = 0.0;
    bool never_release = FALSE;
    bool no_readjust = FALSE;

    for (int i = 1; i < argc; i++)
    {
        if (streq(argv[i], "-percent"))
        {
            assert(i + 1 < argc);
            i++;
            const int err = parse_double(argv[i], &percent, 0, 100);
            if (err) return err;
        }
        else if (streq(argv[i], "-neverRelease"))
        {
            never_release = TRUE;
        }
        else if (streq(argv[i], "-noReadjust"))
        {
            no_readjust = TRUE;
        }
        else
        {
            return fail(USAGE);
        }
    }

    if (percent == 0.0)
    {
        return fail(USAGE);
    }
    if (!(0 < percent && percent <= 99))
    {
        return fail("Percent must be > 0 and <= 99\n");
    }

    *args = (Args) { .desired_mem_usage_fraction = percent / 100, .never_release = never_release, .no_readjust = no_readjust };
    return 0;
}

typedef struct Mem
{
    const Args args;
    const size_t desired_available_bytes;
    const size_t page_size;
    const size_t allocation_granularity;

    // Memory is laid out as: committed | reset | reserved.
    // We may reduce memory by expanding 'reset' into 'committed'
    // by calling VirtualAlloc with MEM_RESET and using VirtualUnlock to release the pages from the working set

    // We increase memory by:
    //   If it was previously reduced: We can unreset that memory by writing to it
    //   Else: Commit some more of the virtual memory
    size_t total_memory_committed;
    size_t total_memory_reset;
    // invariant: total_memory_committed + total_memory_reset <= total_memory
    const size_t total_memory; // Total size of `byte* memory`. Also total physical memory on the system.

    byte* const memory;
} Mem;

// #define VERBOSE TRUE

static Mem init_mem(const Args args)
{
    SYSTEM_INFO system_info;
    GetSystemInfo(&system_info);
    const size_t page_size = system_info.dwPageSize;
    const size_t allocation_granularity = system_info.dwAllocationGranularity;
    
    const size_t total_phys_bytes = get_memory_status().total_physical_bytes;
    const size_t desired_available_bytes = (size_t) round((1.0 - args.desired_mem_usage_fraction) * total_phys_bytes);

    const size_t total_memory = round_down_to_nearest(total_phys_bytes, allocation_granularity);

#if VERBOSE
    puts("\n====================");
    puts("System Information:");
    puts("====================");
    printf("Desired Available Memory: %.2f MB\n", bytes_to_mb(desired_available_bytes));
    printf("Page Size: %.2f KB\n",                bytes_to_kb(page_size));
    printf("Allocation Granularity: %.2f KB\n",   bytes_to_kb(allocation_granularity));
    printf("Allocation Granularity: %zu\n",       allocation_granularity);
    printf("Total Physical Memory: %.2f MB\n",    bytes_to_mb(total_phys_bytes));
    printf("Total Memory: %.2f MB\n",             bytes_to_mb(total_memory));
#endif

    LPVOID alloced = VirtualAlloc(NULL, total_memory, MEM_RESERVE, PAGE_READWRITE);
    assert(alloced);

    return (Mem) {
        .args = args,
        .desired_available_bytes = desired_available_bytes,
        .page_size = page_size,
        .allocation_granularity = allocation_granularity,
        .total_memory_committed = 0,
        .total_memory_reset = 0,
        .total_memory = total_memory,
        .memory = (byte*) alloced,
    };
}

static void write_to_every_page(byte* start, size_t size, size_t page_size)
{
    for (size_t i = 0; i < size; i += page_size)
    {
        start[i] = (byte)(i % 256);
    }
}

typedef enum AdjustKind {
    adjust_kind_no_adjust,
    adjust_kind_within_threshold,
    adjust_kind_was_too_low,
    adjust_kind_was_too_high,
    adjust_kind_was_too_high_and_cant_free,
} AdjustKind;

const char* adjust_kind_to_string(AdjustKind ak)
{
    switch (ak)
    {
        case adjust_kind_no_adjust:
            return "no adjust";
        case adjust_kind_within_threshold:
            return "within threshold";
        case adjust_kind_was_too_low:
            return "too low";
        case adjust_kind_was_too_high:
            return "too high";
        case adjust_kind_was_too_high_and_cant_free:
            return "too high, and make_memory_load has nothing to free";
        default:
            assert(0);
            return "<<error>>";
    }
}

// Returns true if it did adjust
static AdjustKind adjust(Mem* mem, int iteration_num)
{
    assert(is_multiple(mem->total_memory_committed, mem->allocation_granularity));
    assert(is_multiple(mem->total_memory_reset, mem->allocation_granularity));
    assert(is_multiple(mem->total_memory, mem->allocation_granularity));

    const double current_memload_fraction = get_memory_used_fraction_from_mem(get_memory_status());
    const double current_memload_delta = fabs(mem->args.desired_mem_usage_fraction - current_memload_fraction);

#if VERBOSE
    printf("\nCurrent Memory Load: %.4f%%\n", current_memload_fraction * 100.0);
    printf("Desired Memory Load: %.4f%%\n", mem->args.desired_mem_usage_fraction * 100.0);
#endif

    // If our current memory load is less than 0.01% different from the desired
    // one, we can consider it good enough to start/continue running our tests
    // and get accurate traces. There's no need to further adjust the memory
    // load at this point.
    if (current_memload_delta < 0.0001)
    {
        return adjust_kind_no_adjust;
    }

    const size_t current_available_bytes = get_current_available_bytes();
    const ptrdiff_t to_allocate = round_to_nearest(
        (ptrdiff_t) current_available_bytes - (ptrdiff_t) mem->desired_available_bytes,
        mem->allocation_granularity
    );

#if VERBOSE
    printf("Current Available Memory: %.2f MB\n", bytes_to_mb(current_available_bytes));
    printf("Desired Available Memory: %.2f MB\n", bytes_to_mb(mem->desired_available_bytes));
#endif

    // If we reach the final attempt to load the desired amount of memory, and
    // we are not there yet but within an acceptable tolerance range, then allow
    // the program to continue running rather than failing.
    //
    // The tolerance range is currently set to +-5% of the desired available
    // memory, and is defined in util.h inside the current directory.
    if (to_allocate != 0 && iteration_num == MEMORY_LOAD_NUM_ATTEMPTS)
    {
#if VERBOSE
        double acc_mem_delta =   ((double) mem->desired_available_bytes)
                               * ((double) ACCEPTABLE_MEMORY_DELTA_PCT / 100.0);
        double current_delta = fabs(
            ((double) current_available_bytes) - ((double) mem->desired_available_bytes)
        );
        printf("Acceptable Memory Delta: %.2f MB\n", bytes_to_mb(acc_mem_delta));
        printf("Current Memory Delta: %.2f MB\n",  bytes_to_mb(current_delta));
#endif

        if ((current_memload_delta * 100.0) < ACCEPTABLE_MEMORY_DELTA_PCT)
        {
            return adjust_kind_within_threshold;
        }
    }

    if (to_allocate > 0)
    {
        // unreset / commit some more
        if (mem->total_memory_reset != 0)
        {
            // Unreset the memory
            const size_t size = min(mem->total_memory_reset, (size_t)to_allocate);

#if VERBOSE
            printf("Unresetting Memory: %.2f MB\n\n", bytes_to_mb(size));
#endif

            byte* const start = mem->memory + mem->total_memory_committed;
            //VirtualAlloc(start, size, MEM_RESET_UNDO, PAGE_READWRITE);
            // Just write to it to bring it back
            write_to_every_page(start, size, mem->page_size);
            mem->total_memory_committed += size;
            mem->total_memory_reset -= size;
        }
        else
        {
            // Commmit some more memory
            const size_t size = min(mem->total_memory - mem->total_memory_committed, (size_t)to_allocate);

#if VERBOSE
            printf("Committing Memory: %.2f MB\n\n", bytes_to_mb(size));
#endif

            byte* const start = mem->memory + mem->total_memory_committed;
            VirtualAlloc(start, size, MEM_COMMIT, PAGE_READWRITE);
            write_to_every_page(start, size, mem->page_size);
            mem->total_memory_committed += size;
        }

        return adjust_kind_was_too_low;
    }
    else if (to_allocate < 0)
    {
        if (mem->args.never_release)
        {
            return adjust_kind_no_adjust;
        }
        else
        {
            // Reset some memory
            const size_t size = min((size_t) -to_allocate, mem->total_memory_committed);
            const size_t new_total_memory_committed = mem->total_memory_committed - size;

#if VERBOSE
            printf("Want to Reset Memory: %.2f MB\n", bytes_to_mb(-to_allocate));
            printf("Actual Reset: %.2f MB\n\n", bytes_to_mb(size));
#endif

            // Note: last argument is ignored
            VirtualAlloc(mem->memory + new_total_memory_committed, size, MEM_RESET, PAGE_NOACCESS);

            // https://docs.microsoft.com/en-us/windows/desktop/api/memoryapi/nf-memoryapi-virtualunlock
            // This will fail every time, but has a side effect of releasing the pages from the working set
            const bool unlocked = VirtualUnlock(mem->memory + new_total_memory_committed, size);
            assert(!unlocked);

            mem->total_memory_committed = new_total_memory_committed;
            mem->total_memory_reset += size;
            return size == 0 ? adjust_kind_was_too_high_and_cant_free : adjust_kind_was_too_high;
        }
    }
    else
    {
#if VERBOSE
        puts("Success!");
#endif
        return adjust_kind_no_adjust;
    }
}

// Will run forever, must be shut down with ctrl-c
int main(const int argc, char** argv)
{
    Args args;
    const int err = parse_args(&args, argc, argv);
    if (err) return err;

    // print_memory_status();

    Mem mem = init_mem(args);

    // Initial adjust may take a while as it writes to all the pages.
    // Therefore, do various attempts to get the right load going.
    int attempts = 0;

    while (TRUE) {

#if VERBOSE
        printf("\nITERATION NUMBER: %d\n", attempts + 1);
#endif

        // If by the last attempt to get the desired memory load we have an
        // acceptable difference (+-5%), then allow the test to continue with a
        // message warning the user of this delta.
        //
        // NOTE: DO NOT add commas to any fprintf messages. Run_single_test.py
        // expects a very specific format depending on the AdjustKind result here.
        AdjustKind ak = adjust(&mem, attempts);
        if (ak == adjust_kind_no_adjust)
        {
            // No need to adjust as we got the memory load we required.
            // Do NOT change this message, as run_single_test.py is expecting it
            // to know everything went fine here.
            fprintf(stderr, "make_memory_load finished starting up\n");
            break;
        }
        else if (ak == adjust_kind_within_threshold)
        {
            // No need to adjust as we are within the acceptable memory load threshold.
            // This is the ONLY fprintf message that should have ONE comma. This
            // is to separate the message from the achieved memory load percentage,
            // which is later parsed by run_single_test.py.
            fprintf(
                stderr,
                "threshold memory achieved,%.5f\n",
                get_memory_used_fraction_from_mem(get_memory_status()) * 100.0
            );
            break;
        }
        else if (attempts == MEMORY_LOAD_NUM_ATTEMPTS)
        {
            fprintf(
                stderr,
                "Failed to get memory to the desired load (%d%%) after %d attempts: Last memory was %s\n",
                (int) round(args.desired_mem_usage_fraction * 100),
                attempts,
                adjust_kind_to_string(ak));
            return 1;
        }
        attempts++;
    }

#if VERBOSE
    print_memory_status();
#endif

    while (TRUE)
    {
        Sleep(100); // milliseconds
        if (!args.no_readjust)
        {
            // Memory adjustment has to happen throughout the test, so we simply
            // no longer take into account the number of iterations.
            adjust(&mem, 0);
        }
    }

    return 0;
}
