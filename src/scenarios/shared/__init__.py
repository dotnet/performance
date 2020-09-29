from os import environ
import sys
import os

# We want scripts to run trivially outside the lab.
payload = environ.get('HELIX_CORRELATION_PAYLOAD')

if payload:
    sys.path.append(os.path.join(payload, 'scripts'))
else:
    sys.path.append(os.path.join('..', '..', '..', 'scripts'))
