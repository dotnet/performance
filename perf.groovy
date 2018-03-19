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
}

