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
    printf(
        "Memory load is %d%%. Total phys GB is %f, avail phys GB is %f\n",
        (int) round(100 * get_memory_used_fraction_from_mem(mem)),
        bytes_to_gb(mem.total_physical_bytes),
        bytes_to_gb(mem.available_bytes)
    );
}

typedef struct Args
{
    double desired_mem_usage_fraction;
    bool never_release;
} Args;

const char* USAGE = "Usage: make_memory_load.exe -percent 50 [-neverRelease]\n";

static int parse_args(Args* args, const int argc, char** argv)
{
    double percent = 0.0;
    bool never_release = FALSE;

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

    *args = (Args) { .desired_mem_usage_fraction = percent / 100, .never_release = never_release };
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

static Mem init_mem(const Args args)
{
    SYSTEM_INFO system_info;
    GetSystemInfo(&system_info);
    const size_t page_size = system_info.dwPageSize;
    const size_t allocation_granularity = system_info.dwAllocationGranularity;
    
    const size_t total_phys_bytes = get_memory_status().total_physical_bytes;
    const size_t desired_available_bytes = (size_t) round((1.0 - args.desired_mem_usage_fraction) * total_phys_bytes);

    const size_t total_memory = round_down_to_nearest(total_phys_bytes, allocation_granularity);

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

//#define VERBOSE TRUE

// Returns true if it did adjust
static bool adjust(Mem* mem)
{
    assert(is_multiple(mem->total_memory_committed, mem->allocation_granularity));
    assert(is_multiple(mem->total_memory_reset, mem->allocation_granularity));
    assert(is_multiple(mem->total_memory, mem->allocation_granularity));

    const size_t current_available_bytes = get_current_available_bytes();

    const ptrdiff_t to_allocate = round_to_nearest((ptrdiff_t) current_available_bytes - (ptrdiff_t) mem->desired_available_bytes, mem->allocation_granularity);

#if VERBOSE
    printf("Current available GB: %.2fGB, desired: %.2fGB\n", bytes_to_gb(current_available_bytes), bytes_to_gb(mem->desired_available_bytes));
#endif

    if (to_allocate > 0)
    {
        // unreset / commit some more
        if (mem->total_memory_reset != 0)
        {
#if VERBOSE
            printf("Unresetting memory: %.2fMB\n", bytes_to_mb(to_allocate));
#endif

            // Unreset the memory
            const size_t size = min(mem->total_memory_reset, (size_t)to_allocate);
            byte* const start = mem->memory + mem->total_memory_committed;
            //VirtualAlloc(start, size, MEM_RESET_UNDO, PAGE_READWRITE);
            // Just write to it to bring it back
            write_to_every_page(start, size, mem->page_size);
            mem->total_memory_committed += size;
            mem->total_memory_reset -= size;
        }
        else
        {
#if VERBOSE
            printf("Committing more memory: %.2fMB\n", bytes_to_mb(to_allocate));
#endif

            // Commmit some more memory
            const size_t size = min(mem->total_memory - mem->total_memory_committed, (size_t)to_allocate);
            byte* const start = mem->memory + mem->total_memory_committed;
            VirtualAlloc(start, size, MEM_COMMIT, PAGE_READWRITE);
            write_to_every_page(start, size, mem->page_size);
            mem->total_memory_committed += size;
        }

        return TRUE;
    }
    else if (to_allocate < 0)
    {
        if (mem->args.never_release)
        {
            return FALSE;
        }
        else
        {
            // Reset some memory
            const size_t size = min((size_t) -to_allocate, mem->total_memory_committed);
            const size_t new_total_memory_committed = mem->total_memory_committed - size;

#if VERBOSE
        printf("Resetting memory: want to reset %.2fMB, actual size to reset %.2fMB\n", bytes_to_mb(-to_allocate), bytes_to_mb(size));
#endif

            // Note: last argument is ignored
            VirtualAlloc(mem->memory + new_total_memory_committed, size, MEM_RESET, PAGE_NOACCESS);

            // https://docs.microsoft.com/en-us/windows/desktop/api/memoryapi/nf-memoryapi-virtualunlock
            // This will fail every time, but has a side effect of releasing the pages from the working set
            const bool unlocked = VirtualUnlock(mem->memory + new_total_memory_committed, size);
            assert(!unlocked);

            mem->total_memory_committed = new_total_memory_committed;
            mem->total_memory_reset += size;
            return TRUE;
        }
    }
    else
    {
        return FALSE;
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
    // Just to be sure, adjust twice with a sleep in between.
    int attempts = 0;
    while (TRUE) {
        if (!adjust(&mem))
        {
            // No need to adjust
            break;
        }
        if (attempts == 5)
        {
            fprintf(stderr, "Failed to get memory to the desired load after 5 attempts\n");
            return 1;
        }
        attempts++;
    }

    // NOTE: code in run_single_test.py is waiting for exactly this line to print to stderr.
    fprintf(stderr, "make_memory_load finished starting up\n");
    
    // print_memory_status();

    while (TRUE)
    {
        Sleep(100); // milliseconds
        adjust(&mem);
    }

    return 0;
}
