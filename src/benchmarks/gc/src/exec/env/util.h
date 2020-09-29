/* Licensed to the .NET Foundation under one or more agreements.
   The .NET Foundation licenses this file to you under the MIT license.
   See the LICENSE file in the project root for more information. */


#define ACCEPTABLE_MEMORY_DELTA_PCT 5
#define BYTES_IN_KB 1024
#define BYTES_IN_MB (1024 * 1024)
#define BYTES_IN_GB (1024 * 1024 * 1024)
#define MEMORY_LOAD_NUM_ATTEMPTS 5


static BOOL streq(const char* a, const char* b)
{
    return strcmp(a, b) == 0;
}

static size_t mb_to_bytes(double mb)
{
    assert(mb >= 0 && mb <= 10000);
    return (size_t) (mb * 1024.0 * 1024.0);
}

static size_t kb_to_bytes(double kb)
{
    assert(kb >= 0 && kb <= 10000000);
    return (size_t) (kb * 1024.0);
}

double bytes_to_kb(const size_t bytes)
{
    return ((double) bytes) / BYTES_IN_KB;
}

double bytes_to_mb(const size_t bytes)
{
    return ((double) bytes) / BYTES_IN_MB;
}

double bytes_to_gb(const size_t bytes)
{
    return ((double) bytes) / BYTES_IN_GB;
}


static int fail(const char *format, ...)
{
    va_list args;
    va_start(args, format);
    vfprintf(stderr, format, args);
    va_end(args);
    fflush(stderr);
    return 1;
}

// Returns 0 on failure
static int parse_double(char* str, double* res, double min, double max)
{
    char* end;
    *res = strtod(str, &end);
    if (*res < min || *res > max || *end != '\0' || errno != 0)
        // NOTE: strtod destroys its input, so can't print argv[i] here
        return fail("Value is invalid: %f\n", *res);
    return 0;
}
