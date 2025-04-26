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
    
    trx_files = glob.glob("test-results_net8.0_*.trx")
    html_files = glob.glob("test-results_net8.0_*.html")
    
    timestamp_to_project = {}
    
    lines = output.splitlines()
    current_timestamp = None
    
    for line in lines:
        if "Results File:" in line and "test-results_net8.0_" in line:
            match = re.search(r"test-results_net8\.0_(\d+)\.trx", line)
            if match:
                current_timestamp = match.group(1)
        
        if current_timestamp and "Passed!" in line and ".dll (net" in line:
            match = re.search(r"- (.*?)\.dll \(net", line)
            if match:
                project_name = match.group(1)
                timestamp_to_project[current_timestamp] = project_name
                print(f"Mapped timestamp {current_timestamp} to project {project_name}")
                current_timestamp = None
    
    for trx_file in trx_files:
        timestamp = re.search(r'test-results_net8\.0_(\d+)\.trx', trx_file).group(1)
        if timestamp in timestamp_to_project:
            project_name = timestamp_to_project[timestamp]
            new_name = f"{project_name}.trx"
            print(f"Renaming {trx_file} to {new_name}")
            os.rename(trx_file, new_name)
        else:
            print(f"Warning: Could not determine project for {trx_file}")
    
    for html_file in html_files:
        timestamp = re.search(r'test-results_net8\.0_(\d+)\.html', html_file).group(1)
        if timestamp in timestamp_to_project:
            project_name = timestamp_to_project[timestamp]
            new_name = f"{project_name}.html"
            print(f"Renaming {html_file} to {new_name}")
            os.rename(html_file, new_name)
        else:
            print(f"Warning: Could not determine project for {html_file}")


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