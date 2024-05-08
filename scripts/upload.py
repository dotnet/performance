from random import randint
import uuid
from azure.storage.blob import BlobClient, ContentSettings
from azure.storage.queue import QueueClient, TextBase64EncodePolicy
from azure.core.exceptions import ResourceExistsError
from traceback import format_exc
from glob import glob
from performance.common import retry_on_exception
import os

from logging import getLogger

def get_unique_name(filename: str, unique_id: str) -> str:
    newname = "{0}-{1}".format(unique_id, os.path.basename(filename))
    if len(newname) > 1024:
        newname = "{0}-perf-lab-report.json".format(randint(1000, 9999))
    return newname

def upload(globpath: str, container: str, queue: str, sas_token_env: str, storage_account_uri: str):
    try:
        sas_token = os.getenv(sas_token_env)
        if sas_token is None:
            getLogger().error("Sas token environment variable {} was not defined.".format(sas_token_env))
            return 1

        files = glob(globpath, recursive=True)
        any_upload_or_queue_failed = False
        for infile in files:
            blob_name = get_unique_name(infile, os.getenv('HELIX_WORKITEM_ID') or str(uuid.uuid4()))

            getLogger().info("uploading {}".format(infile))

            blob_client = BlobClient(account_url=storage_account_uri.format('blob'), container_name=container, blob_name=blob_name, credential=sas_token)
            
            upload_succeded = False
            with open(infile, "rb") as data:
                try:
                    retry_on_exception(lambda: blob_client.upload_blob(data, blob_type="BlockBlob", content_settings=ContentSettings(content_type="application/json")), raise_exceptions=[ResourceExistsError])
                    upload_succeded = True
                except Exception as ex:
                    any_upload_or_queue_failed = True
                    getLogger().error("upload failed")
                    getLogger().error('{0}: {1}'.format(type(ex), str(ex)))

            if upload_succeded:
                if queue is not None:
                    try:
                        queue_client = QueueClient(account_url=storage_account_uri.format('queue'), queue_name=queue, credential=sas_token, message_encode_policy=TextBase64EncodePolicy())
                        retry_on_exception(lambda: queue_client.send_message(blob_name))
                        getLogger().info("upload and queue complete")
                    except Exception as ex:
                        any_upload_or_queue_failed = True
                        getLogger().error("queue failed")
                        getLogger().error('{0}: {1}'.format(type(ex), str(ex)))
                else:
                    getLogger().info("upload complete")

        return any_upload_or_queue_failed # 0 (False) if all uploads and queues succeeded, 1 (True) otherwise

    except Exception as ex:
        getLogger().error('{0}: {1}'.format(type(ex), str(ex)))
        getLogger().error(format_exc())
        return 1