#!/usr/bin/env python3
# .github/scripts/generate_mutation_summary.py

import json
import os
import sys
import re
import traceback

from collections import defaultdict
from pathlib import Path
from typing import NamedTuple
from enum import Enum

STRYKER_REPORT_DIR = Path("StrykerOutput/reports")
STRYKER_REPORT_FILENAME = "mutation-report.json"
GITHUB_SUMMARY_ENV_VAR = "GITHUB_STEP_SUMMARY"
GITHUB_ENV_VAR = "GITHUB_ENV"

STATUS_KILLED = "Killed"
STATUS_SURVIVED = "Survived"
STATUS_TIMEOUT = "Timeout"
STATUS_NO_COVERAGE = "NoCoverage"
STATUS_IGNORED = "Ignored"
STATUS_COMPILE_ERROR = "CompileError"
STATUS_RUNTIME_ERROR = "RuntimeError"


class MutationStats(NamedTuple):
    killed: int = 0
    survived: int = 0
    timeout: int = 0
    no_coverage: int = 0
    ignored: int = 0
    compile_error: int = 0
    runtime_error: int = 0
    total_mutants: int = 0


class MutationSummary(NamedTuple):
    overall: MutationStats
    directories: dict[str, MutationStats]


class CoverageStatus(Enum):
    GREEN = "🟢"
    YELLOW = "🟡"
    RED = "🔴"


def extract_directory(file_path_str: str) -> str:
    match = re.search(r"server/([^/]+)", file_path_str)
    if match:
        return match.group(1)

    path = Path(file_path_str)
    parts = [
        p
        for p in path.parts
        if p not in ("/", ".", "server", "src", "test", "tests") and "." not in p
    ]
    if parts:
        return parts[0]

    # Fallback hvis nogen af de ovenstående fejler
    parent_dir = path.parent.name
    if parent_dir and parent_dir != ".":
        return parent_dir

    # Hvis det går helt galt
    return "unknown_dir"


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


def parse_stryker_report(file_path: Path) -> MutationSummary | None:
    print(f"Parsing Stryker report: {file_path}")
    if not file_path.is_file():
        print(f"Error: Stryker report file not found at {file_path}")
        return None

    try:
        with file_path.open("r", encoding="utf-8") as file:
            data = json.load(file)

        if not isinstance(data, dict) or "files" not in data:
            print(
                f"Error: Invalid Stryker report format in '{file_path}'. Missing 'files' key."
            )
            return None

        aggregated_stats: dict[str, dict[str, int]] = defaultdict(
            lambda: defaultdict(int)
        )
        overall_counts: dict[str, int] = defaultdict(int)

        for file_path_str, file_data in data.get("files", {}).items():
            if (
                not isinstance(file_data, dict)
                or "mutants" not in file_data
                or not isinstance(file_data["mutants"], list)
            ):
                print(f"Warning: Skipping invalid file entry for '{file_path_str}'")
                continue  # Skp denne fil

            directory = extract_directory(file_path_str)
            dir_counts = aggregated_stats[directory]

            for mutant in file_data["mutants"]:
                if not isinstance(mutant, dict) or "status" not in mutant:
                    print(
                        f"Warning: Skipping invalid mutant in file '{file_path_str}': {mutant}"
                    )
                    continue  # Skip denne mutant

                status = mutant["status"]
                dir_counts[status] += 1
                overall_counts[status] += 1

            dir_counts["total_mutants"] += len(file_data["mutants"])
            overall_counts["total_mutants"] += len(file_data["mutants"])

        directory_metrics: dict[str, MutationStats] = {}
        for dir_name, counts in aggregated_stats.items():
            stats_data_exact = {
                "killed": counts.get("Killed", 0),
                "survived": counts.get("Survived", 0),
                "timeout": counts.get("Timeout", 0),
                "no_coverage": counts.get("NoCoverage", 0),
                "ignored": counts.get("Ignored", 0),
                "compile_error": counts.get("CompileError", 0),
                "runtime_error": counts.get("RuntimeError", 0),
                "total_mutants": counts.get("total_mutants", 0),
            }
            directory_metrics[dir_name] = MutationStats(**stats_data_exact)

        overall_stats_data_exact = {
            "killed": overall_counts.get("Killed", 0),
            "survived": overall_counts.get("Survived", 0),
            "timeout": overall_counts.get("Timeout", 0),
            "no_coverage": overall_counts.get("NoCoverage", 0),
            "ignored": overall_counts.get("Ignored", 0),
            "compile_error": overall_counts.get("CompileError", 0),
            "runtime_error": overall_counts.get("RuntimeError", 0),
            "total_mutants": overall_counts.get("total_mutants", 0),
        }
        overall_metrics = MutationStats(**overall_stats_data_exact)

        print("Successfully parsed Stryker report.")
        return MutationSummary(overall=overall_metrics, directories=directory_metrics)

    except json.JSONDecodeError as e:
        print(f"Error decoding JSON from '{file_path}': {e}")
    except TypeError as e:
        print(
            f"Error creating MutationStats. Potential missing field or wrong data type in report '{file_path}': {e}"
        )
        print("Aggregated overall counts:", dict(overall_counts))
    except (KeyError, ValueError, AttributeError) as e:
        print(f"Error processing data structure in Stryker report '{file_path}': {e}")
    except Exception as e:
        print(f"An unexpected error occurred during report parsing: {e}")
        traceback.print_exc()

    return None


