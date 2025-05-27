#!/usr/bin/env python3
# .github/scripts/generate_coverage_report.py

import subprocess
import re
import os
import sys
import xml.etree.ElementTree as ET

from pathlib import Path
from typing import NamedTuple, Optional
from enum import Enum

REPORT_GENERATOR_CMD = "reportgenerator"
COVERAGE_INPUT_PATTERN = "TestResults/**/coverage.cobertura.xml"
COVERAGE_OUTPUT_DIR = Path("coverage-report")
COBERTURA_REPORT_FILENAME = "Cobertura.xml"
TARGET_REPORT_TYPES = "SonarQube;HTML;Cobertura"
GITHUB_SUMMARY_ENV_VAR = "GITHUB_STEP_SUMMARY"
GITHUB_ENV_VAR = "GITHUB_ENV"


class CoverageMetrics(NamedTuple):
    lines_covered: int = 0
    lines_valid: int = 0
    branches_covered: int = 0
    branches_valid: int = 0

    @property
    def line_rate(self) -> float:
        if self.lines_valid == 0:
            return 1.0
        return self.lines_covered / self.lines_valid

    @property
    def branch_rate(self) -> float:
        """Calculate branch coverage rate."""
        if self.branches_valid == 0:
            return 1.0  # Vi beregner som 100% covered hvis ingen branch eksister
        return self.branches_covered / self.branches_valid


class CoverageSummary(NamedTuple):
    overall: CoverageMetrics
    packages: dict[str, CoverageMetrics]


class CoverageStatus(Enum):
    GREEN = "🟢"
    YELLOW = "🟡"
    RED = "🔴"


def run_report_generator() -> bool:
    print("Generating coverage reports...")
    report_path = COVERAGE_OUTPUT_DIR
    report_path.mkdir(parents=True, exist_ok=True)

    command = [
        REPORT_GENERATOR_CMD,
        f"-reports:{COVERAGE_INPUT_PATTERN}",
        f"-targetdir:{report_path}",
        f"-reporttypes:{TARGET_REPORT_TYPES}",
        "-excludebyattribute:*ExcludeFromCodeCoverage*",
        "-assemblyfilters:Startup;-SharedTestDependencies;-Brodbuddy.WebSocket;-Brodbuddy.TcpProxy"
    ]

    try:
        subprocess.run(
            command,
            check=True,
            capture_output=True,
            text=True,
            timeout=300,  # 5 minutter
        )
        print("ReportGenerator finished successfully")
        return True
    except subprocess.CalledProcessError as e:
        print(f"Error running ReportGenerator (Command: {' '.join(command)})")
        print(f"Exit Code: {e.returncode}")
        print(f"Stderr:\n{e.stderr}")
        print(f"Stdout:\n{e.stdout}")
    except FileNotFoundError:
        print(f"Error: '{REPORT_GENERATOR_CMD}' command not found.")
        print("Make sure the ReportGenerator .NET tool is installed and in the PATH.")
    except subprocess.TimeoutExpired:
        print("Error: ReportGenerator timed out after 300 seconds.")
    except Exception as e:
        print(f"An unexpected error occurred running ReportGenerator: {e}")

    return False


def _parse_branch_coverage(condition_coverage: str) -> tuple[int, int]:
    match = re.search(r"\(\s*(\d+)\s*/\s*(\d+)\s*\)", condition_coverage)
    if match:
        return int(match.group(1)), int(match.group(2))

    # Fallback for procent - hvis oventsående ikke virker, dette er bare en approksimation
    percent_match = re.search(r"(\d+)%", condition_coverage)
    if percent_match:
        coverage_percent = int(percent_match.group(1))
        if coverage_percent > 0:
            # Kan ikke finde ud af total mængde af branches, dette er en approksimation
            return 1, 1
        else:
            return 0, 1  # Antager 1 branch hvis procent er 0%
    return 0, 0  # Default hvis intet brugbar information er fundet


