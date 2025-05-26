using Core.Entities;

namespace Application.Models.DTOs;

public class AnalyzerWithUpdateStatus
{
    public SourdoughAnalyzer Analyzer { get; set; } = null!;
    public bool HasUpdate { get; set; }
}