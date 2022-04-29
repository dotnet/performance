from json import loads, dumps
from urllib.request import urlopen, Request
from urllib.parse import urlencode
import time

def main() -> None:
    tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47"
    appId = "c2fe4cd0-be4a-468b-aa4f-078c67dcab6e"
    uploadService = "https://perfcontrib.azurewebsites.net/"
    
    authUrl = f"https://login.microsoftonline.com/{tenantId}"
    
    # The Url to my Function. This is the Url I am trying to access. It includes the base name above and the actual Function.
    uploadEndpoint = f"{uploadService}/api/UploadMonthlyReport"
    
    # Details of Url from https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-device-code
    # I need 'profile' scope for oid
    # I need 'openid' scope to get the id_token else that is not returned
    authBody = {
        "tenant": tenantId,
        "client_id": appId,
        "scope": "User.Read openid profile",
    }
    
    authBodyEncoded = urlencode(authBody).encode()

    with urlopen(Request(f"{authUrl}/oauth2/v2.0/devicecode", data = authBodyEncoded)) as response:
        item = loads(response.read().decode('utf-8'))

    print(item["message"])
    
    authBody2 = {
        "tenant": tenantId, 
        "grant_type": "urn:ietf:params:oauth:grant-type:device_code",
        "client_id": appId,
        "device_code": item["device_code"]
    }
    
    authBody2Encoded = urlencode(authBody2).encode()
    
    authStatus = "waiting"
    response2 = None
    while (authStatus == "waiting"):
        # Try to get the access token. if we encounter an error check the reason. 
        # If the reason is we are waiting then sleep for some time. 
        # If the reason is the user has declined or we timed out then quit.  
        try:
            with urlopen(Request(f"{authUrl}/oauth2/v2.0/token", data = authBody2Encoded)) as response:
                item = loads(response.read().decode('utf-8'))
            authStatus = "done"
        except:
            time.sleep(10)

    # Based on https://docs.microsoft.com/en-us/azure/app-service/configure-authentication-customize-sign-in-out#client-directed-sign-in
    print("Thanks.")
    authBody3 = {
        "access_token": item["id_token"]
    }

    authBody3Json = dumps(authBody3).encode()

    with urlopen(Request(f"{uploadService}/.auth/login/aad",
                data = authBody3Json,
                headers = {"Content-Type": "application/json"})) as response:
        item = loads(response.read().decode('utf-8'))

    print("Uploading.")
   
    $response3.authenticationToken
    
    
    $header = @{
        "X-ZUMO-AUTH" = $response3.authenticationToken
    }
    Invoke-RestMethod -Method GET -Uri $apiUrl -Headers $header

if __name__ == "__main__":
    my_function()