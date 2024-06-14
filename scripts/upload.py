from azure.storage.blob import BlobClient, ContentSettings
from azure.storage.queue import QueueClient, TextBase64EncodePolicy
from azure.core.exceptions import ResourceExistsError, ClientAuthenticationError
from azure.identity import DefaultAzureCredential, ClientAssertionCredential
from traceback import format_exc
from glob import glob
from performance.constants import TENANT_ID, CLIENT_ID
import os

from logging import getLogger

def get_unique_name(filename, unique_id) -> str:
    newname = "{0}-{1}".format(unique_id,
                                os.path.basename(filename))
    if len(newname) > 1024:
        newname = "{0}-perf-lab-report.json".format(randint(1000, 9999))
    return newname

def upload(globpath, container, queue, sas_token_env, storage_account_uri):
    try:
        credential = None
        try:
            dac = DefaultAzureCredential()
            credential = ClientAssertionCredential(TENANT_ID, CLIENT_ID, lambda: dac.get_token("api://AzureADTokenExchange/.default").token)
            credential.get_token("https://storage.azure.com/.default")
        except ClientAuthenticationError as ex:
            getLogger().info("Unable to use managed identity. Falling back to environment variable.")
            credential = os.getenv(sas_token_env)
        if credential is None:
            getLogger().error("Sas token environment variable {} was not defined.".format(sas_token_env))
            return 1

        files = glob(globpath, recursive=True)

        for infile in files:
            blob_name = get_unique_name(infile, os.getenv('HELIX_WORKITEM_ID'))

            getLogger().info("uploading {}".format(infile))

            blob_client = BlobClient(account_url=storage_account_uri.format('blob'), container_name=container, blob_name=blob_name, credential=credential)
            
            with open(infile, "rb") as data:
                blob_client.upload_blob(data, blob_type="BlockBlob", content_settings=ContentSettings(content_type="application/json"))

            if queue is not None:
                queue_client = QueueClient(account_url=storage_account_uri.format('queue'), queue_name=queue, credential=credential, message_encode_policy=TextBase64EncodePolicy())
                queue_client.send_message(blob_client.url)

            getLogger().info("upload complete")
        return 0

    except Exception as ex:
        getLogger().error('{0}: {1}'.format(type(ex), str(ex)))
        getLogger().error(format_exc())
        return 1