def get_status_emoji(score: float) -> CoverageStatus:
    if score >= 80:
        return CoverageStatus.GREEN
    elif score >= 60:
        return CoverageStatus.YELLOW
    else:
        return CoverageStatus.RED


def format_score(score: float) -> str:
    return f"{score:.1f}%"


def calculate_scores(stats: MutationStats) -> tuple[float, float]:
    detected = stats.killed + stats.timeout
    undetected = stats.survived + stats.no_coverage
    valid_mutants = detected + undetected
    covered_mutants = detected + stats.survived

    score = 100.0 if valid_mutants == 0 else (detected / valid_mutants) * 100
    score_covered = (
        100.0 if covered_mutants == 0 else (detected / covered_mutants) * 100
    )

    return score, score_covered


def generate_markdown_summary(summary: MutationSummary) -> str:
    print("Generating markdown summary...")
    lines: list[str] = []
    lines.append("### Mutation Testing Summary")
    lines.append("")
    lines.append("| Directory | Status | Score | Covered | Killed | Survived | Timeout | No Coverage | Ignored | Runtime errors | Compile errors | Detected | Undetected | Total |")
    lines.append("|-----------|--------|-------|---------|--------|----------|---------|-------------|---------|----------------|----------------|----------|------------|-------|")

    # Overordnet række
    overall = summary.overall
    overall_score, overall_score_covered = calculate_scores(overall)
    emoji = get_status_emoji(overall_score).value
    errors = overall.compile_error + overall.runtime_error
    detected = overall.killed + overall.timeout  
    undetected = overall.survived + overall.no_coverage 
    lines.append(
        f"| **Overall** | {emoji} "
        f"| **{format_score(overall_score)}** "
        f"| {format_score(overall_score_covered)} "
        f"| {overall.killed} | {overall.survived} "
        f"| {overall.timeout} | {overall.no_coverage} "
        f"| {overall.ignored} "
        f"| {overall.runtime_error} | {overall.compile_error} "
        f"| {detected} | {undetected} "
        f"| {overall.total_mutants} |"
    )

    # Directory rækker (sorteret)
    sorted_dirs = sorted(
        summary.directories.items(),
        key=lambda item: calculate_scores(item[1])[0],  # item er (dir_name, stats_obj)
        reverse=True,
    )

    for dir_name, stats in sorted_dirs:
        dir_score, dir_score_covered = calculate_scores(stats)
        emoji = get_status_emoji(dir_score).value
        errors = stats.compile_error + stats.runtime_error
        detected = stats.killed + stats.timeout 
        undetected = stats.survived + stats.no_coverage 
        lines.append(
            f"| {dir_name} | {emoji} "
            f"| {format_score(dir_score)} "
            f"| {format_score(dir_score_covered)} "
            f"| {stats.killed} | {stats.survived} "
            f"| {stats.timeout} | {stats.no_coverage} "
            f"| {stats.ignored} "
            f"| {stats.runtime_error} | {stats.compile_error} "
            f"| {detected} | {undetected} "
            f"| {stats.total_mutants} |"
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
                f.write(markdown + "\n\n")
            print(f"Successfully wrote mutation summary to ${GITHUB_SUMMARY_ENV_VAR}")
        except IOError as e:
            print(f"Error writing to GITHUB_STEP_SUMMARY file '{summary_path}': {e}")
            _print_summary_stdout(markdown)
    else:
        print(f"{GITHUB_SUMMARY_ENV_VAR} environment variable not set.")
        _print_summary_stdout(markdown)


def write_github_env(overall_metrics: MutationStats) -> None:
    env_path_str = os.environ.get(GITHUB_ENV_VAR)
    if env_path_str:
        env_path = Path(env_path_str)
        try:
            score, _ = calculate_scores(overall_metrics)
            score_int = int(round(score))

            with env_path.open("a", encoding="utf-8") as env_file:
                env_file.write(f"MUTATION_SCORE={score_int}\n")
                env_file.write(f"MUTATION_SCORE_FLOAT={score:.1f}\n")
                env_file.write(f"KILLED_MUTANTS={overall_metrics.killed}\n")
                env_file.write(f"SURVIVED_MUTANTS={overall_metrics.survived}\n")
                env_file.write(f"NOCOVERAGE_MUTANTS={overall_metrics.no_coverage}\n")
                env_file.write(f"TIMEOUT_MUTANTS={overall_metrics.timeout}\n")
                env_file.write(f"IGNORED_MUTANTS={overall_metrics.ignored}\n")
                env_file.write(
                    f"COMPILE_ERROR_MUTANTS={overall_metrics.compile_error}\n"
                )
                env_file.write(
                    f"RUNTIME_ERROR_MUTANTS={overall_metrics.runtime_error}\n"
                )
                env_file.write(f"TOTAL_MUTANTS={overall_metrics.total_mutants}\n")

            print(f"Mutation Score set to: {score_int}%")
        except IOError as e:
            print(
                f"Error writing mutation metrics to GITHUB_ENV file '{env_path}': {e}"
            )
        except Exception as e:
            print(f"An unexpected error occurred writing to GITHUB_ENV: {e}")
    else:
        print(
            f"Warning: {GITHUB_ENV_VAR} environment variable not found. Cannot set mutation metrics."
        )


def _print_summary_stdout(markdown: str) -> None:
    print("\n--- Mutation Summary (stdout fallback) ---")
    print(markdown)
    print("--- End Summary ---\n")


def main() -> int:
    print("Starting mutation report processing...")
    report_file = STRYKER_REPORT_DIR / STRYKER_REPORT_FILENAME

    if not report_file.is_file():
        print(f"Error: Stryker report file not found at '{report_file}'.")
        print("Checking directory contents:")
        try:
            stryker_output_dir = report_file.parent.parent
            if stryker_output_dir.is_dir():
                print(
                    f"Contents of {stryker_output_dir}: {os.listdir(stryker_output_dir)}"
                )
                if report_file.parent.is_dir():
                    print(
                        f"Contents of {report_file.parent}: {os.listdir(report_file.parent)}"
                    )
            else:
                print(f"Directory not found: {stryker_output_dir}")
        except Exception as e:
            print(f"Error listing directories: {e}")

        write_github_summary(
            f"### Mutation Testing Summary\n\n🔴 Stryker report file (`{STRYKER_REPORT_FILENAME}`) not found in `{STRYKER_REPORT_DIR}`.\n"
        )
        return 1  # Indikerer fejl

    mutation_summary = parse_stryker_report(report_file)

    if mutation_summary:
        overall_score, _ = calculate_scores(mutation_summary.overall)
        markdown_summary = generate_markdown_summary(mutation_summary)
        write_github_summary(markdown_summary)
        write_github_env(mutation_summary.overall)
        print(f"Overall Mutation Score: {overall_score:.1f}%")
        print("Mutation report processing completed successfully.")
        return 0  # Indikerer success
    else:
        print("Failed to parse Stryker report. Exiting.")
        write_github_summary(
            "### Mutation Testing Summary\n\n🔴 Error parsing Stryker report. Check workflow logs.\n"
        )
        return 1  # Indikerer fejl


if __name__ == "__main__":
    # sys.exit accepterer int, så vi fanger returkoden fra main()
    sys.exit(main())
