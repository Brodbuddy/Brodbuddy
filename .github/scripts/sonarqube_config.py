#!/usr/bin/env python3
# .github/scripts/sonarqube_config.py

import subprocess
import os
import sys


def configure_sonarqube():
    """Configure and start SonarQube analysis"""

    project_key = os.environ.get("SONARQUBE_PROJECT_KEY")
    server_url = os.environ.get("SONARQUBE_SERVER")
    token = os.environ.get("SONARQUBE_TOKEN")

    if not all([project_key, server_url, token]):
        print("Error: Missing required SonarQube environment variables")
        return False

    github_event_name = os.environ.get("GITHUB_EVENT_NAME", "")
    github_pr_number = os.environ.get("GITHUB_PR_NUMBER", "")
    github_head_ref = os.environ.get("GITHUB_HEAD_REF", "")
    github_base_ref = os.environ.get("GITHUB_BASE_REF", "")
    github_repository = os.environ.get("GITHUB_REPOSITORY", "")

    exclusions = [
        "**/TestResults/**/*",
        "**/coverage-report/**/*",
        "client/**/*",
        "**/Program.cs",
        "**/PgDbContext.cs",
        "**/scaffold.py",
        "**/.github/scripts/**/*",
    ]

    cmd = [
        "dotnet",
        "sonarscanner",
        "begin",
        f"/k:{project_key}",
        f"/d:sonar.host.url={server_url}",
        f"/d:sonar.token={token}",
        "/d:sonar.coverageReportPaths=coverage-report/SonarQube.xml",
        f"/d:sonar.exclusions={','.join(exclusions)}",
        "/d:sonar.qualitygate.wait=true",
        "/d:sonar.qualitygate.timeout=300",
    ]

    is_pr = github_event_name == "pull_request"
    if is_pr and github_pr_number and github_head_ref and github_base_ref:
        print(f"Configuring SonarQube for Pull Request #{github_pr_number}")
        cmd.extend(
            [
                f"/d:sonar.pullrequest.key={github_pr_number}",
                f"/d:sonar.pullrequest.branch={github_head_ref}",
                f"/d:sonar.pullrequest.base={github_base_ref}",
                f"/d:sonar.pullrequest.github.repository={github_repository}",
            ]
        )
    else:
        print("Configuring SonarQube for main branch analysis")

    print("Starting SonarQube analysis...")
    try:
        masked_cmd = [
            c if not c.startswith("/d:sonar.token=") else "/d:sonar.token=***"
            for c in cmd
        ]
        print(f"Command: {' '.join(masked_cmd)}")

        subprocess.run(cmd, check=True)
        return True
    except subprocess.CalledProcessError as e:
        print(f"Error configuring SonarQube: {e}")
        return False


if __name__ == "__main__":
    if not configure_sonarqube():
        sys.exit(1)
