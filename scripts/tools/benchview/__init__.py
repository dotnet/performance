from sys import path
import os.path


def __add_dependencies_to_path(fileName: str) -> None:
    scriptDir = os.path.dirname(os.path.realpath(fileName))
    path.append(os.path.join(scriptDir, "..", "dependencies"))


__add_dependencies_to_path(__file__)
