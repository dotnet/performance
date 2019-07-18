#!/usr/bin/env python3

from traceback import format_exc
from glob import glob
import urllib.request
import os.path
import json
import hashlib

from logging import getLogger
from performance.common import rename_upload_files

# 64mb max upload size (due to Azure limits on a single PUT)
MAX_UPLOAD_BYTES = 1024 * 1024 * 64

def load_submission_file(path):
    if os.path.getsize(path) > MAX_UPLOAD_BYTES:
        raise ValueError("File {} exceeds maximum upload size {}".format(path, MAX_UPLOAD_BYTES))
    with open(path, "rb") as f:
        return f.read()


def build_upload_url(jsonName, storageAccountUri, container, sasToken):
    if not storageAccountUri.endswith("/"):
        storageAccountUri += "/"

    if not sasToken.startswith("?"):
        sasToken = "?" + sasToken

    return "https://{}{}/{}{}".format(storageAccountUri, container, jsonName, sasToken)


def upload_data(url, data):
    headers = { "x-ms-blob-type": "BlockBlob", "Content-Type": "application/json" }
    req = urllib.request.Request(url=url, data=data, method='PUT', headers=headers)
    with urllib.request.urlopen(req) as f:
        pass
    if not (f.status == 200 or f.status == 201):
        raise ConnectionError("Upload to url {} failed with status {} and reason {}".format(url, f.status, f.reason))

def upload(globpath, container, sas_token_env, storage_account_uri):
    try:
        sas_token_env = sas_token_env
        sas_token = os.environ.get(sas_token_env)
        if sas_token is None:
            getLogger().error("Sas token environment variable {} was not defined.".format(sas_token_env))
            return 1

        files = glob(globpath, recursive=True)
        rename_upload_files(files, os.getenv('HELIX_WORKITEM_ID'))

        renamed_files = glob(globpath, recursive=True)

        for infile in files:
            getLogger().info("uploading {}".format(infile))

            data = load_submission_file(infile)
            hash = hashlib.sha1(data).hexdigest()
            url = build_upload_url(infile, storage_account_uri, container, sas_token)

            upload_data(url, data)

            getLogger().info("upload complete")

    except Exception as ex:
        getLogger().error('{0}: {1}'.format(type(ex), str(ex)))
        getLogger().error(format_exc())
        return 1

if __name__ == "__main__":
    exit(main())
