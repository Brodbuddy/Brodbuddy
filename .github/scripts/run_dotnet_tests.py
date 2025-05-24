#!/usr/bin/env python3
# .github/scripts/run_dotnet_tests.py


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
        "--filter", "FullyQualifiedName!~PlaywrightTests",
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


def rename_test_files(output):
    os.chdir("TestResults")
    
    timestamp_to_project = {}
    
    lines = output.split('\n')
    timestamp = None
    
    for line in lines:
        if "Results File:" in line and "test-results_net8.0_" in line:
            match = re.search(r"test-results_net8\.0_(\d+)\.trx", line)
            if match:
                timestamp = match.group(1)
        
        elif timestamp and ".dll (net" in line:
            match = re.search(r"- ([\w\.]+)\.dll", line)
            if match:
                dll_name = match.group(1)
                timestamp_to_project[timestamp] = dll_name
                print(f"Mapped timestamp {timestamp} to project {dll_name}")
                timestamp = None
    
    for trx_file in glob.glob("test-results_net8.0_*.trx"):
        try:
            timestamp = re.search(r"test-results_net8\.0_(\d+)\.trx", trx_file).group(1)
            if timestamp in timestamp_to_project:
                project_name = timestamp_to_project[timestamp]
                new_name = f"{project_name}.trx"
                print(f"Renaming {trx_file} to {new_name}")
                os.rename(trx_file, new_name)
        except Exception as e:
            print(f"Error renaming TRX file: {e}")
    
    for html_file in glob.glob("test-results_net8.0_*.html"):
        try:
            timestamp = re.search(r"test-results_net8\.0_(\d+)\.html", html_file).group(1)
            if timestamp in timestamp_to_project:
                project_name = timestamp_to_project[timestamp]
                new_name = f"{project_name}.html"
                print(f"Renaming {html_file} to {new_name}")
                os.rename(html_file, new_name)
        except Exception as e:
            print(f"Error renaming HTML file: {e}")


def main():
    try:
        print("Cleaning old test results...")
        clean_test_results()

        print("Running tests...")
        output = run_tests()
        print(output)

        print("Renaming test files...")
        rename_test_files(output)

        print("Test run and file renaming completed successfully!")

    except Exception as e:
        print(f"Error: {str(e)}")
        exit(1)

if __name__ == "__main__":
    main()