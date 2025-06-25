using Simple.DynamicWebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWebApiSample.Samples;


/// <summary>
/// 动态Web API示例服务类
/// </summary>
public class SampleQueryService : IDynamicWebApi
{
    /// <summary>
    /// SampleDto
    /// </summary>
    public record SampleDto(int Id, string Name, bool IsActive);

    /// <summary>
    /// 模拟数据
    /// </summary>
    private static readonly List<SampleDto> _samples = new()
    {
        new SampleDto(1, "Sample A", true),
        new SampleDto(2, "Sample B", false),
        new SampleDto(3, "Sample C", true),
        new SampleDto(4, "Sample D", true)
    };


    public SampleDto Get(int id)
    {
        return _samples.FirstOrDefault(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Sample with ID {id} not found");
    }

    public List<SampleDto> QueryStatus(bool isActive)
    {
        return _samples
            .Where(s => s.IsActive == isActive)
            .ToList();
    }


    public List<SampleDto> FindName(string name)
    {
        return _samples
            .Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public List<SampleDto> FetchAll()
    {
        return _samples;
    }

    public List<SampleDto> SelectActive()
     => QueryStatus(true);

    public object GetStatistics()
    {
        return new
        {
            TotalCount = _samples.Count,
            ActiveCount = _samples.Count(s => s.IsActive),
            LatestSample = _samples.LastOrDefault()?.Name
        };
    }
}