def parse_cobertura_report(file_path: Path) -> Optional[CoverageSummary]:
    print(f"Parsing Cobertura report: {file_path}")
    if not file_path.is_file():
        print(f"Error: Cobertura report file not found at {file_path}")
        return None

    try:
        tree = ET.parse(file_path)
        root = tree.getroot()

        overall_metrics = CoverageMetrics(
            lines_covered=int(root.attrib.get("lines-covered", 0)),
            lines_valid=int(root.attrib.get("lines-valid", 0)),
            branches_covered=int(root.attrib.get("branches-covered", 0)),
            branches_valid=int(root.attrib.get("branches-valid", 0)),
        )

        package_metrics: dict[str, CoverageMetrics] = {}
        packages_element = root.find("packages")

        if packages_element is None:
            print("Warning: No <packages> element found in Cobertura report.")
        else:
            for package in packages_element.findall("package"):
                package_name = package.attrib.get("name", "UnknownPackage")
                pkg_lines_covered = 0
                pkg_lines_valid = 0
                pkg_branches_covered = 0
                pkg_branches_valid = 0

                classes_element = package.find("classes")
                if classes_element is not None:
                    for class_elem in classes_element.findall("class"):
                        lines_element = class_elem.find("lines")
                        if lines_element is not None:
                            for line in lines_element.findall("line"):
                                pkg_lines_valid += 1
                                if int(line.attrib.get("hits", 0)) > 0:
                                    pkg_lines_covered += 1

                                if line.attrib.get("branch", "false").lower() == "true":
                                    condition_coverage = line.attrib.get(
                                        "condition-coverage", ""
                                    )
                                    covered, total = _parse_branch_coverage(
                                        condition_coverage
                                    )
                                    pkg_branches_covered += covered
                                    pkg_branches_valid += total

                package_metrics[package_name] = CoverageMetrics(
                    lines_covered=pkg_lines_covered,
                    lines_valid=pkg_lines_valid,
                    branches_covered=pkg_branches_covered,
                    branches_valid=pkg_branches_valid,
                )
        print("Successfully parsed Cobertura report.")
        return CoverageSummary(overall=overall_metrics, packages=package_metrics)

    except ET.ParseError as e:
        print(f"Error parsing Cobertura XML file '{file_path}': {e}")
    except (KeyError, ValueError, AttributeError) as e:
        print(f"Error processing data in Cobertura XML file '{file_path}': {e}")
    except Exception as e:
        print(f"An unexpected error occurred during XML parsing: {e}")
        import traceback

        traceback.print_exc()

    return None


def get_status_emoji(score_percentage: float) -> CoverageStatus:
    if score_percentage >= 80:
        return CoverageStatus.GREEN
    elif score_percentage >= 50:
        return CoverageStatus.YELLOW
    else:
        return CoverageStatus.RED


def format_percentage(rate: float) -> str:
    return f"{rate * 100:.1f}%"


def generate_markdown_summary(summary: CoverageSummary) -> str:
    print("Generating markdown summary...")
    lines: list[str] = []
    lines.append("### Code Coverage Summary")
    lines.append("")

    lines.append(
        "| Package | Status | Line Coverage | Lines (C/V) | Branch Coverage | Branches (C/V) |"
    )
    lines.append(
        "|---------|--------|---------------|-------------|-----------------|----------------|"
    )

    # Overordnet række
    overall_line_pct = summary.overall.line_rate * 100
    line_emoji = get_status_emoji(overall_line_pct).value
    lines.append(
        f"| **Overall** | {line_emoji} | **{format_percentage(summary.overall.line_rate)}** "
        f"| {summary.overall.lines_covered}/{summary.overall.lines_valid} "
        f"| **{format_percentage(summary.overall.branch_rate)}** "
        f"| {summary.overall.branches_covered}/{summary.overall.branches_valid} |"
    )

    # Package rækker (sorteret)
    for pkg_name, stats in sorted(summary.packages.items()):
        line_pct = stats.line_rate * 100
        pkg_line_emoji = get_status_emoji(line_pct).value
        lines.append(
            f"| {pkg_name} | {pkg_line_emoji} | {format_percentage(stats.line_rate)} "
            f"| {stats.lines_covered}/{stats.lines_valid} "
            f"| {format_percentage(stats.branch_rate)} "
            f"| {stats.branches_covered}/{stats.branches_valid} |"
        )

    lines.append("\n_Note: Se artifacts for detaljeret HTML rapport._")
    return "\n".join(lines)


