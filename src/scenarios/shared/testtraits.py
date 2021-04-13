from shared.util import const

# These are the kinds of scenarios we run. Default here indicates whether ALL
# scenarios should try and run a given test type.
testtypes = [const.STARTUP,
             const.SDK,
             const.CROSSGEN,
             const.CROSSGEN2,
             const.SOD,
             const.INNERLOOP]

class TestTraits:

    def __init__(self, **kwargs):
        # initialize traits
        self.exename = ''
        self.scenarioname = ''
        self.scenariotypename = ''
        self.apptorun = ''
        self.guiapp = ''
        self.startupmetric = ''
        self.appargs = ''
        self.iterations = ''
        self.timeout = ''
        self.warmup = ''
        self.workingdir = ''
        self.iterationsetup = ''
        self.setupargs = ''
        self.iterationcleanup = ''
        self.cleanupargs = ''
        self.processwillexit = ''
        self.measurementdelay = ''
        self.environmentvariables = ''
        self.skipprofile = ''
        self.artifact = ''
        self.innerloopcommand = ''
        self.innerloopcommandargs = ''
        self.projext = ''
        self.runwithoutexit = ''
        self.hotreloaditers = ''
        self.skipmeasurementiteration = ''

        # add test types to traits
        for testtype in testtypes:
            setattr(self, testtype, '')
        
        # add user-input initial traits
        self.add_traits(overwrite=True, **kwargs)

        # validate required traits
        if not self.exename:
            raise Exception("exename cannot be empty")

    # add traits if not present or overwrite existing traits if overwrite=True
    def add_traits(self, overwrite=True, **kwargs):
        for keyword in kwargs:
            if not self.is_valid_trait(keyword):
                raise Exception("%s is not a valid trait." % keyword)
            if not getattr(self, keyword) or overwrite:
                setattr(self, keyword, kwargs[keyword])

    def is_valid_trait(self, key: str):
        try:
            getattr(self, key)
        except AttributeError:
            return False
        return True
    
    def get_all_traits(self):
        return vars(self)

