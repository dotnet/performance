'''
Module for parsing Crossgen args
'''

import sys
import os

from logging import getLogger
from argparse import ArgumentParser
from typing import Any, List, Optional
from shared import const

class CrossgenArguments:
    '''
    Common helper for parsing crossgen args
    This is intended for runtime tests where we must execute crossgen directly.
    AOT code for SDK and higher level tests is generated during publish.
    '''

    def __init__(self):
        self.coreroot: str = None
        self.singlefile: Optional[str] = None
        self.compositefile: Optional[str] = None
        self.singlethreaded: Optional[bool] = None

    def add_crossgen_arguments(self, parser: ArgumentParser):
        "Arguments to generate AOT code with Crossgen"

        parser.add_argument('--test-name', 
                                    dest='single', 
                                    type=str, 
                                    required=False,
                                    help=
'''
[Deprecated - use --single] input assembly under Core_Root to compile
ex: System.Private.Xml.dll
'''
                                    )
        parser.add_argument('--single', 
                                    dest='single', 
                                    type=str, 
                                    required=False,
                                    help=
'''
input assembly under Core_Root to compile
ex: System.Private.Xml.dll
'''
                                    )

        parser.add_argument('--core-root', 
                                    dest='coreroot', 
                                    type=str, 
                                    required=True,
                                    help=
r'''
path of Core_Root generated from runtime build
ex: C:\repos\runtime\artifacts\tests\coreclr\Windows_NT.x64.Release\Tests\Core_Root
'''                                 )

    def add_crossgen2_arguments(self, parser: ArgumentParser):
        "Arguments to generate AOT code with Crossgen2"
        
        parser.add_argument('--core-root', 
                            dest='coreroot', 
                            type=str, 
                            required=True,
                            help=
r'''
path of Core_Root generated from runtime build
ex: C:\repos\runtime\artifacts\tests\coreclr\Windows_NT.x64.Release\Tests\Core_Root
'''
                            )
        parser.add_argument('--single', 
                            dest='single', 
                            type=str, 
                            required=False,
                            help=
r'''
a single input assembly under Core_Root to compile
ex: System.Private.Xml.dll
'''                                  
                            )
        parser.add_argument('--composite', 
                            dest='composite', 
                            type=str, 
                            required=False,
                            help=
r'''
path to an rsp file that represents a collection of assemblies
ex: C:\repos\performance\src\scenarios\crossgen2\framework-r2r.dll.rsp
'''
                            )
        parser.add_argument('--singlethreaded', 
                            dest='singlethreaded', 
                            required=False,
                            help=
r'''
Suppress internal Crossgen2 parallelism
'''
                            )

    def parse_crossgen_args(self, args: Any):
        self.singlefile = args.single
        self.coreroot = args.coreroot

        if self.coreroot and not os.path.isdir(self.coreroot):
            getLogger().error('Cannot find CORE_ROOT at %s', self.coreroot)
            sys.exit(1)
        if self.singlefile is None:
            getLogger().error('Specify an assembly to crossgen with --single <assembly name>')
            sys.exit(1)
    
    def parse_crossgen2_args(self, args: Any):
        self.coreroot = args.coreroot
        self.singlefile = args.single
        self.compositefile = args.composite
        self.singlethreaded = args.singlethreaded

        if self.coreroot and not os.path.isdir(self.coreroot):
            getLogger().error('Cannot find CORE_ROOT at %s', self.coreroot)
            sys.exit(1)
        if bool(self.singlefile) == bool(self.compositefile):
            getLogger().error("Please specify either --single <single assembly name> or --composite <absolute path of rsp file>")
            sys.exit(1)

    def get_crossgen_command_line(self) -> List[str]:
        "Returns the computed crossgen command line arguments"
        filename, ext = os.path.splitext(self.singlefile)
        outputdir = os.path.join(os.getcwd(), const.CROSSGENDIR)
        if not os.path.exists(outputdir):
            os.mkdir(outputdir)
        outputfile = os.path.join(outputdir, filename+'.ni'+ext )

        crossgenargs = [
            '/nologo',
            '/out', outputfile,
            '/p', self.coreroot,
            os.path.join(self.coreroot, self.singlefile) 
        ]
        
        return crossgenargs
        
    def get_crossgen2_command_line(self):
        "Returns the computed crossgen2 command line arguments"
        compiletype = self.crossgen2_compiletype()

        if compiletype == const.CROSSGEN2_SINGLEFILE:
            referencefilenames = ['System.*.dll', 'Microsoft.*.dll', 'netstandard.dll', 'mscorlib.dll']
            # single assembly filename: example.dll
            filename, ext = os.path.splitext(self.singlefile)
            outputdir = os.path.join(os.getcwd(), const.CROSSGENDIR)
            if not os.path.exists(outputdir):
                os.mkdir(outputdir)
            outputfile = os.path.join(outputdir, filename+'.ni'+ext )
            
            crossgen2args = [
                os.path.join(self.coreroot, self.singlefile),
                '-o', outputfile,
                '-O'
            ]

            for reffile in referencefilenames:
                crossgen2args.extend(['-r', os.path.join(self.coreroot, reffile)])
        
        elif compiletype == const.CROSSGEN2_COMPOSITE:
            # composite rsp filename: ..\example.dll.rsp
            dllname, _ = os.path.splitext(os.path.basename(self.compositefile))
            filename, ext = os.path.splitext(dllname)
            outputdir = os.path.join(os.getcwd(), const.CROSSGENDIR)
            if not os.path.exists(outputdir):
                os.mkdir(outputdir)
            outputfile = os.path.join(outputdir, filename+'.ni'+ext )

            crossgen2args = [
                '--composite',
                '-o', outputfile,
                '-O',
                '@%s' % (self.compositefile)
            ]
            
        if self.singlethreaded:
            crossgen2args += ['--parallelism', '1']

        return crossgen2args

    def crossgen2_compiletype(self):
        return const.CROSSGEN2_COMPOSITE if self.compositefile else const.CROSSGEN2_SINGLEFILE

    def crossgen2_scenario_filename(self) -> str:
        "Returns the name of the assembly being compiled or composite image generated, without file extension"
        compiletype = self.crossgen2_compiletype()

        if compiletype == const.CROSSGEN2_SINGLEFILE:
            filename, _ = os.path.splitext(self.singlefile)
        elif compiletype == const.CROSSGEN2_COMPOSITE:
            dllname, _ = os.path.splitext(os.path.basename(self.compositefile))
            filename, _ = os.path.splitext(dllname)
        return filename
