/* Licensed to the .NET Foundation under one or more agreements.
   The .NET Foundation licenses this file to you under the MIT license.
   See the LICENSE file in the project root for more information. */

// cl /Wall /WX /wd4255 /wd4668 /wd4710 /wd4204 /wd5045 ./get_host_info.c

#include <assert.h>
#include <stdio.h>
#include <windows.h>

typedef struct LogicalProcessorInfos {
    size_t buffer_size_bytes;
    // NOT AN ARRAY. Points to the first info, use its size to get the next.
    // Caller must free.
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* first_info;
} LogicalProcessorInfos;

static LogicalProcessorInfos get_logical_processor_infos() {
    DWORD buffer_size_bytes = 0;
    LOGICAL_PROCESSOR_RELATIONSHIP relation = RelationAll; // Note: these are not flags, so can't do RelationCache | RelationNumaNode

    // TODO: the documentation at https://docs.microsoft.com/en-us/windows/desktop/api/sysinfoapi/nf-sysinfoapi-getlogicalprocessorinformationex
    // specifies that this writes a number of *bytes* to buffer_size_bytes
    // but on maonisSL01, this sets buffer_size_bytes to 88. Which is the number of elements, not the number of bytes.
    // (`sizeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX)` is 76).
    if (!GetLogicalProcessorInformationEx(relation, NULL, &buffer_size_bytes) && GetLastError() != ERROR_INSUFFICIENT_BUFFER) {
        printf("Failed to get # elements\n");
        return (LogicalProcessorInfos) { 0, NULL };
    }

    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* infos = malloc(buffer_size_bytes);
    assert(infos != NULL);

    DWORD old_buffer_size_bytes = buffer_size_bytes;
    if (!GetLogicalProcessorInformationEx(relation, infos, &buffer_size_bytes))
    {
        printf("Failed to get elements\n");
        free(infos);
        return (LogicalProcessorInfos) { 0, NULL };
    }

    assert(buffer_size_bytes == old_buffer_size_bytes);
    return (LogicalProcessorInfos) { buffer_size_bytes, infos };
}

#define MIN_LEVEL 1
#define MAX_LEVEL 3

typedef struct CacheStatsForLevel
{
    size_t n_caches;
    size_t total_bytes;
} CacheStatsForLevel;

typedef struct CacheStats
{
    size_t numa_nodes;
    size_t n_physical_processors;
    size_t n_logical_processors;
    CacheStatsForLevel levels[MAX_LEVEL + 1]; // index with 1, 2, or 3;
} CacheStats;

static SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* offset(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* ptr, size_t bytes) {
    return (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*) (((char*) ptr) + bytes);
}

static CacheStats getCacheStats()
{
    CacheStats res;
    res.n_physical_processors = 0;
    res.n_logical_processors = 0;
    res.numa_nodes = 0;
    for (size_t level = 0; level <= MAX_LEVEL; level++)
        res.levels[level] = (CacheStatsForLevel) { 0, 0 };

    LogicalProcessorInfos infos = get_logical_processor_infos();
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* info_ptr = infos.first_info;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* end = offset(info_ptr, infos.buffer_size_bytes);

    while (info_ptr < end)
    {
        SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX info = *info_ptr;

        //printf("relationship is %d\n", info.Relationship);
        //ULONG_PTR x = info[i].ProcessorMask;
        switch (info.Relationship)
        {
            case RelationCache:
            {
                CACHE_RELATIONSHIP cache = info.Cache;
                size_t level = cache.Level;
                size_t size = cache.CacheSize;

                // cache.Type; // one of: CacheUnified, CacheInstruction, Cache.Data, CacheTrace

                assert(MIN_LEVEL <= level && level <= MAX_LEVEL);
                res.levels[level].n_caches++;
                res.levels[level].total_bytes += size;
                // printf("L%zd cache: %zd\n", level, size);
                break;
            }
            case RelationNumaNode:
            {
                // NUMA_NODE_RELATIONSHIP nn = info.NumaNode;
                // printf("FOUND A NUMA NODE %ld\n", nn.NodeNumber);
                // DWORD node_number = info.NumaNode.NodeNumber; // This is the only member of that struct.
                res.numa_nodes++;
                break;
            }

            case RelationProcessorCore:
            {
                PROCESSOR_RELATIONSHIP processor = info.Processor;
                BYTE flags = processor.Flags;
                // Assuming only ever 2 hyperthreads
                BOOL hyperthreaded = flags == LTP_PC_SMT;
                if (!hyperthreaded) assert(flags == 0);
                res.n_physical_processors++;
                res.n_logical_processors += (hyperthreaded ? 2 : 1);

                break;
            }
            case RelationProcessorPackage:
            case RelationGroup:
                break;

            default:
                printf("Invalid relationship %d\n", info.Relationship);
                //assert(0); // invalid relationship
                break;
        }

        info_ptr = offset(info_ptr, info.Size);
    }

    assert(info_ptr == end);

    free(infos.first_info); // frees the whole buffer
    return res;
}

int main(void) {
    CacheStats stats = getCacheStats();
    printf("numa_nodes: %zd\n", stats.numa_nodes);
    printf("n_physical_processors: %zd\n", stats.n_physical_processors);
    printf("n_logical_processors: %zd\n", stats.n_logical_processors);
    printf("caches:\n");
    for (size_t level = MIN_LEVEL; level <= MAX_LEVEL; level++) {
        CacheStatsForLevel lStats = stats.levels[level];
        printf("  l%zd: { n_caches: %zd, total_bytes: %zd }\n", level, lStats.n_caches, lStats.total_bytes);
    }
}
