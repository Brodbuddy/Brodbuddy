#!/usr/bin/env python3
# .github/scripts/generate_sonarqube_summary.py

import os
import sys
import requests
import json

from typing import Optional, NamedTuple, Any
from enum import Enum
from pathlib import Path

GITHUB_SUMMARY_ENV_VAR = "GITHUB_STEP_SUMMARY"
SONAR_API_QG_STATUS = "/api/qualitygates/project_status"
SONAR_API_METRICS = "/api/measures/component"
DEFAULT_METRICS = "security_rating,reliability_rating,sqale_rating,coverage,duplicated_lines_density,security_hotspots_reviewed,violations"
REQUEST_TIMEOUT = 30


class QualityGateCondition(NamedTuple):
    metric_key: str
    status: str
    value: str
    error_threshold: str


class QualityGateStatus(NamedTuple):
    status: str
    conditions: list[QualityGateCondition]


class Metric(NamedTuple):
    metric: str
    value: str


class SonarQubeAnalysis(NamedTuple):
    quality_gate: Optional[QualityGateStatus]
    metrics: Optional[dict[str, str]]
    dashboard_url: Optional[str]


class ConditionStatus(Enum):
    OK = "✅"
    ERROR = "❌"
    WARN = "⚠️"
    UNKNOWN = "❓"


def _make_sonar_request(sonar_url: str, sonar_token: Optional[str], api_endpoint: str, params: dict[str, Any]) -> Optional[dict[str, Any]]:
    full_url = f"{sonar_url.rstrip('/')}{api_endpoint}"
    headers = {"Accept": "application/json"}
    auth = (sonar_token, "") if sonar_token else None

    print(f"Requesting SonarQube data from: {full_url} with params: {params}")
    try:
        response = requests.get(full_url, headers=headers, auth=auth, params=params, timeout=REQUEST_TIMEOUT)
        response.raise_for_status()
        return response.json()
    except requests.exceptions.Timeout:
        print(f"Error: Request to SonarQube timed out ({full_url})")
    except requests.exceptions.ConnectionError as e:
        print(f"Error: Could not connect to SonarQube ({full_url}): {e}")
    except requests.exceptions.HTTPError as e:
        print(f"Error: HTTP Error fetching SonarQube data ({full_url}): {e.response.status_code} {e.response.reason}")
    except requests.exceptions.RequestException as e:
        print(f"Error: Request failed to SonarQube ({full_url}): {e}")
    except json.JSONDecodeError as e:
        print(f"Error: Failed to decode JSON response from SonarQube ({full_url}): {e}")
    except Exception as e:
        print(f"An unexpected error occurred during SonarQube request: {e}")
    return None


def get_quality_gate_details(sonar_url: str, sonar_token: Optional[str], project_key: str) -> Optional[QualityGateStatus]:
    print("Fetching Quality Gate details...")
    params = {"projectKey": project_key}
    qg_raw_data = _make_sonar_request(
        sonar_url, sonar_token, SONAR_API_QG_STATUS, params
    )

    if qg_raw_data and "projectStatus" in qg_raw_data:
        project_status = qg_raw_data["projectStatus"]
        conditions_raw = project_status.get("conditions", [])
        conditions = [
            QualityGateCondition(
                metric_key=cond.get("metricKey", "unknown_metric"),
                status=cond.get("status", "UNKNOWN").upper(),
                value=cond.get("actualValue", "N/A"),
                error_threshold=cond.get("errorThreshold", "N/A"),
            )
            for cond in conditions_raw
        ]
        status = project_status.get("status", "UNKNOWN").upper()
        print(f"Quality Gate Status: {status}")
        return QualityGateStatus(status=status, conditions=conditions)
    else:
        print("Error: Could not retrieve valid Quality Gate data.")
        return None


def get_project_metrics(sonar_url: str, sonar_token: Optional[str], project_key: str, metric_keys: str = DEFAULT_METRICS) -> Optional[dict[str, str]]:
    print(f"Fetching project metrics: {metric_keys}")
    params = {"component": project_key, "metricKeys": metric_keys}
    metrics_raw_data = _make_sonar_request(
        sonar_url, sonar_token, SONAR_API_METRICS, params
    )

    if (
        metrics_raw_data
        and "component" in metrics_raw_data
        and "measures" in metrics_raw_data["component"]
    ):
        metrics = {
            measure.get("metric"): measure.get("value", "N/A")
            for measure in metrics_raw_data["component"]["measures"]
            if "metric" in measure
        }
        print("Successfully fetched project metrics.")
        return metrics
    else:
        print("Error: Could not retrieve valid project metrics data.")
        return None


def format_condition_status_emoji(status: str) -> str:
    return (
        ConditionStatus[status].value
        if status in ConditionStatus.__members__
        else ConditionStatus.UNKNOWN.value
    )


