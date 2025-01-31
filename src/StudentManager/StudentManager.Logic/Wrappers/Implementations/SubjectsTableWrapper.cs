﻿using FluentResults;
using LazyCache;
using Serilog;
using StudentManager.Tables;
using StudentManager.Tables.Models;

namespace StudentManager.Logic.Wrappers.Implementations;

public class SubjectsTableWrapper : BaseTableWrapper<AcademicSubjectData>
{
    private readonly string _cacheDictByGroupIdKey;
    private readonly string _cacheDictByTeacherIdKey;

    protected override TimeSpan CacheLifeTime => TimeSpan.FromMinutes(6);

    public SubjectsTableWrapper(IManagerSheetEditor<AcademicSubjectData> sheet, IAppCache appCache, ILogger logger)
        : base(sheet, appCache, logger)
    {
        _cacheDictByGroupIdKey = $"{GetType().Name}-ByGroupId";
        _cacheDictByTeacherIdKey = $"{GetType().Name}-ByTeacherId";
    }

    protected override async Task<List<AcademicSubjectData>> UpdateCache()
    {
        var result = await base.UpdateCache();
        AppCache.Add(_cacheDictByGroupIdKey, result.GroupBy(x => x.IdGroup).ToDictionary(x => x.Key, x => x.ToArray()),
            GetCacheOptions());
        AppCache.Add(_cacheDictByTeacherIdKey,
            result.GroupBy(x => x.IdTeacher).ToDictionary(x => x.Key, x => x.ToArray()),
            GetCacheOptions());
        return result;
    }

    public virtual async Task<Result<AcademicSubjectData[]>> ReadByGroupId(string groupId)
    {
        if (!AppCache.TryGetValue<Dictionary<string, AcademicSubjectData[]>>(_cacheDictByGroupIdKey, out var dict))
            return Result.Fail<AcademicSubjectData[]>(WrapperErrors.EmptyInGoogleTablesCache);

        return dict.TryGetValue(groupId, out var value)
            ? value
            : Array.Empty<AcademicSubjectData>();
    }

    public virtual async Task<Result<AcademicSubjectData[]>> ReadByTeacherId(string teacherId)
    {
        if (!AppCache.TryGetValue<Dictionary<string, AcademicSubjectData[]>>(_cacheDictByTeacherIdKey, out var dict))
            return Result.Fail<AcademicSubjectData[]>(WrapperErrors.EmptyInGoogleTablesCache);

        return dict.TryGetValue(teacherId, out var value)
            ? value
            : Array.Empty<AcademicSubjectData>();
    }

    public virtual async Task<Result<AcademicSubjectData[]>> ReadByIds(IEnumerable<string> ids)
    {
        if (!AppCache.TryGetValue<Dictionary<string, AcademicSubjectData>>(CacheDictByIdKey, out var dict))
            return Result.Fail<AcademicSubjectData[]>(WrapperErrors.EmptyInGoogleTablesCache);

        return ids
            .Where(id => dict.ContainsKey(id))
            .Select(id => dict[id])
            .ToArray();
    }
}