def write_github_summary(markdown: str) -> None:
    summary_path_str = os.environ.get(GITHUB_SUMMARY_ENV_VAR)
    if summary_path_str:
        summary_path = Path(summary_path_str)
        try:
            # Bruger 'a' mode for appending, som anbefalet af GitHub Actions docs
            with summary_path.open("a", encoding="utf-8") as f:
                f.write(markdown + "\n")
            print(f"Successfully wrote coverage summary to ${GITHUB_SUMMARY_ENV_VAR}")
        except IOError as e:
            print(f"Error writing to GITHUB_STEP_SUMMARY file '{summary_path}': {e}")
            # Fallback til stdout hvis at skrive fejler
            _print_summary_stdout(markdown)
    else:
        print(f"{GITHUB_SUMMARY_ENV_VAR} environment variable not set.")
        _print_summary_stdout(markdown)


def write_github_env(overall_metrics: CoverageMetrics) -> None:
    env_path_str = os.environ.get(GITHUB_ENV_VAR)
    if env_path_str:
        env_path = Path(env_path_str)
        try:
            line_pct = int(overall_metrics.line_rate * 100)
            branch_pct = int(overall_metrics.branch_rate * 100)
            with env_path.open("a", encoding="utf-8") as env_file:
                env_file.write(f"LINE_COVERAGE_PCT={line_pct}\n")
                env_file.write(f"BRANCH_COVERAGE_PCT={branch_pct}\n")
            print(f"Overall Line Coverage set to: {line_pct}%")
            print(f"Overall Branch Coverage set to: {branch_pct}%")
        except IOError as e:
            print(
                f"Error writing coverage percentages to GITHUB_ENV file '{env_path}': {e}"
            )
        except Exception as e:
            print(f"An unexpected error occurred writing to GITHUB_ENV: {e}")
    else:
        print(
            f"Warning: {GITHUB_ENV_VAR} environment variable not found. Cannot set coverage percentages."
        )


def _print_summary_stdout(markdown: str) -> None:
    """Helper to print summary to stdout as a fallback."""
    print("\n--- Coverage Summary (stdout fallback) ---")
    print(markdown)
    print("--- End Summary ---\n")


def main() -> int:
    """Main script execution logic."""
    print("Starting coverage report processing...")

    if not run_report_generator():
        print("Failed to generate reports. Exiting.")
        write_github_summary(
            "### Code Coverage Summary\n\n🔴 Failed to generate coverage reports. Check workflow logs.\n"
        )
        return 1  # Indikerer fejl

    cobertura_file = COVERAGE_OUTPUT_DIR / COBERTURA_REPORT_FILENAME
    coverage_summary = parse_cobertura_report(cobertura_file)

    if coverage_summary:
        markdown_summary = generate_markdown_summary(coverage_summary)
        write_github_summary(markdown_summary)
        write_github_env(coverage_summary.overall)
        print("Coverage processing completed successfully.")
        return 0  # Indikerer success
    else:
        print("Failed to parse coverage report. Exiting.")
        if cobertura_file.exists():
            write_github_summary(
                "### Code Coverage Summary\n\n🔴 Error parsing coverage report. Check workflow logs.\n"
            )
        else:
            write_github_summary(
                f"### Code Coverage Summary\n\n🔴 Coverage report file (`{COBERTURA_REPORT_FILENAME}`) not found in `{COVERAGE_OUTPUT_DIR}`.\n"
            )
        return 1  # Indikerer fejl


if __name__ == "__main__":
    # sys.exit accepterer int, så vi fanger returkoden fra main()
    sys.exit(main())
