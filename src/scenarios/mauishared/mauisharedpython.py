import subprocess
import os

# Remove the aab files as we don't need them, this saves space in the correlation payload
def RemoveAABFiles(output_dir="."):
    file_list = os.listdir(output_dir)
    for file in file_list:
        if file.endswith(".aab"):
            os.remove(os.path.join(output_dir, file))
