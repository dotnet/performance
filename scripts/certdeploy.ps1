sleep 240
# Download the certificates

# Path to the certificate files
$certPath1 = "LabCert1.cer"
$certPath2 = "LabCert2.cer"

# Friendly names for the certificates
$friendlyName1 = "Cert 1"
$friendlyName2 = "Cert 2"

# Import the certificates
$cert1 = Import-Certificate -FilePath $certPath1 -CertStoreLocation Cert:\LocalMachine\My
$cert2 = Import-Certificate -FilePath $certPath2 -CertStoreLocation Cert:\LocalMachine\My

# Set the friendly names
$cert1.FriendlyName = $friendlyName1
$cert2.FriendlyName = $friendlyName2

# Define the action to run a PowerShell script
$action = New-ScheduledTaskAction -Execute 'C:\CertRotator\CertRotator.exe'

# Define the trigger to run daily at 8 AM
$trigger = New-ScheduledTaskTrigger -Once -At "12:00AM" -RepetitionInterval (New-TimeSpan -Hours 1) -RepetitionDuration (New-TimeSpan -Days 3650)

# Define the principal (user) under which the task will run
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

# Register the scheduled task
Register-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -TaskName "CertRotatorTask" -Description "Runs the certificate rotator script every hour."

mkdir C:\CertRotator
Copy-Item %HELIX_WORKITEM_ROOT%\CertRotator.exe C:\CertRotator