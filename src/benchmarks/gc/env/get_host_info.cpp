/* Licensed to the .NET Foundation under one or more agreements.
   The .NET Foundation licenses this file to you under the MIT license.
   See the LICENSE file in the project root for more information. */

// cl /Wall /WX /wd4255 /wd4668 /wd4710 /wd4204 /wd5045 ./get_host_info.c

#include <assert.h>
#include <stdio.h>
#include <vector>
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
        return LogicalProcessorInfos { 0, NULL };
    }

    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* infos = static_cast<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*>(malloc(buffer_size_bytes));
    assert(infos != NULL);

    DWORD old_buffer_size_bytes = buffer_size_bytes;
    if (!GetLogicalProcessorInformationEx(relation, infos, &buffer_size_bytes))
    {
        printf("Failed to get elements\n");
        free(infos);
        return LogicalProcessorInfos { 0, NULL };
    }

    assert(buffer_size_bytes == old_buffer_size_bytes);
    return LogicalProcessorInfos { buffer_size_bytes, infos };
}

#define MIN_LEVEL 1
#define MAX_LEVEL 3

typedef struct CacheStatsForLevel
{
    size_t n_caches;
    size_t total_bytes;
} CacheStatsForLevel;

struct RangeIter {
    size_t i;

    size_t operator*() const {
        return i;
    }

    bool operator!=(const RangeIter other) const {
        return i != other.i;
    }

    RangeIter& operator++() {
        ++i;
        return *this;
    }
};

struct Range {
    // both inclusive
    size_t lo;
    size_t hi;

    RangeIter begin() const { return RangeIter{lo}; }
    RangeIter end() const { return RangeIter{hi + 1}; }
};

struct NumaNodeInfo {
    size_t numa_node_number;
    size_t cpu_group_number;
    std::vector<Range> ranges;
};

typedef struct CacheStats
{
    std::vector<NumaNodeInfo> numa_nodes;
    size_t n_physical_processors = 0;
    size_t n_logical_processors = 0;
    CacheStatsForLevel levels[MAX_LEVEL + 1]; // index with 1, 2, or 3;
} CacheStats;

static SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* offset(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* ptr, size_t bytes) {
    return (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*) (((char*) ptr) + bytes);
}

static bool has_bit(size_t a, size_t bit_index) {
    return (a & (static_cast<size_t>(1) << bit_index)) != 0;
}

static std::vector<Range> ranges_from_mask(const KAFFINITY mask) {
    static_assert(sizeof(mask) == sizeof(size_t), "expecting KAFFINITY == size_t");
    assert(mask != 0);
    std::vector<Range> ranges;
    for (size_t i = 0; i < sizeof(mask) * 8; i++) {
        if (has_bit(mask, i)) {
            if (ranges.empty() || i != ranges.back().hi + 1) {
                ranges.push_back(Range{i, i});
            } else {
                ranges.back().hi = i;
            }
        }
    }
    assert(!ranges.empty());
    return ranges;
}

static void check_numa_nodes_are_correct(const std::vector<NumaNodeInfo>& numa_nodes) {
    for (const NumaNodeInfo& nn : numa_nodes) {
        for (const Range& range : nn.ranges) {
            for (size_t i : range) {
                PROCESSOR_NUMBER pn { static_cast<WORD>(nn.cpu_group_number), static_cast<BYTE>(i), /*reserved*/ 0 };
                USHORT check_numa_node_number;
                BOOL success = GetNumaProcessorNodeEx(&pn, &check_numa_node_number);
                assert(success);
                assert(check_numa_node_number == nn.numa_node_number);
            }
        }
    }
}

static CacheStats getCacheStats()
{
    CacheStats res {};
    res.n_physical_processors = 0;
    res.n_logical_processors = 0;
    for (size_t level = 0; level <= MAX_LEVEL; level++)
        res.levels[level] = CacheStatsForLevel { 0, 0 };

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
                //printf("Processor %d is on numa node %d\n", info.Processor, info.NumaNode.NodeNumber);
                NUMA_NODE_RELATIONSHIP nn = info.NumaNode;
                // printf("FOUND A NUMA NODE %ld\n", nn.NodeNumber);
                // DWORD node_number = info.NumaNode.NodeNumber; // This is the only member of that struct.
                GROUP_AFFINITY gm = nn.GroupMask;
                res.numa_nodes.push_back(NumaNodeInfo{nn.NodeNumber, gm.Group, ranges_from_mask(gm.Mask)});
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
                // RelationProcessorModule:
                if (info.Relationship == 7)
                {
                    break;
                }

                else
                {
                    printf("Invalid relationship %d\n", info.Relationship);
                    //assert(0); // invalid relationship
                    break;
                }
        }

        info_ptr = offset(info_ptr, info.Size);
    }

    assert(info_ptr == end);

    check_numa_nodes_are_correct(res.numa_nodes);

    free(infos.first_info); // frees the whole buffer
    return res;
}

int main(void) {
    CacheStats stats = getCacheStats();
    printf("numa_nodes:\n", stats.numa_nodes);
    for (const NumaNodeInfo& nn : stats.numa_nodes) {
        printf("  -\n");
        printf("    numa_node_number: %zd\n", nn.numa_node_number);
        printf("    cpu_group_number: %zd\n", nn.cpu_group_number);
        printf("    ranges:\n");
        for (const Range& range : nn.ranges) {
           printf("      - { lo: %zd, hi: %zd }\n", range.lo, range.hi);
        }
    }
    printf("n_physical_processors: %zd\n", stats.n_physical_processors);
    printf("n_logical_processors: %zd\n", stats.n_logical_processors);
    printf("caches:\n");
    for (size_t level = MIN_LEVEL; level <= MAX_LEVEL; level++) {
        CacheStatsForLevel lStats = stats.levels[level];
        printf("  l%zd: { n_caches: %zd, total_bytes: %zd }\n", level, lStats.n_caches, lStats.total_bytes);
    }
}
