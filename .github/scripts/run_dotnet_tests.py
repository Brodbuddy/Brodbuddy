#!/usr/bin/env python3
# .github/scripts/run_dotnet_tests.py

import os
import subprocess
import glob
import shutil
import re
import xml.etree.ElementTree as ET

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


def rename_test_files():
    """
    Renames test files by examining their content to determine which project they belong to.
    """
    os.chdir("TestResults")

    trx_files = glob.glob("test-results_net8.0_*.trx")
    html_files = glob.glob("test-results_net8.0_*.html")
    
    project_mapping = {}
    
    for trx_file in trx_files:
        try:
            tree = ET.parse(trx_file)
            root = tree.getroot()
            
            for test_element in root.findall(".//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTest"):
                class_name = test_element.get("className", "")
                if class_name:
                    project_name = class_name.split('.')[0]
                    project_mapping[trx_file] = project_name
                    break
        except Exception as e:
            print(f"Error processing {trx_file}: {e}")
    
    for trx_file, project_name in project_mapping.items():
        try:
            new_name = f"{project_name}.trx"
            print(f"Renaming {trx_file} to {new_name}")
            os.rename(trx_file, new_name)
        except Exception as e:
            print(f"Error renaming {trx_file}: {e}")
    
    for html_file in html_files:
        try:
            with open(html_file, 'r', encoding='utf-8') as f:
                content = f.read()
                
            for project_name in set(project_mapping.values()):
                if project_name in content:
                    new_name = f"{project_name}.html"
                    print(f"Renaming {html_file} to {new_name}")
                    os.rename(html_file, new_name)
                    break
        except Exception as e:
            print(f"Error processing HTML file {html_file}: {e}")


def main():
    try:
        print("Cleaning old test results...")
        clean_test_results()

        print("Running tests...")
        output = run_tests()
        print(output)

        print("Renaming test files...")
        rename_test_files()

        print("Test run and file renaming completed successfully!")

    except Exception as e:
        print(f"Error: {str(e)}")
        exit(1)

if __name__ == "__main__":
    main()
