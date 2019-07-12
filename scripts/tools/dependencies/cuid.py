"""
A `cuid` is a secure, portable, and sequentially-ordered identifier
designed for horizontal scalability and speed -- the reference implementation
is in Javascript at https://github.com/ericelliott/cuid
"""
import os
import time
import socket
import random

# Constants describing the cuid algorithm

BASE = 36
BLOCK_SIZE = 4
DISCRETE_VALUES = BASE ** BLOCK_SIZE

# Helper functions

_alphabet = "0123456789abcdefghijklmnopqrstuvwxyz"
def _to_base36(number):
    """
    Convert a positive integer to a base36 string.

    Taken from Stack Overflow and modified.
    """
    if number < 0:
        raise ValueError("Cannot encode negative numbers")

    chars = ""
    while number != 0:
        number, i = divmod(number, 36)  # 36-character alphabet
        chars = _alphabet[i] + chars

    return chars or "0"


_padding = "000000000"
def _pad(string, size):
    """
    'Pad' a string with leading zeroes to fit the given size, truncating
    if necessary.
    """
    strlen = len(string)
    if strlen == size:
        return string
    if strlen < size:
        return _padding[0:size-strlen] + string
    return string[-size:]


def _random_block():
    """
    Generate a random string of `BLOCK_SIZE` length.
    """
    # TODO: Use a better RNG than random.randint
    random_number = random.randint(0, DISCRETE_VALUES)
    random_string = _to_base36(random_number)
    return _pad(random_string, BLOCK_SIZE)

# Exported functionality

def get_process_fingerprint():
    """
    Extract a unique fingerprint for the current process, using a
    combination of the process PID and the system's hostname.
    """
    pid = os.getpid()
    hostname = socket.gethostname()
    padded_pid = _pad(_to_base36(pid), 2)
    hostname_hash = sum([ord(x) for x in hostname]) + len(hostname) + 36
    padded_hostname = _pad(_to_base36(hostname_hash), 2)
    return padded_pid + padded_hostname


_generator = None

def cuid():
    global _generator
    if not _generator:
        _generator = CuidGenerator()
    return _generator.cuid()


def slug():
    global _generator
    if not _generator:
        _generator = CuidGenerator()
    return _generator.slug()


class CuidGenerator(object):
    """
    Generate cuids
    """

    def __init__(self, fingerprint=None):
        self.fingerprint = fingerprint or get_process_fingerprint()
        self._counter = -1

    @property
    def counter(self):
        """
        Rolling counter that ensures same-machine and same-time
        cuids don't collide.
        """
        self._counter += 1
        if self._counter >= DISCRETE_VALUES:
            self._counter = 0
        return self._counter

    def cuid(self):
        """
        Generate a full-length cuid as a string.
        """
        # start with a hardcoded lowercase c
        identifier = "c"
        # add a timestamp in milliseconds since the epoch, in base 36
        millis = int(time.time() * 1000)
        identifier += _to_base36(millis)
        # use a counter to ensure no collisions on the same machine
        # in the same millisecond
        count = _pad(_to_base36(self.counter), BLOCK_SIZE)
        identifier += count
        # add the process fingerprint
        identifier += self.fingerprint
        # add a couple of random blocks
        identifier += _random_block()
        identifier += _random_block()

        return identifier

    def slug(self):
        """
        Generate a short (7-character) cuid as a bytestring.

        While this is a convenient shorthand, this is much less likely
        to be unique and should not be relied on. Prefer full-size
        cuids where possible.
        """
        identifier = ""
        # use a truncated timestamp
        millis = int(time.time() * 1000)
        millis_string = _to_base36(millis)
        identifier += millis_string[-2:]
        # use a truncated counter
        count = _pad(_to_base36(self.counter), 1)
        identifier += count
        # use a truncated fingerprint
        identifier += self.fingerprint[0]
        identifier += self.fingerprint[-1]
        # use some truncated random data
        random_data = _random_block()
        identifier += random_data[-2:]

        return identifier
