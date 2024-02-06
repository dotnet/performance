from json import loads, dumps
from typing import Optional
from urllib.request import urlopen, Request
from urllib.parse import urlencode
from urllib.error import HTTPError
import time
import sys
import os

tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47"
appId = "c2fe4cd0-be4a-468b-aa4f-078c67dcab6e"

uploadService = "https://perfcontrib.azurewebsites.net"
uploadEndpoint = f"{uploadService}/api/UploadPerfData"
listEndpoint = f"{uploadService}/api/ListUploads"
authEndpoint = f"{uploadService}/.auth/login/aad"
authDetailsEndpoint = f"{uploadService}/.auth/me"

aadUrl = f"https://login.microsoftonline.com/{tenantId}"

def get_token() -> str:
    path = os.path.expanduser("~/.perfcontrib")

    token: Optional[str] = None

    try:
        if not os.path.exists(path):
            os.makedirs(path)
        with open(os.path.expanduser("~/.perfcontrib/token")) as tokenfile:
            token = tokenfile.readline()
    except FileNotFoundError:
        pass

    if token:
        try:
            with urlopen(Request(authDetailsEndpoint,
                    headers = { "X-ZUMO-AUTH": token })) as response:
                print("Using cached credentials.")
        except HTTPError as error:
            token = None

    if not token:
        token = authenticate()
    
    if token:
        with open(os.path.expanduser("~/.perfcontrib/token"), "w") as tokenfile:
            tokenfile.write(token)
    
    return token

def authenticate() -> str:
    authBody = {
        "tenant": tenantId,
        "client_id": appId,
        "scope": "User.Read openid profile",
    }
    
    authBodyEncoded = urlencode(authBody).encode()

    with urlopen(Request(f"{aadUrl}/oauth2/v2.0/devicecode", data = authBodyEncoded)) as response:
        devicecodeResponse = loads(response.read().decode('utf-8'))

    print(devicecodeResponse["message"])
    
    authBody2 = {
        "tenant": tenantId, 
        "grant_type": "urn:ietf:params:oauth:grant-type:device_code",
        "client_id": appId,
        "device_code": devicecodeResponse["device_code"]
    }
    
    authBody2Encoded = urlencode(authBody2).encode()
    
    authStatus = "waiting"
    print("waiting", end="", flush=True)
    while (authStatus == "waiting"):
        # Try to get the access token. if we encounter an error check the reason.
        # If the reason is we are waiting then sleep for some time.
        # If the reason is the user has declined or we timed out then quit.
        try:
            with urlopen(Request(f"{aadUrl}/oauth2/v2.0/token", data = authBody2Encoded)) as response:
                tokenResponse = loads(response.read().decode('utf-8'))
            authStatus = "done"
        except Exception as ex:
            reason = loads(ex.read().decode('utf-8'))["error"]
            if reason == "authorization_pending":
                print(".", end="", flush=True)
                time.sleep(5)
            elif reason == "authorization_declined":
                authStatus = "failed"
            elif reason == "expired_token":
                authStatus = "failed"

    print()

    if authStatus == "failed": raise "Authentication failed"

    idToken = tokenResponse["id_token"]

    print("Thanks.")
    authBody3 = {
        "access_token": idToken
    }

    authBody3Json = dumps(authBody3).encode()

    with urlopen(Request(authEndpoint,
                data = authBody3Json,
                headers = {"Content-Type": "application/json"})) as response:
        aadLoginResponse = loads(response.read().decode('utf-8'))

    token = aadLoginResponse["authenticationToken"]

    return token

def upload(filename: str) -> None:
    token = get_token()

    print("Uploading.")
   
    with urlopen(Request(uploadEndpoint,
                headers = { "X-ZUMO-AUTH": token, "Content-Type": "application/octet-stream", "X-Filename": filename },
                data = open(filename,"rb"))) as response:
        print(loads(response.read().decode('utf-8'))["message"])

def list_uploads() -> None:
    token = get_token()

    with urlopen(Request(listEndpoint,
                headers = { "X-ZUMO-AUTH": token })) as response:
        uploads = loads(response.read().decode('utf-8'))
        for filename in uploads["filenames"]:
            print(filename)
    
    print()

if __name__ == "__main__":
    if (len(sys.argv) <= 1):
        print("""Usage:

    Upload a file:
        benchmark_monthly_upload.py filename_to_upload.tar.gz

    List your uploads:
        benchmark_monthly_upload.py --list
""")
        exit(0)

    if (sys.argv[1] == "--list"):
        list_uploads()
    else:
        upload(sys.argv[1])
