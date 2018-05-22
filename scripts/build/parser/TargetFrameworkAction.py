'''
Contains the definition of the TargetFrameworkAction type used to parse
the .NET Cli the supported target frameworks.
'''

import argparse


class TargetFrameworkAction(argparse.Action):
    '''
    Used by the ArgumentParser to represent the information needed to parse the
    supported .NET Core target frameworks argument from the command line.
    '''

    def __call__(self, parser, namespace, values, option_string=None):
        if values:
            wrong_choices = []
            for value in values:
                if value not in self.supported_target_frameworks():
                    wrong_choices.append(value)
            if wrong_choices:
                message = ', '.join(wrong_choices)
                message = 'Invalid choice(s): {}'.format(message)
                raise argparse.ArgumentError(self, message)
            setattr(namespace, self.dest, values)

    @staticmethod
    def supported_target_frameworks() -> list:
        '''List of supported .NET Core target frameworks.'''
        return ['netcoreapp1.1', 'netcoreapp2.0', 'netcoreapp2.1', 'net461']