def _format_metric_value(metric_key: str, value: str, threshold: str) -> tuple[str, str]:
    display_value = value
    display_threshold = threshold
    if metric_key in [
        "coverage",
        "duplicated_lines_density",
        "security_hotspots_reviewed",
    ]:
        try:
            display_value = f"{float(value):.1f}%" if value != "N/A" else value
        except (ValueError, TypeError):
            pass
        try:
            display_threshold = (
                f"{float(threshold):.1f}%" if threshold != "N/A" else threshold
            )
        except (ValueError, TypeError):
            pass
    return display_value, display_threshold


def generate_summary_markdown(analysis_data: SonarQubeAnalysis) -> str:
    print("Generating markdown summary...")
    lines: list[str] = []
    lines.append("### SonarQube analyse resultater")
    lines.append("")

    if analysis_data.quality_gate:
        qg = analysis_data.quality_gate
        status_text = qg.status.replace("_", " ").title()
        status_emoji = format_condition_status_emoji(qg.status)
        lines.append(f"**Quality Gate:** {status_emoji} {status_text}")
        lines.append("")

        if qg.conditions:
            lines.append("| Metric | Status | Value | Error Threshold |")
            lines.append("|--------|--------|-------|-----------------|")

            metric_display_names = {
                "coverage": "Coverage",
                "duplicated_lines_density": "Duplications",
                "security_hotspots_reviewed": "Hotspots Reviewed",
                "security_rating": "Security Rating",
                "reliability_rating": "Reliability Rating",
                "sqale_rating": "Maintainability Rating",
                "violations": "Issues",
                "new_coverage": "New Code Coverage",
                "new_duplicated_lines_density": "New Code Duplications",
            }

            for cond in qg.conditions:
                metric_name = metric_display_names.get(
                    cond.metric_key, cond.metric_key.replace("_", " ").title()
                )
                cond_status_emoji = format_condition_status_emoji(cond.status)
                display_value, display_threshold = _format_metric_value(
                    cond.metric_key, cond.value, cond.error_threshold
                )
                lines.append(f"| {metric_name} | {cond_status_emoji} | {display_value} | {display_threshold} |")
            lines.append("")
        else:
            lines.append("_No specific conditions reported for the Quality Gate._")
            lines.append("")
    else:
        lines.append(f"**Quality Gate:** {ConditionStatus.UNKNOWN.value} Could not retrieve status.")
        lines.append("")

    if analysis_data.dashboard_url:
        lines.append(f"[Se fuld analyse på SonarQube]({analysis_data.dashboard_url})")

    return "\n".join(lines)


def write_github_summary(markdown: str) -> None:
    summary_path_str = os.environ.get(GITHUB_SUMMARY_ENV_VAR)
    if summary_path_str:
        summary_path = Path(summary_path_str)
        try:
            with summary_path.open("a", encoding="utf-8") as f:
                f.write(markdown + "\n\n")
            print(f"Successfully wrote SonarQube summary to ${GITHUB_SUMMARY_ENV_VAR}")
        except IOError as e:
            print(f"Error writing to GITHUB_STEP_SUMMARY file '{summary_path}': {e}")
            _print_summary_stdout(markdown)
    else:
        print(f"{GITHUB_SUMMARY_ENV_VAR} environment variable not set.")
        _print_summary_stdout(markdown)


def _print_summary_stdout(markdown: str) -> None:
    print("\n--- SonarQube Summary (stdout fallback) ---")
    print(markdown)
    print("--- End Summary ---\n")


def main() -> int:
    sonar_url = os.environ.get("SONARQUBE_SERVER")
    sonar_token = os.environ.get("SONARQUBE_TOKEN")
    project_key = os.environ.get("SONARQUBE_PROJECT_KEY")
    fail_on_gate_str = os.environ.get("FAIL_ON_QUALITY_GATE", "false")
    fail_on_gate = fail_on_gate_str.lower() == "true"

    if not sonar_url or not project_key:
        print("Error: SONARQUBE_SERVER and SONARQUBE_PROJECT_KEY environment variables must be set.")
        write_github_summary("### SonarQube Analysis Results\n\n🔴 Missing required configuration (URL, Project Key).")
        return 1

    dashboard_url = f"{sonar_url.rstrip('/')}/dashboard?id={project_key}"
    print(f"Starting SonarQube summary generation for project: {project_key}")

    qg_details = get_quality_gate_details(sonar_url, sonar_token, project_key)
    metrics = get_project_metrics(sonar_url, sonar_token, project_key)

    analysis_data = SonarQubeAnalysis(quality_gate=qg_details, metrics=metrics, dashboard_url=dashboard_url)

    markdown = generate_summary_markdown(analysis_data)
    write_github_summary(markdown)

    qg_status = qg_details.status if qg_details else "UNKNOWN"

    if fail_on_gate and qg_status == "ERROR":
        print("Quality Gate Failed! Exiting with error code.")
        return 1
    elif qg_status == "UNKNOWN":
        print("Quality Gate status unknown. Check logs.")
        return 0
    else:
        print("Summary generated successfully.")
        return 0


if __name__ == "__main__":
    # sys.exit accepterer int, så vi fanger returkoden fra main()
    sys.exit(main())
