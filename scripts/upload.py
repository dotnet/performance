from azure.storage.blob import BlobClient, ContentSettings
from traceback import format_exc
from glob import glob
import os

from logging import getLogger

def get_unique_name(filename, unique_id) -> str:
    newname = "{0}-{1}".format(unique_id,
                                os.path.basename(filename))
    if len(newname) > 1024:
        newname = "{0}-perf-lab-report.json".format(randint(1000, 9999))
    return newname

def upload(globpath, container, sas_token_env, storage_account_uri):
    try:
        sas_token_env = sas_token_env
        sas_token = os.getenv(sas_token_env)
        if sas_token is None:
            getLogger().error("Sas token environment variable {} was not defined.".format(sas_token_env))
            return 1

        files = glob(globpath, recursive=True)

        for infile in files:
            blob_name = get_unique_name(infile, os.getenv('HELIX_WORKITEM_ID'))

            getLogger().info("uploading {}".format(infile))

            blob_client = BlobClient(account_url=storage_account_uri, container_name=container, blob_name=blob_name, credential=sas_token)
            
            with open(infile, "rb") as data:
                blob_client.upload_blob(data, blob_type="BlockBlob", content_settings=ContentSettings(content_type="application/json"))

            getLogger().info("upload complete")

    except Exception as ex:
        getLogger().error('{0}: {1}'.format(type(ex), str(ex)))
        getLogger().error(format_exc())
        return 1