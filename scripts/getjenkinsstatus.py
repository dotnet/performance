import urllib.request
import sys
import subprocess
import argparse

##########################################################################
# Argument Parser
##########################################################################

description = 'Tool to get job status'

parser = argparse.ArgumentParser(description=description)

parser.add_argument('-os', dest='operatingSystem', default='Windows', choices=['Windows', 'Linux'])
parser.add_argument('-jobType', dest='jobType', default='all', choices=['all','perf','size','rwc','throughput'])
parser.add_argument('-arch', dest='arch', default='all', choices=['all','x86','x64'])
parser.add_argument('-repo', dest='repo', default='', choices=['coreclr','corefx'])

def parseStatusPage(url, job):
    source = 'test.txt'
    p = subprocess.Popen("powershell [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; [io.file]::WriteAllText(\\\"{}\\\", (Invoke-WebRequest -Uri {}).content)".format(source, url), shell=True)
    p.communicate()

    failing = False
    passing = False
    running = False
    aborted = False

    status = ""

    f = open(source, "r")

    for line in f.readlines():
        if "failing" in line:
            status = "failing"
            failing = True
        if "passing" in line:
            status = "passing"
            passing = True
        if "running" in line:
            status = "running"
            running = True
        if "aborted" in line:
            status = "aborted"
            aborted = True

    print("{}: {}".format(job, status))
    return failing or aborted

def main(args):
    operatingSystem = args.operatingSystem
    jobType = args.jobType
    arch = args.arch
    repo = args.repo

    if operatingSystem == 'Linux':
        if not (jobType == 'all' or jobType == 'perf' or jobType == 'throughput'):
            raise ValueError('Linux only has perf and throughput jobs. JobType %s is invalid.', jobType)
        if not (arch == 'all' or arch == 'x64'):
            raise ValueError('Linux only supports x64. Arch %s is invalid.', arch)

    urlPrefix = "https://ci2.dot.net/job/"
    urlJobPath = "/job/perf/job/master/job/"
    urlSuffix = "/lastCompletedBuild/badge/icon"

    coreclrJobs = {
            'Windows' : {
                'size' : {
                    'x64' : [
                        'perf_illink_Windows_NT_x64_full_opt_ryujit',
                        'sizeondisk_x64'
                    ],
                    'x86' : [
                        'sizeondisk_x86'
                    ]
                },
                'perf' : {
                    'x64' : [
                        'perf_perflab_Windows_NT_x64_full_opt_ryujit',
                        'perf_perflab_Windows_NT_x64_min_opt_ryujit'
                    ],
                    'x86' : [
                        'perf_perflab_Windows_NT_x86_full_opt_ryujit',
                        'perf_perflab_Windows_NT_x86_min_opt_ryujit'
                    ]
                },
                'scenarios' : {
                    'x64' : [
                        'perf_illink_Windows_NT_x64_full_opt_ryujit'
                        'perf_scenarios_Windows_NT_x64_full_opt_ryujit',
                        'perf_scenarios_Windows_NT_x64_min_opt_ryujit',
                        'perf_scenarios_Windows_NT_x64_tiered_ryujit'
                    ],
                    'x86' : [
                        'perf_scenarios_Windows_NT_x86_full_opt_ryujit',
                        'perf_scenarios_Windows_NT_x86_min_opt_ryujit',
                        'perf_scenarios_Windows_NT_x86_tiered_ryujit'
                    ]
                },
                'throughput' : {
                    'x64' : [
                        'perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo',
                        'perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo',
                        'perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_nopgo',
                        'perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_pgo'
                    ],
                    'x86' : [
                        'perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo',
                        'perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo',
                        'perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_nopgo',
                        'perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_pgo'
                    ]
                }
            },
            'Linux' : {
                'perf' : {
                    'x64': [
                        'perf_linux_flow'
                    ]
                },
                'throughput' : {
                    'x64': [
                        'perf_throughput_linux_flow'
                    ]
                }
            }
        }

    corefxJobs = {
            'Windows' : [ 'perf_windows_nt_release'],
            'Linux' : ['perf_ubuntu16.04_release']
        }

    overallStatus = "passing"

    if repo == 'coreclr' or repo == '':
        urlRepo = 'dotnet_coreclr'
        for jobChoice in coreclrJobs[operatingSystem]:
            if jobChoice == jobType or jobType == 'all':
                for archChoice in coreclrJobs[operatingSystem][jobChoice]:
                    if archChoice == arch or arch == 'all':
                        for job in coreclrJobs[operatingSystem][jobChoice][archChoice]:
                            url = urlPrefix + urlRepo + urlJobPath + job + urlSuffix
                            
                            if parseStatusPage(url, job):
                                overallStatus = 'failing'
    if repo == 'corefx' or repo == '':
        urlRepo = 'dotnet_corefx'
        for job in corefxJobs[operatingSystem]:
            url = urlPrefix + urlRepo + urlJobPath + job + urlSuffix
            
            if parseStatusPage(url, job):
                overallStatus = 'failing'


    print('%s %s %s %s jobs are %s' % (repo, operatingSystem, arch, jobType, overallStatus))
    if overallStatus == 'failing':
        return -1
    return 0

if __name__ == "__main__":
    Args = parser.parse_args(sys.argv[1:])
    sys.exit(main(Args))
