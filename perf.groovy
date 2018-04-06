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

            def newJob = job(InternalUtilities.getFullJobName(project, jobName, false)) {
                steps {
                    batchFile("py scripts\\getjenkinsstatus.py -repo coreclr -os ${os} -jobType ${jobType}")
                }
            }

            Utilities.setMachineAffinity(newJob, "Windows_NT", '20170427-elevated')
            InternalUtilities.standardJobSetup(newJob, project, false, "*/${branch}")

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
    def newJob = job(InternalUtilities.getFullJobName(project, jobName, false)) {
        steps {
            batchFile("py scripts\\getjenkinsstatus.py -repo corefx -os ${os}")
        }
    }

    Utilities.setMachineAffinity(newJob, "Windows_NT", '20170427-elevated')
    InternalUtilities.standardJobSetup(newJob, project, false, "*/${branch}")

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
        newJob = job(InternalUtilities.getFullJobName(project, jobName, false)) {
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

        InternalUtilities.standardJobSetup(newJob, project, false, "*/${branch}")

        Utilities.addPeriodicTrigger(newJob, "@daily", true /*always run*/)
        newJob.with {
            wrappers {
                timeout {
                    absolute(240)
                }
            }
        }
    }
        
    jobName = "dmlib_${os}_amd64"
    newJob = job(InternalUtilities.getFullJobName(project, jobName, false)) {
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

    InternalUtilities.standardJobSetup(newJob, project, false, "*/${branch}")

    Utilities.addPeriodicTrigger(newJob, "@daily", true /*always run*/)
    newJob.with {
        wrappers {
            timeout {
                absolute(240)
            }
        }
    }
}