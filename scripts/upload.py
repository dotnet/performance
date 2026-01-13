from typing import Optional
import uuid
from azure.storage.blob import BlobClient, ContentSettings
from azure.storage.queue import QueueClient, TextBase64EncodePolicy
from azure.core.exceptions import ResourceExistsError, ClientAuthenticationError
from azure.identity import DefaultAzureCredential, ClientAssertionCredential, CertificateCredential
from traceback import format_exc
from glob import glob
from performance.common import retry_on_exception, base64_to_bytes, get_certificates
from performance.constants import TENANT_ID, ARC_CLIENT_ID, CERT_CLIENT_ID
import os
import json

from logging import getLogger

class QueueMessage:
    container_name: str
    blob_name: str

    def __init__(self, container: str, name: str):
        self.container_name = container
        self.blob_name = name

def get_unique_name(filename: str, unique_id: str) -> str:
    basename = os.path.basename(filename)
    newname = "{0}-{1}".format(unique_id, basename)
    if len(newname) > 1024:
        # Truncate the basename to fit within 1024 characters while preserving unique_id
        # Reserve space for unique_id, hyphen, and file extension
        max_basename_length = 1024 - len(unique_id) - 1  # -1 for the hyphen
        if max_basename_length > 0:
            # Try to preserve the file extension
            ext_index = basename.rfind('.')
            if ext_index > 0 and len(basename) - ext_index <= 20:  # reasonable extension length
                extension = basename[ext_index:]
                max_name_length = max_basename_length - len(extension)
                if max_name_length > 0:
                    truncated_name = basename[:max_name_length]
                    newname = "{0}-{1}{2}".format(unique_id, truncated_name, extension)
                else:
                    # Extension takes too much space, use minimal name within available space
                    # We have max_basename_length chars available after unique_id and hyphen
                    minimal_name = "f" * max_basename_length if max_basename_length > 0 else ""
                    newname = "{0}-{1}".format(unique_id, minimal_name)
            else:
                # No extension or extension is too long, just truncate basename
                truncated_basename = basename[:max_basename_length]
                newname = "{0}-{1}".format(unique_id, truncated_basename)
        else:
            # unique_id itself is very long (>1023 chars), use just unique_id truncated
            newname = unique_id[:1024]
    return newname

def get_credential():
    try:
        dac = DefaultAzureCredential()
        credential = ClientAssertionCredential(TENANT_ID, ARC_CLIENT_ID, lambda: dac.get_token("api://AzureADTokenExchange/.default").token)
        credential.get_token("https://storage.azure.com/.default")
        return credential
    except ClientAuthenticationError as ex:
        getLogger().info("Unable to use managed identity. Falling back to certificate.")
        certs = get_certificates()
        for cert in certs:
            credential = CertificateCredential(TENANT_ID, CERT_CLIENT_ID, certificate_data=base64_to_bytes(cert), send_certificate_chain=True)
            try:
                credential.get_token("https://storage.azure.com/.default")
                return credential
            except ClientAuthenticationError as ex:
                getLogger().error(ex.message)
                continue

    raise RuntimeError("Authentication failed with managed identity and certificates. No valid authentication method available.")

def upload(globpath: str, container: str, queue: Optional[str], storage_account_uri: str):
    try:
        credential = get_credential()
        files = glob(globpath, recursive=True)
        any_upload_or_queue_failed = False
        for infile in files:
            blob_name = get_unique_name(infile, os.getenv('HELIX_WORKITEM_ID') or str(uuid.uuid4()))

            getLogger().info("uploading {}".format(infile))

            blob_client = BlobClient(account_url=storage_account_uri.format('blob'), container_name=container, blob_name=blob_name, credential=credential)
            
            upload_succeded = False
            with open(infile, "rb") as data:
                try:
                    def _upload():
                        blob_client.upload_blob( # pyright: ignore[reportUnknownMemberType] -- type stub contains Unknown kwargs
                            data, 
                            blob_type="BlockBlob", 
                            content_settings=ContentSettings(content_type="application/json"))

                    retry_on_exception(_upload, raise_exceptions=[ResourceExistsError])
                    upload_succeded = True
                except Exception as ex:
                    any_upload_or_queue_failed = True
                    getLogger().error("upload failed")
                    getLogger().error('{0}: {1}'.format(type(ex), str(ex)))

            if upload_succeded:
                if queue is not None:
                    try:
                        queue_client = QueueClient(account_url=storage_account_uri.format('queue'), queue_name=queue, credential=credential, message_encode_policy=TextBase64EncodePolicy())
                        message = QueueMessage(container, blob_name)
                        retry_on_exception(lambda: queue_client.send_message(json.dumps(message.__dict__)))
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