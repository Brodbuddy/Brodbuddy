using Core.Entities;

namespace Application.Models.Results;

public record RegisterAnalyzerResult(
    SourdoughAnalyzer Analyzer,
    UserAnalyzer UserAnalyzer,
    bool IsNewAnalyzer
);