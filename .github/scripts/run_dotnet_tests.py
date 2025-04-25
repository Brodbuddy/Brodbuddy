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
    os.chdir("TestResults")

    trx_files = glob.glob("test-results_net8.0_*.trx")
    html_files = glob.glob("test-results_net8.0_*.html")
    
    timestamp_to_project = {}
    
    for trx_file in trx_files:
        try:
            timestamp = re.search(r'test-results_net8\.0_(\d+)\.trx', trx_file).group(1)
            
            tree = ET.parse(trx_file)
            root = tree.getroot()
            
            namespace = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
            test_elements = root.findall(f".//{namespace}UnitTest")
            
            if test_elements:
                for test in test_elements:
                    storage_node = test.find(f".//{namespace}TestMethod")
                    if storage_node is not None:
                        class_name = storage_node.get("className", "")
                        if class_name:
                            parts = class_name.split('.')
                            if len(parts) >= 3 and parts[1] == "Tests":
                                project_name = ".".join(parts[:3])
                            elif len(parts) >= 2 and parts[1] == "Tests":
                                project_name = ".".join(parts[:2])
                            else:
                                project_name = parts[0]
                                
                            timestamp_to_project[timestamp] = project_name
                            print(f"Timestamp {timestamp} mapped to project {project_name}")
                            break
            
            if timestamp not in timestamp_to_project:
                for test in test_elements:
                    for elem in test.iter():
                        class_attr = elem.get("className")
                        if class_attr:

                            parts = class_attr.split('.')
                            if len(parts) >= 3 and parts[1] == "Tests":
                                project_name = ".".join(parts[:3])
                            elif len(parts) >= 2 and parts[1] == "Tests":
                                project_name = ".".join(parts[:2])
                            else:
                                project_name = parts[0]
                                
                            timestamp_to_project[timestamp] = project_name
                            print(f"Fallback: Timestamp {timestamp} mapped to project {project_name}")
                            break
                    if timestamp in timestamp_to_project:
                        break
            
        except Exception as e:
            print(f"Error processing TRX file {trx_file}: {e}")
    
    for trx_file in trx_files:
        try:
            timestamp = re.search(r'test-results_net8\.0_(\d+)\.trx', trx_file).group(1)
            if timestamp in timestamp_to_project:
                project_name = timestamp_to_project[timestamp]
                new_name = f"{project_name}.trx"
                print(f"Renaming {trx_file} to {new_name}")
                os.rename(trx_file, new_name)
            else:
                print(f"Warning: Could not determine project for {trx_file}")
        except Exception as e:
            print(f"Error renaming TRX file {trx_file}: {e}")
    
    for html_file in html_files:
        try:
            timestamp = re.search(r'test-results_net8\.0_(\d+)\.html', html_file).group(1)
            if timestamp in timestamp_to_project:
                project_name = timestamp_to_project[timestamp]
                new_name = f"{project_name}.html"
                print(f"Renaming {html_file} to {new_name}")
                os.rename(html_file, new_name)
            else:
                print(f"Warning: Could not determine project for {html_file}")
        except Exception as e:
            print(f"Error renaming HTML file {html_file}: {e}")


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
