#!/usr/bin/env python3
# .github/scripts/run_tests.py

import os
import subprocess
import glob
import shutil
import re

def clean_test_results():
    if os.path.exists("TestResults"):
        shutil.rmtree("TestResults")
    os.makedirs("TestResults")


def run_tests():
    cmd = [
        "dotnet",
        "test",
        "--collect:XPlat Code Coverage",
        "--logger",
        "trx;LogFilePrefix=test-results",
        "--logger",
        "html;LogFilePrefix=test-results",
        "--results-directory",
        "TestResults",
    ]

    result = subprocess.run(cmd, capture_output=True, text=True)
    return result.stdout


def extract_project_names(output):
    pattern = r"Passed!.*- (.*?)\.dll \(net"
    matches = re.finditer(pattern, output)
    return [match.group(1) for match in matches]


def rename_test_files(project_names):
    os.chdir("TestResults")

    trx_files = sorted(
        glob.glob("test-results_net8.0_*.trx"), key=os.path.getctime, reverse=True
    )
    html_files = sorted(
        glob.glob("test-results_net8.0_*.html"), key=os.path.getctime, reverse=True
    )

    # Omdøb filer for hvert projekt
    for i, project in enumerate(project_names):
        if i < len(trx_files):
            os.rename(trx_files[i], f"{project}.trx")
        if i < len(html_files):
            os.rename(html_files[i], f"{project}.html")


def main():
    try:
        print("Cleaning old test results...")
        clean_test_results()

        print("Running tests...")
        output = run_tests()
        print(output)

        print("Extracting project names...")
        project_names = extract_project_names(output)
        if not project_names:
            raise Exception("No test projects found in output")

        print(f"Found projects: {', '.join(project_names)}")

        print("Renaming test files...")
        rename_test_files(project_names)

        print("Test run and file renaming completed successfully!")

    except Exception as e:
        print(f"Error: {str(e)}")
        exit(1)


if __name__ == "__main__":
    main()
