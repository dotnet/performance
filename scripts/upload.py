from random import randint
import uuid
from azure.storage.blob import BlobClient, ContentSettings
from azure.storage.queue import QueueClient, TextBase64EncodePolicy
from azure.core.exceptions import ResourceExistsError, ClientAuthenticationError
from azure.identity import DefaultAzureCredential, ClientAssertionCredential, CertificateCredential
from traceback import format_exc
from glob import glob
from performance.common import retry_on_exception, iswin, islinux
from performance.constants import TENANT_ID, ARC_CLIENT_ID, CERT_SUBJECT, CERT_CLIENT_ID
import os
import json
import ssl
from cryptography import x509

from logging import getLogger

class QueueMessage:
    container_name: str
    blob_name: str

    def __init__(self, container: str, name: str):
        self.container_name = container
        self.blob_name = name

def get_unique_name(filename: str, unique_id: str) -> str:
    newname = "{0}-{1}".format(unique_id, os.path.basename(filename))
    if len(newname) > 1024:
        newname = "{0}-perf-lab-report.json".format(randint(1000, 9999))
    return newname

def upload(globpath: str, container: str, queue: str, sas_token_env: str, storage_account_uri: str):
    try:
        credential = None
        try:
            dac = DefaultAzureCredential()
            credential = ClientAssertionCredential(TENANT_ID, ARC_CLIENT_ID, lambda: dac.get_token("api://AzureADTokenExchange/.default").token)
            credential.get_token("https://storage.azure.com/.default")
        except ClientAuthenticationError as ex:
            getLogger().info("Unable to use managed identity. Falling back to certificate.")
            cert_collection = list()
            if iswin():
                for bin_cert in ssl.enum_certificates("MY"):
                    cert = x509.load_pem_x509_certificate(bin_cert[0])
                    if cert and cert.subject == CERT_SUBJECT:
                        cert_collection.append(cert)
            elif islinux():
                user_dir = os.path.expanduser("~")
                cert_dir = os.path.join(user_dir, ".dotnet/corefx/cryptography/x509stores/my")
                pfx_files = glob(os.path.join(cert_dir, "*.pfx"))

                for pfx_file in pfx_files:
                    with open(pfx_file, "rb") as f:
                        pfx_data = f.read()
                        cert = x509.load_pem_x509_certificate(pfx_data)
                        if cert and cert.subject == CERT_SUBJECT:
                            cert_collection.append(cert)
            for cert in cert_collection:
                try:
                    credential = CertificateCredential(TENANT_ID, CERT_CLIENT_ID, cert)
                    credential.get_token("https://storage.azure.com/.default")
                    break
                except ClientAuthenticationError as ex:
                    getLogger().info("Certificate not valid for client id: {0}".format(ARC_CLIENT_ID))
        if credential is None:
            getLogger().error("Sas token environment variable {} was not defined.".format(sas_token_env))
            return 1

        files = glob(globpath, recursive=True)
        any_upload_or_queue_failed = False
        for infile in files:
            blob_name = get_unique_name(infile, os.getenv('HELIX_WORKITEM_ID') or str(uuid.uuid4()))

            getLogger().info("uploading {}".format(infile))

            blob_client = BlobClient(account_url=storage_account_uri.format('blob'), container_name=container, blob_name=blob_name, credential=credential)
            
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