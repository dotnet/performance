/* Licensed to the .NET Foundation under one or more agreements.
   The .NET Foundation licenses this file to you under the MIT license.
   See the LICENSE file in the project root for more information. */

#include <windows.h>
#include <stdio.h>

int main(void) {
    HANDLE process = GetCurrentProcess();
    BOOL in_job = FALSE;
    if (!IsProcessInJob(process, NULL, &in_job)) {
        perror("IsProcessInJob failed\n");
        return 1;
    } else {
        puts(in_job ? "true" : "false");
        return 0;
    }
}
