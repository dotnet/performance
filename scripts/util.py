from os import path, stat, listdir, name as os_name, makedirs
import subprocess

_script_dir = path.dirname(path.realpath(__file__))
_tools_dir = path.normpath(path.join(_script_dir, '..', 'tools'))
_reports_dir = path.normpath(path.join(_script_dir, '..', 'reports'))
_bvtools_dir = None
_py_prefix = ['py', '-3'] if os_name == 'nt' else []

def cmd(cmdargs, handler=None, **kwargs):
    try:
        return subprocess.run(
            cmdargs,
            stderr=subprocess.PIPE,
            stdout=subprocess.PIPE,
            check=True,
            **kwargs
        )
    except subprocess.CalledProcessError as e:
        if handler is None or not handler(e):
            print(e.output.decode('utf-8'))
            raise
        else:
            return None

def aquire_bvtools():
    global _bvtools_dir
    
    cmd([
        'nuget', 'install',
        'Microsoft.BenchView.JSONFormat',
        '-Source', 'http://benchviewtestfeed.azurewebsites.net/nuget',
        '-OutputDirectory', _tools_dir,
        '-Prerelease'
    ])

    # Find the correct folder containing the latest package
    bvpkg_dir = None
    bvpkg_mtime = None
    for name in listdir(_tools_dir):
        if name.startswith('Microsoft.BenchView.JSONFormat'):
            directory = path.join(_tools_dir, name)
            mtime = stat(directory).st_mtime
            if bvpkg_mtime is None or mtime > bvpkg_mtime:
                bvpkg_dir = directory
                bvpkg_mtime = mtime

    if bvpkg_dir is None:
        raise ValueError("could not find the downloaded Microsoft.BenchView.JSONFormat package")

    _bvtools_dir = path.join(bvpkg_dir, 'tools')
    
def generate_metadata(name, user_email, outfile=None):
    if _bvtools_dir is None:
        aquire_bvtools()
        
    if outfile is None:
        if not path.isdir(_reports_dir):
            makedirs(_reports_dir)

        outfile = path.join(_reports_dir, "submission-metadata.json")
        
    cmd(_py_prefix + [
        path.join(_bvtools_dir, 'submission-metadata.py'),
        '--name', name,
        '--user-email', user_email,
        '--outfile', outfile
    ])

def generate_machinedata(outfile=None):
    if _bvtools_dir is None:
        aquire_bvtools()

    if outfile is None:
        if not path.isdir(_reports_dir):
            makedirs(_reports_dir)
            
        outfile = path.join(_reports_dir, "machinedata.json")
        
    cmd(_py_prefix + [
        path.join(_bvtools_dir, 'machinedata.py'),
        '--outfile', outfile
    ])

def generate_build(branch, number, timestamp, type, repository, outfile=None):
    if _bvtools_dir is None:
        aquire_bvtools()

    if outfile is None:
        if not path.isdir(_reports_dir):
            makedirs(_reports_dir)
            
        outfile = path.join(_reports_dir, "build.json")
        
    cmd(_py_prefix + [
        path.join(_bvtools_dir, 'build.py'),
        '--branch', branch,
        '--number', number,
        '--source-timestamp', timestamp,
        '--type', type,
        '--repository', repository,
        '--outfile', outfile
    ])
    
def generate_measurement_csv(datafile, metric, unit, ascending, outfile=None):
    if _bvtools_dir is None:
        aquire_bvtools()

    if outfile is None:
        if not path.isdir(_reports_dir):
            makedirs(_reports_dir)
            
        outfile = path.join(_reports_dir, "measurement.json")
        
    cmd(_py_prefix + [
        path.join(_bvtools_dir, 'measurement.py'), 'csv',
        datafile,
        '--metric', metric,
        '--unit', unit,
        '--better', 'asc' if ascending else 'desc',
        '--outfile', outfile
    ])
    
def generate_submission(group, type, config_name, config, arch, machinepool, datafile=None, build=None, machine=None, metadata=None, outfile=None):
    if _bvtools_dir is None:
        aquire_bvtools()

    if datafile is None:
        datafile = path.join(_reports_dir, "measurement.json")
    if build is None:
        build = path.join(_reports_dir, "build.json")
    if machine is None:
        machine = path.join(_reports_dir, "machinedata.json")
    if metadata is None:
        metadata = path.join(_reports_dir, "submission-metadata.json")
    if outfile is None:
        if not path.isdir(_reports_dir):
            makedirs(_reports_dir)
            
        outfile = path.join(_reports_dir, "submission.json")
        
    config_opts = [arg for k, v in config.items() for arg in ('--config', k, v)]
    
    cmd(_py_prefix + [
        path.join(_bvtools_dir, 'submission.py'),
        datafile,
        '--group', group,
        '--type', type,
        '--config-name', config_name,
        '--architecture', arch,
        '--machinepool', machinepool,
        '--build', build,
        '--machine-data', machine,
        '--metadata', metadata,
        '--outfile', outfile
    ] + config_opts)
    
def upload(container, sas_token_env=None, account=None, *submissions):
    if _bvtools_dir is None:
        aquire_bvtools()
        
    sas_opt = ['--sas-token-env', sas_token_env] if sas_token_env is not None else []
    account_opt = ['--storage-account-uri', account] if account is not None else []
    
    cmd(_py_prefix + [
        path.join(_bvtools_dir, 'upload.py'),
    ] + submissions + sas_opt + account_opt + [
        '--container', container
    ])
