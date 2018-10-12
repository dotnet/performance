#!/usr/bin/env python3

from os import path
from urllib.request import urlopen
from xml.etree import ElementTree

from build.common import download_zip_file
from build.common import get_benchview_scripts_directory
from build.common import make_directory
from build.common import remove_directory
from build.common import unzip_file


def install():
    '''
    Downloads scripts that serialize/upload performance data to BenchView.
    '''
    url_str = __get_latest_benchview_script_version()
    benchview_path = get_benchview_scripts_directory()
    if path.isdir(benchview_path):
        remove_directory(benchview_path)
    if not path.exists(benchview_path):
        make_directory(benchview_path)
    print('{} -> {}'.format(url_str, benchview_path))
    zipfile_path = download_zip_file(url_str, benchview_path)
    unzip_file(zipfile_path, benchview_path)


def __get_latest_benchview_script_version() -> str:
    scheme_authority = 'http://benchviewtestfeed.azurewebsites.net'
    fullpath = "/nuget/FindPackagesById()?id='Microsoft.BenchView.JSONFormat'"
    url_str = '{}{}'.format(scheme_authority, fullpath)
    with urlopen(url_str) as response:
        tree = ElementTree.parse(response)
        root = tree.getroot()
        namespace = root.tag[0:root.tag.index('}') + 1]
        xpath = '{0}entry/{0}content[@type="application/zip"]'.format(
            namespace)
        packages = [element.get('src') for element in tree.findall(xpath)]
        if not packages:
            raise RuntimeError('No BenchView packages found.')
        packages.sort()
        return packages[-1]


def __main():
    install()
    # TODO: Add to PATH?


if __name__ == "__main__":
    __main()
