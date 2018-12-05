// Import the utility functionality.

import jobs.generation.*;

def project = GithubProject
def branch = GithubBranchName
def projectName = Utilities.getFolderName(project)
def projectFolder = projectName + '/' + Utilities.getFolderName(branch)

['Windows', 'Linux'].each { os ->
    ['perf','throughput','size','e2e'].each { jobType ->
        if (!(os == 'Linux' && (jobType == 'size' || jobType == 'e2e'))) {
            def jobName = "perf_monitoring_coreclr_${os}_${jobType}"

            def newJob = job(Utilities.getFullJobName(project, jobName, false)) {
                steps {
                    batchFile("py scripts\\getjenkinsstatus.py -repo coreclr -os ${os} -jobType ${jobType}")
                }
            }

            Utilities.setMachineAffinity(newJob, "Windows_NT", '20170427-elevated')
            Utilities.standardJobSetup(newJob, project, false, "*/${branch}")

            Utilities.addPeriodicTrigger(newJob, "@hourly", true /*always run*/)
            newJob.with {
                wrappers {
                    timeout {
                        absolute(240)
                    }
                }
            }
        }
    }

    def jobName = "performance_monitoring_corefx_${os}"
    def newJob = job(Utilities.getFullJobName(project, jobName, false)) {
        steps {
            batchFile("py scripts\\getjenkinsstatus.py -repo corefx -os ${os}")
        }
    }

    Utilities.setMachineAffinity(newJob, "Windows_NT", '20170427-elevated')
    Utilities.standardJobSetup(newJob, project, false, "*/${branch}")

    Utilities.addPeriodicTrigger(newJob, "@hourly", true /*always run*/)
    newJob.with {
        wrappers {
            timeout {
                absolute(240)
            }
        }
    }

    if (os == 'Windows') {
        jobName = "container_benchmarks_static_${os}_amd64"
        newJob = job(Utilities.getFullJobName(project, jobName, false)) {
            wrappers {
                credentialsBinding {
                    string('BV_UPLOAD_SAS_TOKEN', 'Container_Perf_BenchView_Sas')
                }
            }

            steps {
                batchFile("py -3 scripts\\container_benchmarks_ci.py")
            }

            label("windows_container_perf")
        }

        Utilities.standardJobSetup(newJob, project, false, "*/${branch}")

        Utilities.addPeriodicTrigger(newJob, "@daily", true /*always run*/)
        newJob.with {
            wrappers {
                timeout {
                    absolute(240)
                }
            }
        }

        jobName = "dmlib_${os}_amd64"
        newJob = job(Utilities.getFullJobName(project, jobName, false)) {
            wrappers {
                credentialsBinding {
                    string('BV_UPLOAD_SAS_TOKEN', 'Container_Perf_BenchView_Sas')
                    string('BENCHMARK_SAS_TOKEN', 'Dmlib_Benchmark_Sas')
                    string('BENCHMARK_ACCOUNT', 'Dmlib_Benchmark_Account')
                }
            }

            steps {
                batchFile("py -3 scripts\\dmlib_benchmark_ci.py")
            }

            label("windows_container_perf")
        }

        Utilities.standardJobSetup(newJob, project, false, "*/${branch}")

        Utilities.addPeriodicTrigger(newJob, "@daily", true /*always run*/)
        newJob.with {
            wrappers {
                timeout {
                    absolute(240)
                }
            }
        }

        // CoreCLR perf jobs
        [true, false].each { isPR ->
            ['x64', 'x86'].each { arch ->
                jobName = "coreclr_perf_${os}_${arch}"
                newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {
                    wrappers {
                        credentialsBinding {
                            string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
                        }
                    }

                    def runType = isPR ? "private" : "rolling"
                    def benchviewCommitNamePrefix = ".NET Performance: CoreClr ${runType}:"
                    def benchviewCommitName = isPR ? "${benchviewCommitNamePrefix} ${ghprbPullTitle}" : "${benchviewCommitNamePrefix} %GIT_BRANCH% %GIT_COMMIT%"
                    parameters {
                        stringParam('BenchviewCommitName', benchviewCommitName, 'The name that you will be used to build the full title of a run in Benchview.')
                    }

                    def python = "C:\\Python35\\python.exe"

                    steps {
                        batchFile("${python} .\\scripts\\benchmarks_ci.py --incremental no --architecture ${arch} --category CoreClr -f netcoreapp3.0 --generate-benchview-data --upload-to-benchview-container coreclr --benchview-run-type ${runType}")
                    }

                    label("windows_server_2016_clr_perf")
                }

                Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

                if (isPR) {
                    TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
                    builder.setGithubContext("CoreCLR Windows ${arch} Perf Test")
                    builder.triggerOnlyOnComment()
                    builder.setCustomTriggerPhrase("(?i).*test\\W+coreclr\\W+${arch}\\W+perf.*")
                    builder.triggerForBranch(branch)
                    builder.emitTrigger(newJob)
                }
                else {
                    Utilities.addPeriodicTrigger(newJob, "@daily", true /*always run*/)
                    newJob.with {
                        wrappers {
                            timeout {
                                absolute(240)
                            }
                        }
                    }
                }
            }
        }
    }
}